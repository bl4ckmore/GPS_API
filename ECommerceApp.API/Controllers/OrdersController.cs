using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ECommerceApp.Infrastructure.Data;
using ECommerceApp.Core.Entities;
using ECommerceApp.Core.Interfaces;
using System.Linq;
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
    private readonly IConfiguration _config;
    private readonly IServiceProvider _serviceProvider;

    public OrdersController(
        ApplicationDbContext db,
        IEmailSender email,
        ILogger<OrdersController> log,
        IConfiguration config,
        IServiceProvider serviceProvider)
    {
        _db = db;
        _email = email;
        _log = log;
        _config = config;
        _serviceProvider = serviceProvider;
    }

    [HttpGet]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
    public async Task<IActionResult> GetAllOrders(CancellationToken ct)
    {
        var query =
            from o in _db.Orders.AsNoTracking()
            join u in _db.Users.AsNoTracking() on o.UserId equals u.id into users
            from u in users.DefaultIfEmpty()
            orderby o.CreatedAt descending
            select new
            {
                o.id,
                o.OrderNumber,
                Status = o.Status.ToString(),
                o.Total,
                o.CreatedAt,
                CustomerName = u != null ? u.Username : (o.FullName ?? "Guest"),
                o.Email,
                o.Phone,
                o.AddressLine,
                o.City,
                o.Country,
                o.Notes,
                ItemCount = o.Items.Count
            };

        var list = await query.ToListAsync(ct);
        return Ok(list);
    }

    [HttpPost("place")]
    public async Task<IActionResult> Place([FromBody] PlaceOrderRequest req, CancellationToken ct)
    {
        var userId = await ResolveUserIdAsync(ct);
        if (userId == Guid.Empty)
            return Unauthorized(new { error = "Cannot resolve user from JWT" });

        var cart = await _db.Carts.AsNoTracking().FirstOrDefaultAsync(c => c.UserId == userId, ct);
        if (cart is null) return BadRequest(new { error = "Cart is empty" });

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

        var subtotal = items.Sum(i => i.UnitPrice * i.Qty);
        var total = subtotal;
        var now = DateTime.UtcNow;
        var orderNumber = $"EC-{now:yyyyMMdd}-{now.Ticks % 100000:D5}";

        var order = new Order
        {
            id = Guid.NewGuid(),
            UserId = userId,
            Status = OrderStatus.Pending,
            OrderNumber = orderNumber,
            Subtotal = subtotal,
            Total = total,
            CreatedAt = now,
            FullName = req.FullName,
            Email = req.Email,
            Phone = req.Phone,
            AddressLine = req.AddressLine,
            City = req.City,
            Country = req.Country,
            Notes = req.Notes
        };

        await _db.Orders.AddAsync(order, ct);

        foreach (var it in items)
        {
            order.Items.Add(new OrderItem
            {
                id = Guid.NewGuid(),
                ProductId = it.ProductId,
                unitPrice = it.UnitPrice,
                qty = it.Qty
            });
        }

        var cartItems = _db.CartItems.Where(ci => ci.CartId == cart.id);
        _db.CartItems.RemoveRange(cartItems);

        await _db.SaveChangesAsync(ct);

        // --- FIXED: .Cast<dynamic>() ---
        var emailItems = items.Cast<dynamic>();

        _ = Task.Run(async () =>
        {
            await SendOrderEmails(order.Email, orderNumber, total, emailItems, req.FullName, req.AddressLine);
        });

        return Ok(new
        {
            orderId = order.id,
            orderNumber,
            total,
            items = items.Select(i => new { i.ProductId, i.Title, i.Qty, i.UnitPrice })
        });
    }

    [HttpGet("my")]
    public async Task<IActionResult> MyOrders(CancellationToken ct)
    {
        var userId = await ResolveUserIdAsync(ct);
        if (userId == Guid.Empty) return Unauthorized();

        var orders = await _db.Orders
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new { o.id, o.OrderNumber, Status = o.Status.ToString(), o.Total, o.CreatedAt })
            .ToListAsync(ct);

        return Ok(orders);
    }

    [HttpGet("{orderId:guid}/items")]
    public async Task<IActionResult> GetItems(Guid orderId, CancellationToken ct)
    {
        var userId = await ResolveUserIdAsync(ct);
        var isAdmin = User.IsInRole("Admin");

        var isUserOrder = await _db.Orders.AnyAsync(o => o.id == orderId && o.UserId == userId, ct);

        if (!isUserOrder && !isAdmin) return NotFound();

        var items = await _db.OrderItems
            .Where(oi => oi.OrderId == orderId)
            .Join(_db.Products, oi => oi.ProductId, p => p.id, (oi, p) => new
            { oi.id, p.Name, p.SKU, p.ImageUrl, Qty = oi.qty, UnitPrice = oi.unitPrice })
            .ToListAsync(ct);

        return Ok(items);
    }

    private async Task SendOrderEmails(string? userEmail, string orderNum, decimal total, IEnumerable<dynamic> items, string? customerName, string? address)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            var adminEmail = config["AppConfig:ORDERS_ADMIN_EMAIL"];

            var sb = new StringBuilder();
            sb.Append($"<h2>Order Confirmation: {orderNum}</h2>");
            sb.Append($"<p>Dear {customerName ?? "Customer"},</p>");
            sb.Append("<p>Thank you for shopping with GPS Hub! We have received your order.</p>");

            sb.Append("<table style='width:100%; border-collapse: collapse; margin-top: 15px;'>");
            sb.Append("<tr style='background-color: #f3f3f3; text-align: left;'>");
            sb.Append("<th style='padding: 8px; border-bottom: 1px solid #ddd;'>Product</th>");
            sb.Append("<th style='padding: 8px; border-bottom: 1px solid #ddd;'>Qty</th>");
            sb.Append("<th style='padding: 8px; border-bottom: 1px solid #ddd;'>Price</th>");
            sb.Append("</tr>");

            foreach (var item in items)
            {
                sb.Append("<tr>");
                sb.Append($"<td style='padding: 8px; border-bottom: 1px solid #ddd;'>{item.Title}</td>");
                sb.Append($"<td style='padding: 8px; border-bottom: 1px solid #ddd;'>{item.Qty}</td>");
                sb.Append($"<td style='padding: 8px; border-bottom: 1px solid #ddd;'>{item.UnitPrice:C}</td>");
                sb.Append("</tr>");
            }
            sb.Append("</table>");

            sb.Append($"<h3>Total: {total:C}</h3>");
            sb.Append($"<p><strong>Shipping to:</strong> {address ?? "Address provided"}</p>");
            sb.Append("<p>We will contact you shortly regarding delivery.</p>");
            sb.Append("<hr><p style='font-size: 12px; color: #888;'>GPS Hub Georgia</p>");

            var body = sb.ToString();

            if (!string.IsNullOrWhiteSpace(userEmail))
                await emailSender.SendAsync(userEmail, $"Order #{orderNum} Confirmed", body, CancellationToken.None);

            if (!string.IsNullOrWhiteSpace(adminEmail))
                await emailSender.SendAsync(adminEmail, $"[New Order] {orderNum} - {total:C}", body, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to send order emails");
        }
    }

    private async Task<Guid> ResolveUserIdAsync(CancellationToken ct)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
        if (Guid.TryParse(sub, out var g)) return g;

        var uid = User.FindFirst("userId")?.Value ?? User.FindFirst("uid")?.Value;
        if (Guid.TryParse(uid, out var g2)) return g2;

        var username = User.Identity?.Name ?? User.FindFirst(ClaimTypes.Email)?.Value;
        if (!string.IsNullOrWhiteSpace(username))
        {
            var userId = await _db.Users
                .Where(u => u.Username == username || u.Email == username)
                .Select(u => u.id)
                .FirstOrDefaultAsync(ct);
            if (userId != Guid.Empty) return userId;
        }

        return Guid.Empty;
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