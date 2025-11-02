using System;
using System.Linq;
using System.Threading.Tasks;
using ECommerceApp.Core.Entities;
using ECommerceApp.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public OrdersController(ApplicationDbContext db) => _db = db;

        private Guid GetUserId()
        {
            var sub = User.Identity?.Name ?? throw new InvalidOperationException("No user in context.");
            var user = _db.Users.AsNoTracking().FirstOrDefault(u => u.Username == sub)
                       ?? throw new InvalidOperationException("User not found.");
            return user.id;
        }

        public sealed class PlaceOrderDto
        {
            public string? FullName { get; set; }
            public string? Phone { get; set; }
            public string? Email { get; set; }
            public string? AddressLine { get; set; }
            public string? City { get; set; }
            public string? Country { get; set; }
            public string? Notes { get; set; }
            public decimal Shipping { get; set; } = 0m;
            public decimal Discount { get; set; } = 0m;
            public decimal Tax { get; set; } = 0m;
        }

        [HttpPost("place")]
        public async Task<IActionResult> Place([FromBody] PlaceOrderDto body)
        {
            var userId = GetUserId();

            var cart = await _db.Carts
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.UserId == userId && !c.IsDeleted);

            if (cart == null) return BadRequest(new { error = "Cart not found" });

            var linePairs = await _db.CartItems
                .Where(ci => ci.CartId == cart.id && !ci.IsDeleted)
                .Join(_db.Products,
                      ci => ci.ProductId,
                      p => p.id,
                      (ci, p) => new { ci, p })
                .ToListAsync();

            if (linePairs.Count == 0) return BadRequest(new { error = "Cart is empty" });

            var subtotal = linePairs.Sum(x => (x.ci.unitPrice > 0 ? x.ci.unitPrice : x.p.Price) * x.ci.qty);
            var shipping = body.Shipping;
            var discount = body.Discount;
            var tax = body.Tax;
            var total = subtotal + shipping + tax - discount;

            var order = new Order
            {
                id = Guid.NewGuid(),
                UserId = userId,
                Status = OrderStatus.Pending,
                Subtotal = subtotal,
                Shipping = shipping,
                Discount = discount,
                Tax = tax,
                Total = total,
                FullName = body.FullName,
                Phone = body.Phone,
                Email = body.Email,
                AddressLine = body.AddressLine,
                City = body.City,
                Country = body.Country,
                Notes = body.Notes,
                CreatedAt = DateTime.UtcNow
            };

            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            foreach (var x in linePairs)
            {
                var price = x.ci.unitPrice > 0 ? x.ci.unitPrice : x.p.Price;

                var oi = new OrderItem
                {
                    id = Guid.NewGuid(),
                    OrderId = order.id,
                    ProductId = x.p.id,
                    qty = x.ci.qty,
                    unitPrice = price
                    // ❌ Do NOT set Title/Sku/ImageUrl – these are NOT in your entity
                };
                _db.OrderItems.Add(oi);
            }

            // Soft-clear cart
            var cartItems = await _db.CartItems.Where(ci => ci.CartId == cart.id && !ci.IsDeleted).ToListAsync();
            foreach (var ci in cartItems)
            {
                ci.IsDeleted = true;
                ci.DeletedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();

            return Ok(new { orderId = order.id, total = order.Total });
        }

        [HttpGet("mine")]
        public async Task<IActionResult> MyOrders()
        {
            var userId = GetUserId();

            var orders = await _db.Orders
                .Where(o => o.UserId == userId && !o.IsDeleted)
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new
                {
                    o.id,
                    o.Status,
                    o.Total,
                    o.Subtotal,
                    o.Shipping,
                    o.Discount,
                    o.Tax,
                    o.CreatedAt
                })
                .ToListAsync();

            return Ok(orders);
        }

        [HttpGet("{orderId:guid}/items")]
        public async Task<IActionResult> GetItems(Guid orderId)
        {
            var userId = GetUserId();

            var exists = await _db.Orders
                .AnyAsync(o => o.id == orderId && o.UserId == userId && !o.IsDeleted);

            if (!exists) return NotFound();

            // Join to Products to expose name/SKU/image without storing on OrderItem
            var items = await _db.OrderItems
                .Where(oi => oi.OrderId == orderId && !oi.IsDeleted)
                .Join(_db.Products,
                      oi => oi.ProductId,
                      p => p.id,
                      (oi, p) => new
                      {
                          oi.id,
                          oi.ProductId,
                          qty = oi.qty,
                          unitPrice = oi.unitPrice,
                          title = p.Name,
                          sku = p.SKU,
                          imageUrl = p.ImageUrl
                      })
                .ToListAsync();

            return Ok(items);
        }
    }
}
