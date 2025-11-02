using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ECommerceApp.Infrastructure.Data;
using ECommerceApp.Core.Entities;
using ECommerceApp.Core.Interfaces;
using ECommerceApp.Infrastructure.Email;


namespace ECommerceApp.API.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Produces("application/json")]
public sealed class OrdersController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IEmailSender _email;
    private readonly ILogger<OrdersController> _log;

    public OrdersController(ApplicationDbContext db, IEmailSender email, ILogger<OrdersController> log)
    {
        _db = db;
        _email = email;
        _log = log;
    }

    // POST /api/orders/place
    [HttpPost("place")]
    public async Task<IActionResult> Place(CancellationToken ct)
    {
        var userId = await ResolveUserIdAsync(ct);
        if (userId == Guid.Empty) return Unauthorized(new { error = "Cannot resolve user from JWT" });

        // Cart
        var cart = await _db.Carts.AsNoTracking().FirstOrDefaultAsync(c => c.UserId == userId, ct);
        if (cart is null) return BadRequest(new { error = "Cart is empty" });

        // Items (use your model fields: ci.qty, NOT Quantity)
        var items = await _db.CartItems
            .Where(ci => ci.CartId == cart.id)
            .Join(_db.Products, ci => ci.ProductId, p => p.id, (ci, p) => new
            {
                ProductId = p.id,
                Title = p.Name,
                UnitPrice = p.Price,      // Price is non-nullable decimal in your model
                Qty = ci.qty,
                ImageUrl = p.ImageUrl,
                SKU = p.SKU
            })
            .ToListAsync(ct);

        if (items.Count == 0) return BadRequest(new { error = "Cart has no items" });

        // Totals
        var subtotal = items.Sum(i => i.UnitPrice * i.Qty);
        var tax = 0m;
        var shipping = 0m;
        var discount = 0m;
        var total = subtotal + tax + shipping - discount;

        // Create Order (Status is enum)
        var order = new Order
        {
            id = Guid.NewGuid(),
            UserId = userId,
            Status = OrderStatus.Pending,  // <-- enum
            Subtotal = subtotal,
            Tax = tax,
            Shipping = shipping,
            Discount = discount,
            Total = total,
            CreatedAt = DateTime.UtcNow
        };
        await _db.Orders.AddAsync(order, ct);

        // Create OrderItems (your fields: unitPrice, qty)
        foreach (var it in items)
        {
            var oi = new OrderItem
            {
                id = Guid.NewGuid(),
                OrderId = order.id,
                ProductId = it.ProductId,
                unitPrice = it.UnitPrice,
                qty = it.Qty
            };
            await _db.OrderItems.AddAsync(oi, ct);
        }

        // Clear cart
        var cartItems = _db.CartItems.Where(ci => ci.CartId == cart.id);
        _db.CartItems.RemoveRange(cartItems);

        await _db.SaveChangesAsync(ct);

        // Email (take from JWT claims)
        var userEmail = GetUserEmailFromClaims();
        if (!string.IsNullOrWhiteSpace(userEmail))
        {
            var lines = string.Join("", items.Select(i =>
                $"<tr><td style='padding:6px 8px'>{System.Net.WebUtility.HtmlEncode(i.Title)}</td><td style='padding:6px 8px'>{i.Qty}</td><td style='padding:6px 8px'>{i.UnitPrice:C}</td></tr>"));

            var html = $@"
                <h2>Order confirmation</h2>
                <p>Thank you for your order.</p>
                <table style='border-collapse:collapse'>
                    <thead>
                        <tr>
                          <th align='left' style='padding:6px 8px'>Product</th>
                          <th style='padding:6px 8px'>Qty</th>
                          <th style='padding:6px 8px'>Price</th>
                        </tr>
                    </thead>
                    <tbody>{lines}</tbody>
                </table>
                <p><strong>Subtotal:</strong> {subtotal:C}<br/>
                   <strong>Tax:</strong> {tax:C}<br/>
                   <strong>Shipping:</strong> {shipping:C}<br/>
                   <strong>Total:</strong> {total:C}</p>";

            await _email.SendAsync(userEmail, "Your order confirmation", html, ct);
        }

        var adminEmail = Environment.GetEnvironmentVariable("ORDERS_ADMIN_EMAIL");
        if (!string.IsNullOrWhiteSpace(adminEmail))
        {
            await _email.SendAsync(adminEmail, "New order placed",
                $"<p>User: {System.Net.WebUtility.HtmlEncode(userEmail)}<br/>Total: {total:C}<br/>Items: {items.Count}</p>", ct);
        }

        return Ok(new
        {
            orderId = order.id,
            total,
            items = items.Select(i => new { i.ProductId, i.Title, i.Qty, i.UnitPrice })
        });
    }

    // GET /api/orders/my
    [HttpGet("my")]
    public async Task<IActionResult> MyOrders(CancellationToken ct)
    {
        var userId = await ResolveUserIdAsync(ct);
        if (userId == Guid.Empty) return Unauthorized();

        var orders = await _db.Orders
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new
            {
                o.id,
                o.Status,
                o.Total,
                o.Subtotal,
                o.Tax,
                o.Shipping,
                o.Discount,
                o.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(orders);
    }

    // GET /api/orders/{orderId}/items
    [HttpGet("{orderId:guid}/items")]
    public async Task<IActionResult> GetItems(Guid orderId, CancellationToken ct)
    {
        var userId = await ResolveUserIdAsync(ct);
        if (userId == Guid.Empty) return Unauthorized();

        var ok = await _db.Orders.AnyAsync(o => o.id == orderId && o.UserId == userId, ct);
        if (!ok) return NotFound();

        var items = await _db.OrderItems
            .Where(oi => oi.OrderId == orderId)
            .Join(_db.Products, oi => oi.ProductId, p => p.id, (oi, p) => new
            {
                oi.id,
                p.Name,
                p.SKU,
                p.ImageUrl,
                Qty = oi.qty,                  // <-- your field
                UnitPrice = oi.unitPrice       // <-- your field
            })
            .ToListAsync(ct);

        return Ok(items);
    }

    // ===== Helpers =====
    private async Task<Guid> ResolveUserIdAsync(CancellationToken ct)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(sub, out var guid)) return guid;

        var name = User.Identity?.Name ?? User.FindFirstValue(ClaimTypes.Name);
        if (!string.IsNullOrWhiteSpace(name))
        {
            var user = await _db.Users
                .Where(u => u.Username == name)
                .Select(u => u.id)
                .FirstOrDefaultAsync(ct);
            if (user != Guid.Empty) return user;
        }
        return Guid.Empty;
    }

    private string? GetUserEmailFromClaims()
    {
        // Try standard email claims
        return User.FindFirstValue(ClaimTypes.Email)
            ?? User.FindFirst("email")?.Value
            ?? User.FindFirst("preferred_username")?.Value; // fallback if you stuff email there
    }
}
