using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using ECommerceApp.Infrastructure.Data;
using ECommerceApp.Core.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceApp.API.Controllers
{
    [ApiController]
    [Route("api/cart")] // ✅ FIX: Route matches Angular 'api/cart'
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public sealed class CartController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public CartController(ApplicationDbContext db)
        {
            _db = db;
        }

        // 1. GET CART
        [HttpGet]
        public async Task<IActionResult> GetCart(CancellationToken ct)
        {
            var userId = await ResolveUserIdAsync(ct);
            if (userId == Guid.Empty) return Unauthorized();

            var dto = await BuildCartDto(userId, ct);
            return Ok(dto);
        }

        // 2. ADD ITEM
        [HttpPost]
        public async Task<IActionResult> AddItem([FromBody] AddItemDto body, CancellationToken ct)
        {
            if (body == null || body.ProductId == Guid.Empty)
                return BadRequest(new { error = "Invalid product" });

            var qtyToAdd = body.Quantity > 0 ? body.Quantity : 1;
            var userId = await ResolveUserIdAsync(ct);
            if (userId == Guid.Empty) return Unauthorized();

            // Find or Create Cart
            var cart = await _db.Carts.FirstOrDefaultAsync(c => c.UserId == userId, ct);
            if (cart == null)
            {
                cart = new Cart { id = Guid.NewGuid(), CreatedAt = DateTime.UtcNow, UserId = userId };
                _db.Carts.Add(cart);
                await _db.SaveChangesAsync(ct);
            }

            // Find existing item by ProductId
            var existingItem = await _db.CartItems
                .FirstOrDefaultAsync(i => i.CartId == cart.id && i.ProductId == body.ProductId, ct);

            if (existingItem != null)
            {
                existingItem.qty += qtyToAdd; // Update qty
            }
            else
            {
                _db.CartItems.Add(new CartItem
                {
                    id = Guid.NewGuid(),
                    CartId = cart.id,
                    ProductId = body.ProductId,
                    qty = qtyToAdd,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync(ct);
            return Ok(await BuildCartDto(userId, ct));
        }

        // 3. UPDATE QUANTITY
        [HttpPut("{itemId}")]
        public async Task<IActionResult> UpdateItem(string itemId, [FromBody] UpdateItemDto body, CancellationToken ct)
        {
            var userId = await ResolveUserIdAsync(ct);
            if (userId == Guid.Empty) return Unauthorized();

            var cart = await _db.Carts.FirstOrDefaultAsync(c => c.UserId == userId, ct);
            if (cart == null) return NotFound("Cart not found");

            // Look up by Item ID OR Product ID (Hybrid check)
            var item = await _db.CartItems
                .FirstOrDefaultAsync(i =>
                    i.CartId == cart.id &&
                    (i.id.ToString() == itemId || i.ProductId.ToString() == itemId), ct);

            if (item == null) return NotFound("Item not found in cart");

            if (body.Quantity <= 0)
            {
                _db.CartItems.Remove(item);
            }
            else
            {
                item.qty = body.Quantity;
            }

            await _db.SaveChangesAsync(ct);
            return Ok(await BuildCartDto(userId, ct));
        }

        // 4. REMOVE ITEM
        [HttpDelete("{itemId}")]
        public async Task<IActionResult> RemoveItem(string itemId, CancellationToken ct)
        {
            var userId = await ResolveUserIdAsync(ct);
            if (userId == Guid.Empty) return Unauthorized();

            var cart = await _db.Carts.FirstOrDefaultAsync(c => c.UserId == userId, ct);
            if (cart == null) return Ok(new CartViewDto());

            var item = await _db.CartItems
                .FirstOrDefaultAsync(i =>
                    i.CartId == cart.id &&
                    (i.id.ToString() == itemId || i.ProductId.ToString() == itemId), ct);

            if (item != null)
            {
                _db.CartItems.Remove(item);
                await _db.SaveChangesAsync(ct);
            }

            return Ok(await BuildCartDto(userId, ct));
        }

        // 5. CLEAR CART
        [HttpDelete]
        public async Task<IActionResult> ClearCart(CancellationToken ct)
        {
            var userId = await ResolveUserIdAsync(ct);
            var cart = await _db.Carts.FirstOrDefaultAsync(c => c.UserId == userId, ct);

            if (cart != null)
            {
                var items = _db.CartItems.Where(i => i.CartId == cart.id);
                _db.CartItems.RemoveRange(items);
                await _db.SaveChangesAsync(ct);
            }

            return Ok(new CartViewDto { id = cart?.id ?? Guid.Empty });
        }

        // --- HELPERS ---

        private async Task<CartViewDto> BuildCartDto(Guid userId, CancellationToken ct)
        {
            var cart = await _db.Carts.AsNoTracking().FirstOrDefaultAsync(c => c.UserId == userId, ct);
            if (cart == null) return new CartViewDto();

            var items = await _db.CartItems
                .AsNoTracking()
                .Where(i => i.CartId == cart.id)
                .Include(i => i.Product) // Load Product
                .ToListAsync(ct);

            var itemDtos = items.Select(i => new CartItemViewDto
            {
                id = i.id, // CartItem ID
                quantity = i.qty,
                product = new ProductBriefDto
                {
                    id = i.ProductId,
                    name = i.Product?.Name ?? "Unknown",
                    price = i.Product?.Price ?? 0,
                    imageUrl = i.Product?.ImageUrl,
                    category = i.Product?.Category?.Name,
                    stock = (int)(i.Product?.Stock ?? 0)
                }
            }).ToList();

            var dto = new CartViewDto { id = cart.id, items = itemDtos };
            dto.RecalculateTotals();
            return dto;
        }

        private async Task<Guid> ResolveUserIdAsync(CancellationToken ct)
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim != null && Guid.TryParse(claim.Value, out var id)) return id;

            var name = User.Identity?.Name;
            if (!string.IsNullOrEmpty(name))
            {
                var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == name, ct);
                return user?.id ?? Guid.Empty;
            }
            return Guid.Empty;
        }
    }

    // --- DTOs ---
    public class AddItemDto { public Guid ProductId { get; set; } public int Quantity { get; set; } }
    public class UpdateItemDto { public int Quantity { get; set; } }

    public class CartViewDto
    {
        public Guid id { get; set; }
        public List<CartItemViewDto> items { get; set; } = new();
        public decimal subtotal { get; set; }
        public decimal total { get; set; }
        public void RecalculateTotals()
        {
            subtotal = items.Sum(i => (i.product?.price ?? 0) * i.quantity);
            total = subtotal;
        }
    }

    public class CartItemViewDto
    {
        public Guid id { get; set; }
        public int quantity { get; set; }
        public ProductBriefDto product { get; set; } = new();
    }

    public class ProductBriefDto
    {
        public Guid id { get; set; }
        public string name { get; set; } = string.Empty;
        public decimal price { get; set; }
        public string? imageUrl { get; set; }
        public string? category { get; set; }
        public int stock { get; set; }
    }
}