using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ECommerceApp.Infrastructure.Data;
using ECommerceApp.Core.Entities;
using ECommerceApp.Core.Interfaces;
using ECommerceApp.Infrastructure.Email;
using Microsoft.Extensions.Configuration; // <-- NEW: Required to read AppConfig

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
    private readonly IConfiguration _config; // <-- NEW FIELD

    public OrdersController(
        ApplicationDbContext db,
        IEmailSender email,
        ILogger<OrdersController> log,
        IConfiguration config) // <-- ADD IConfiguration
    {
        _db = db;
        _email = email;
        _log = log;
        _config = config; // <-- ASSIGN
    }

    // POST /api/orders/place
    [HttpPost("place")]
    public async Task<IActionResult> Place([FromBody] PlaceOrderRequest req, CancellationToken ct)
    {
        var userId = await ResolveUserIdAsync(ct);
        if (userId == Guid.Empty) return Unauthorized(new { error = "Cannot resolve user from JWT" });

        // Cart (read-only)
        var cart = await _db.Carts.AsNoTracking().FirstOrDefaultAsync(c => c.UserId == userId, ct);
        if (cart is null) return BadRequest(new { error = "Cart is empty" });

        // Items retrieval
        var items = await _db.CartItems
            .Where(ci => ci.CartId == cart.id)
            .Join(_db.Products, ci => ci.ProductId, p => p.id, (ci, p) => new
            {
                ProductId = p.id,
                Title = p.Name,
                UnitPrice = p.Price,
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

        // Generate a unique Order Number
        var now = DateTime.UtcNow;
        var orderNumber = $"EC-{now:yyyyMMdd}-{now.Ticks % 100000:D5}";

        // Create Order (Parent Entity)
        var order = new Order
        {
            id = Guid.NewGuid(),
            UserId = userId,
            Status = OrderStatus.Pending,
            OrderNumber = orderNumber,
            Subtotal = subtotal,
            Tax = tax,
            Shipping = shipping,
            Discount = discount,
            Total = total,
            CreatedAt = now,

            // Map Contact/Shipping details from DTO
            FullName = req.FullName,
            Email = req.Email,
            Phone = req.Phone,
            AddressLine = req.AddressLine,
            City = req.City,
            Country = req.Country,
            Notes = req.Notes
        };
        await _db.Orders.AddAsync(order, ct);

        // Create OrderItems (Child Entities)
        foreach (var it in items)
        {
            var oi = new OrderItem
            {
                id = Guid.NewGuid(),
                ProductId = it.ProductId,
                unitPrice = it.UnitPrice,
                qty = it.Qty
            };
            order.Items.Add(oi); // Use navigation property
        }

        // Clear cart
        var cartItems = _db.CartItems.Where(ci => ci.CartId == cart.id);
        _db.CartItems.RemoveRange(cartItems);

        // ====================================================================
        // Email Logging Logic - Part 1: Log Intent
        // ====================================================================
        var logs = new List<EmailLog>();
        var userEmail = GetUserEmailFromClaims();

        // Email HTML content preparation
        var lines = string.Join("", items.Select(i =>
            $"<tr><td style='padding:6px 8px'>{System.Net.WebUtility.HtmlEncode(i.Title)}</td><td style='padding:6px 8px'>{i.Qty}</td><td style='padding:6px 8px'>{i.UnitPrice:C}</td></tr>"));

        var userHtml = $@"
            <h2>Order confirmation ({orderNumber})</h2>
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

        // 💥 FIX: Read Admin Email from AppConfig section
        var adminEmail = _config["AppConfig:ORDERS_ADMIN_EMAIL"];
        var adminHtml = $"<p>New order placed! Ref: {orderNumber}<br/>User: {System.Net.WebUtility.HtmlEncode(userEmail)}<br/>Total: {total:C}<br/>Items: {items.Count}</p>";

        // Log *user* email intent
        if (!string.IsNullOrWhiteSpace(userEmail))
        {
            var log = new EmailLog
            {
                OrderId = order.id,
                To = userEmail,
                Subject = $"Your order confirmation ({orderNumber})",
                BodyContent = userHtml.Substring(0, Math.Min(userHtml.Length, 4000)),
            };
            await _db.EmailLogs.AddAsync(log, ct);
            logs.Add(log);
        }

        // Log *admin* email intent
        if (!string.IsNullOrWhiteSpace(adminEmail))
        {
            var log = new EmailLog
            {
                OrderId = order.id,
                To = adminEmail,
                Subject = $"New order placed: {orderNumber}",
                BodyContent = adminHtml.Substring(0, Math.Min(adminHtml.Length, 4000)),
            };
            await _db.EmailLogs.AddAsync(log, ct);
            logs.Add(log);
        }

        // Save Order (Parent), OrderItems (Children), Cart clearance, and EmailLog entries in ONE TRANSACTION
        await _db.SaveChangesAsync(ct);


        // ====================================================================
        // Email Logging Logic - Part 2: Send and Update Status (WITH DEBUGGING)
        // ====================================================================
        foreach (var log in logs)
        {
            var htmlContent = log.To == userEmail ? userHtml : adminHtml;

            try
            {
                await _email.SendAsync(log.To, log.Subject, htmlContent, ct);
                log.SentAt = DateTime.UtcNow;
                log.Status = "Success";

                // 🎯 CONSOLE DEBUG LOGGING: Success message
                _log.LogInformation("EMAIL DEBUG: Successfully completed send process for order {OrderNumber} to {To}.", orderNumber, log.To);
            }
            catch (Exception ex)
            {
                log.Status = "Failed";
                log.ErrorMessage = ex.Message;
                // 🎯 CONSOLE DEBUG LOGGING: Explicit Failure Message
                _log.LogError(ex, "EMAIL DEBUG: FATAL FAILURE during send process for order {OrderNumber} to {To}.", orderNumber, log.To);
            }
        }

        // Save the final log status updates
        if (logs.Any())
            await _db.SaveChangesAsync(ct);

        // ====================================================================

        return Ok(new
        {
            orderId = order.id,
            orderNumber = orderNumber,
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
                o.OrderNumber,
                Status = o.Status.ToString(),
                o.Total,
                o.Subtotal,
                o.Tax,
                o.Shipping,
                o.Discount,
                o.CreatedAt,
                // Select first few items for display
                OrderItems = o.Items
                    .Take(3)
                    .Select(oi => new
                    {
                        oi.qty,
                        Product = _db.Products
                            .Where(p => p.id == oi.ProductId)
                            .Select(p => new { p.Name, p.ImageUrl })
                            .FirstOrDefault()
                    })
                    .Where(item => item.Product != null)
                    .ToList()
            })
            .ToListAsync(ct);

        return Ok(orders);
    }

    // GET /api/orders/{orderId}/items (This endpoint still exists if full details are needed elsewhere)
    [HttpGet("{orderId:guid}/items")]
    public async Task<IActionResult> GetItems(Guid orderId, CancellationToken ct)
    {
        var userId = await ResolveUserIdAsync(ct);
        if (userId == Guid.Empty) return Unauthorized();

        var ok = await _db.Orders.AnyAsync(o => o.id == orderId && o.UserId == userId, ct);
        if (!ok) return NotFound();

        var items = await _db.Orders
            .Where(o => o.id == orderId)
            .SelectMany(o => o.Items)
            .Join(_db.Products, oi => oi.ProductId, p => p.id, (oi, p) => new
            {
                oi.id,
                p.Name,
                p.SKU,
                p.ImageUrl,
                Qty = oi.qty,
                UnitPrice = oi.unitPrice
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
        return User.FindFirstValue(ClaimTypes.Email)
            ?? User.FindFirst("email")?.Value
            ?? User.FindFirst("preferred_username")?.Value;
    }

    public sealed class PlaceOrderRequest
    {
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? AddressLine { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public string? Notes { get; set; }
    }
}