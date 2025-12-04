using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ECommerceApp.Infrastructure.Data;
using ECommerceApp.Core.Entities;
using ECommerceApp.Core.Interfaces;
using ECommerceApp.Infrastructure.Email;

namespace ECommerceApp.API.Controllers;

[ApiController]
[Route("api/orders")]
// Be explicit: use the JWT bearer scheme, same as in Program.cs
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

    // ================== ADMIN: LIST ALL ORDERS ==================
    // Only admin can see all orders
    [HttpGet]
    [Authorize(
        AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
        Roles = "Admin")]
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
                // Customer Info
                CustomerName = u != null ? u.Username : (o.FullName ?? "Guest"),
                o.Email,
                o.Phone,        // Added
                o.AddressLine,  // Added
                o.City,         // Added
                o.Country,      // Added
                o.Notes,        // Added

                // Stats
                ItemCount = o.Items.Count
            };

        var list = await query.ToListAsync(ct);
        return Ok(list);
    }

    // ================== CLIENT: PLACE ORDER ==================
    // POST /api/orders/place
    [HttpPost("place")]
    public async Task<IActionResult> Place([FromBody] PlaceOrderRequest req, CancellationToken ct)
    {
        var userId = await ResolveUserIdAsync(ct);
        if (userId == Guid.Empty)
            return Unauthorized(new { error = "Cannot resolve user from JWT" });

        var cart = await _db.Carts
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId, ct);

        if (cart is null)
            return BadRequest(new { error = "Cart is empty" });

        var items = await _db.CartItems
            .Where(ci => ci.CartId == cart.id)
            .Join(
                _db.Products,
                ci => ci.ProductId,
                p => p.id,
                (ci, p) => new
                {
                    ProductId = p.id,
                    Title = p.Name,
                    UnitPrice = p.Price,
                    Qty = ci.qty,
                    ImageUrl = p.ImageUrl,
                    SKU = p.SKU
                })
            .ToListAsync(ct);

        if (items.Count == 0)
            return BadRequest(new { error = "Cart has no items" });

        var subtotal = items.Sum(i => i.UnitPrice * i.Qty);
        var tax = 0m;
        var shipping = 0m;
        var discount = 0m;
        var total = subtotal + tax + shipping - discount;

        var now = DateTime.UtcNow;
        var orderNumber = $"EC-{now:yyyyMMdd}-{now.Ticks % 100000:D5}";

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

        // Clear cart
        var cartItems = _db.CartItems.Where(ci => ci.CartId == cart.id);
        _db.CartItems.RemoveRange(cartItems);

        await _db.SaveChangesAsync(ct);

        // Fire & forget emails in background
        _ = Task.Run(async () =>
        {
            await SendOrderEmails(order.id, order.Email, orderNumber, total, items);
        });

        return Ok(new
        {
            orderId = order.id,
            orderNumber,
            total,
            items = items.Select(i => new { i.ProductId, i.Title, i.Qty, i.UnitPrice })
        });
    }

    // ================== CLIENT: MY ORDERS ==================
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
                o.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(orders);
    }

    // ================== ITEMS FOR AN ORDER ==================
    // GET /api/orders/{orderId}/items
    [HttpGet("{orderId:guid}/items")]
    public async Task<IActionResult> GetItems(Guid orderId, CancellationToken ct)
    {
        var userId = await ResolveUserIdAsync(ct);
        var isAdmin = User.IsInRole("Admin");

        // Either it's this user's order OR user is admin
        var isUserOrder = await _db.Orders
            .AnyAsync(o => o.id == orderId && o.UserId == userId, ct);

        if (!isUserOrder && !isAdmin)
            return NotFound();

        var items = await _db.OrderItems
            .Where(oi => oi.OrderId == orderId)
            .Join(
                _db.Products,
                oi => oi.ProductId,
                p => p.id,
                (oi, p) => new
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

    // ================== EMAIL SENDER (BACKGROUND) ==================
    private async Task SendOrderEmails(
        Guid orderId,
        string? userEmail,
        string orderNum,
        decimal total,
        IEnumerable<dynamic> items)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            var adminEmail = config["AppConfig:ORDERS_ADMIN_EMAIL"];
            var body = $"<h3>Order {orderNum}</h3><p>Total: {total:C}</p>";

            if (!string.IsNullOrWhiteSpace(userEmail))
            {
                await emailSender.SendAsync(
                    userEmail,
                    $"Order Confirmation {orderNum}",
                    body,
                    CancellationToken.None);
            }

            if (!string.IsNullOrWhiteSpace(adminEmail))
            {
                await emailSender.SendAsync(
                    adminEmail,
                    $"New Order: {orderNum}",
                    body,
                    CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to send order emails");
        }
    }

    // ================== USER RESOLUTION FROM JWT ==================
    private async Task<Guid> ResolveUserIdAsync(CancellationToken ct)
    {
        // 1) Try standard NameIdentifier / "sub" as GUID
        var sub =
            User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirst("sub")?.Value;

        if (!string.IsNullOrEmpty(sub) && Guid.TryParse(sub, out var guidFromSub))
            return guidFromSub;

        // 2) Try custom "userId" / "uid" claims as GUID
        var uid =
            User.FindFirst("userId")?.Value ??
            User.FindFirst("uid")?.Value ??
            User.FindFirst("UserId")?.Value;

        if (!string.IsNullOrEmpty(uid) && Guid.TryParse(uid, out var guidFromUid))
            return guidFromUid;

        // 3) Fallback: resolve by username or email
        var username =
            User.Identity?.Name ??
            User.FindFirstValue(ClaimTypes.Name) ??
            User.FindFirst("unique_name")?.Value ??
            User.FindFirst("preferred_username")?.Value ??
            User.FindFirst(ClaimTypes.Email)?.Value;

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

    // ================== DTO ==================
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
