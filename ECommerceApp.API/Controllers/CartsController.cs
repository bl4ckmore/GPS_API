using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using ECommerceApp.Infrastructure.Data;
using ECommerceApp.Core.Entities;
using ECommerceApp.Core.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceApp.API.Controllers;

[ApiController]
[Route("api/carts")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Consumes("application/json")]
[Produces("application/json")]
public sealed class CartsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IGenericRepository<Cart> _carts;
    private readonly IGenericRepository<CartItem> _cartItems;
    private readonly ILogger<CartsController> _log;

    public CartsController(
        ApplicationDbContext db,
        IGenericRepository<Cart> carts,
        IGenericRepository<CartItem> cartItems,
        ILogger<CartsController> log)
    {
        _db = db;
        _carts = carts;
        _cartItems = cartItems;
        _log = log;
    }

    // =========================================================================
    // READ CURRENT USER CART
    // =========================================================================

    // Alias: GET /api/carts  -> same as GET /api/carts/me  (fixes 405 from frontend)
    [HttpGet]
    public Task<IActionResult> GetMyCartAlias(CancellationToken ct) => GetMyCart(ct);

    // Main: GET /api/carts/me
    [HttpGet("me")]
    public async Task<IActionResult> GetMyCart(CancellationToken ct)
    {
        var userId = await ResolveUserIdAsync(ct);
        if (userId == Guid.Empty)
            return Unauthorized(new { error = "Cannot resolve user from JWT" });

        var cartId = await _db.Carts.AsNoTracking()
            .Where(c => c.UserId == userId)
            .Select(c => c.id)
            .FirstOrDefaultAsync(ct);

        if (cartId == Guid.Empty)
            return Ok(new CartViewDto { id = Guid.Empty, items = new() });

        var items = await _db.CartItems
            .Where(ci => ci.CartId == cartId)
            .Join(_db.Products, ci => ci.ProductId, p => p.id, (ci, p) => new { ci, p })
            .Select(x => new CartItemViewDto
            {
                id = x.ci.id,
                quantity = x.ci.Quantity,
                product = new ProductBriefDto
                {
                    id = x.p.id,
                    name = x.p.Name,
                    price = (decimal?)(x.p.Price) ?? 0m,
                    imageUrl = x.p.ImageUrl ?? "",
                    category = x.p.Category != null ? x.p.Category.Name : null,
                    type = x.p.Type,
                    stock = (int?)(x.p.Stock) ?? 0
                }
            })
            .ToListAsync(ct);

        return Ok(new CartViewDto { id = cartId, items = items });
    }

    // Optional admin/debug: GET /api/carts/all  (moved from bare [HttpGet] to avoid conflict)
    [HttpGet("all")]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var all = await _carts.GetAllAsync(ct);
        return Ok(all);
    }

    // Optional: GET /api/carts/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var cart = await _carts.GetByIdAsync(id, ct);
        return cart is null ? NotFound() : Ok(cart);
    }

    // =========================================================================
    // WRITE OPERATIONS
    // =========================================================================

    // POST /api/carts/items   (add or merge)
    [HttpPost("items")]
    public async Task<IActionResult> AddItem([FromBody] AddItemRequestDto dto, CancellationToken ct)
    {
        if (dto is null) return BadRequest(new { error = "Body required" });
        if (dto.ProductId == Guid.Empty) return BadRequest(new { error = "productId invalid" });
        if (dto.Quantity <= 0) return BadRequest(new { error = "quantity must be > 0" });

        var userId = await ResolveUserIdAsync(ct);
        if (userId == Guid.Empty) return Unauthorized(new { error = "Cannot resolve user from JWT" });

        var cart = await _db.Carts.FirstOrDefaultAsync(c => c.UserId == userId, ct);
        if (cart is null)
        {
            cart = new Cart { id = Guid.NewGuid(), CreatedAt = DateTime.UtcNow, UserId = userId };
            await _db.Carts.AddAsync(cart, ct);
        }

        var existing = await _db.CartItems.FirstOrDefaultAsync(
            i => i.CartId == cart.id && i.ProductId == dto.ProductId, ct);

        if (existing is not null)
        {
            existing.Quantity += dto.Quantity;
        }
        else
        {
            var newItem = new CartItem
            {
                id = Guid.NewGuid(),
                CartId = cart.id,
                ProductId = dto.ProductId,
                Quantity = dto.Quantity
            };
            await _db.CartItems.AddAsync(newItem, ct);
        }

        await _db.SaveChangesAsync(ct);
        return await GetMyCart(ct);
    }

    // PATCH /api/carts/items/{itemId}   (update quantity)
    [HttpPatch("items/{itemId:guid}")]
    public async Task<IActionResult> UpdateItem(Guid itemId, [FromBody] UpdateQtyDto dto, CancellationToken ct)
    {
        if (dto is null || dto.quantity <= 0)
            return BadRequest(new { error = "quantity must be > 0" });

        var userId = await ResolveUserIdAsync(ct);
        if (userId == Guid.Empty) return Unauthorized(new { error = "Cannot resolve user from JWT" });

        var item = await _db.CartItems
            .Join(_db.Carts, ci => ci.CartId, c => c.id, (ci, c) => new { ci, c })
            .Where(x => x.ci.id == itemId && x.c.UserId == userId)
            .Select(x => x.ci)
            .FirstOrDefaultAsync(ct);

        if (item is null) return NotFound();

        item.Quantity = dto.quantity;
        await _db.SaveChangesAsync(ct);

        return await GetMyCart(ct);
    }

    // DELETE /api/carts/items/{itemId}
    [HttpDelete("items/{itemId:guid}")]
    public async Task<IActionResult> RemoveItem(Guid itemId, CancellationToken ct)
    {
        var userId = await ResolveUserIdAsync(ct);
        if (userId == Guid.Empty) return Unauthorized(new { error = "Cannot resolve user from JWT" });

        var item = await _db.CartItems
            .Join(_db.Carts, ci => ci.CartId, c => c.id, (ci, c) => new { ci, c })
            .Where(x => x.ci.id == itemId && x.c.UserId == userId)
            .Select(x => x.ci)
            .FirstOrDefaultAsync(ct);

        if (item is null) return NotFound();

        _db.CartItems.Remove(item);
        await _db.SaveChangesAsync(ct);

        return await GetMyCart(ct);
    }

    // DELETE /api/carts   (clear my cart)
    [HttpDelete]
    public async Task<IActionResult> ClearCart(CancellationToken ct)
    {
        var userId = await ResolveUserIdAsync(ct);
        if (userId == Guid.Empty) return Unauthorized(new { error = "Cannot resolve user from JWT" });

        var cartId = await _db.Carts
            .Where(c => c.UserId == userId)
            .Select(c => c.id)
            .FirstOrDefaultAsync(ct);

        if (cartId == Guid.Empty)
            return Ok(new CartViewDto { id = Guid.Empty, items = new() });

        var items = _db.CartItems.Where(i => i.CartId == cartId);
        _db.CartItems.RemoveRange(items);
        await _db.SaveChangesAsync(ct);

        return Ok(new CartViewDto { id = cartId, items = new() });
    }

    // =========================================================================
    // HELPERS
    // =========================================================================

    private async Task<Guid> ResolveUserIdAsync(CancellationToken ct)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(sub, out var guid)) return guid;

        var name = User.Identity?.Name ?? User.FindFirstValue(ClaimTypes.Name);
        if (!string.IsNullOrWhiteSpace(name))
        {
            var user = await _db.Users
                .Where(u => u.Username == name)
                .Select(u => new { u.id })
                .FirstOrDefaultAsync(ct);
            if (user is not null) return user.id;
        }
        return Guid.Empty;
    }
}

// ================== DTOs returned/accepted by this controller ==================
public sealed class AddItemRequestDto
{
    [Required] public Guid ProductId { get; set; }
    [Range(1, int.MaxValue)] public int Quantity { get; set; }
}

public sealed class UpdateQtyDto { public int quantity { get; set; } }

public sealed class CartViewDto
{
    public Guid id { get; set; }
    public List<CartItemViewDto> items { get; set; } = new();
}

public sealed class CartItemViewDto
{
    public Guid id { get; set; }
    public int quantity { get; set; }
    public ProductBriefDto product { get; set; } = new();
}

public sealed class ProductBriefDto
{
    public Guid id { get; set; }
    public string? name { get; set; }
    public decimal price { get; set; }
    public string? imageUrl { get; set; }
    public string? category { get; set; }
    public string? type { get; set; }
    public int stock { get; set; }
}
