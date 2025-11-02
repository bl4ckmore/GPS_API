using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
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

    // GET /api/carts (alias)
    [HttpGet]
    public Task<IActionResult> GetMyCartAlias(CancellationToken ct) => GetMyCart(ct);

    // GET /api/carts/me
    [HttpGet("me")]
    public async Task<IActionResult> GetMyCart(CancellationToken ct)
    {
        var userId = await ResolveUserIdAsync(ct);
        if (userId == Guid.Empty)
            return Unauthorized(new { error = "Cannot resolve user from JWT" });

        var cart = await _db.Carts.AsNoTracking().FirstOrDefaultAsync(c => c.UserId == userId, ct);
        if (cart is null) return Ok(new CartViewDto());

        var items = await _db.CartItems
            .Where(ci => ci.CartId == cart.id)
            .Join(_db.Products, ci => ci.ProductId, p => p.id, (ci, p) => new { ci, p })
            .Select(x => new CartItemViewDto
            {
                id = x.ci.id,
                quantity = x.ci.qty, // <-- entity uses `qty`
                product = new ProductBriefDto
                {
                    id = x.p.id,
                    name = x.p.Name,
                    title = x.p.Name,             // legacy alias
                    sku = x.p.SKU,
                    price = x.p.Price,            // non-nullable decimal
                    imageUrl = x.p.ImageUrl ?? "",
                    mainImageUrl = x.p.ImageUrl ?? "", // legacy alias
                    category = x.p.Category != null ? x.p.Category.Name : null,
                    type = x.p.Type,
                    stock = (int?)(x.p.Stock) ?? 0
                }
            })
            .ToListAsync(ct);

        var dto = new CartViewDto { id = cart.id, items = items };
        dto.RecalculateTotals();
        return Ok(dto);
    }

    // GET /api/carts/all
    [HttpGet("all")]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var all = await _carts.GetAllAsync(ct);
        return Ok(all);
    }

    // GET /api/carts/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var cart = await _carts.GetByIdAsync(id, ct);
        return cart is null ? NotFound() : Ok(cart);
    }

    // POST /api/carts/items
    [HttpPost("items")]
    public async Task<IActionResult> AddItem([FromBody] AddItemRequestDto body, CancellationToken ct)
    {
        if (body is null) return BadRequest(new { error = "Body required" });
        if (body.ProductId == Guid.Empty) return BadRequest(new { error = "productId invalid" });

        var qtyToAdd = body.Quantity ?? body.Qty ?? 0;
        if (qtyToAdd <= 0) return BadRequest(new { error = "quantity must be > 0" });

        var userId = await ResolveUserIdAsync(ct);
        if (userId == Guid.Empty) return Unauthorized(new { error = "Cannot resolve user from JWT" });

        var cart = await _db.Carts.FirstOrDefaultAsync(c => c.UserId == userId, ct);
        if (cart is null)
        {
            cart = new Cart { id = Guid.NewGuid(), CreatedAt = DateTime.UtcNow, UserId = userId };
            await _db.Carts.AddAsync(cart, ct);
        }

        var existing = await _db.CartItems
            .FirstOrDefaultAsync(i => i.CartId == cart.id && i.ProductId == body.ProductId, ct);

        if (existing is not null)
        {
            existing.qty += qtyToAdd; // <-- use qty
        }
        else
        {
            await _db.CartItems.AddAsync(new CartItem
            {
                id = Guid.NewGuid(),
                CartId = cart.id,
                ProductId = body.ProductId,
                qty = qtyToAdd
            }, ct);
        }

        await _db.SaveChangesAsync(ct);
        var cartDto = await BuildCartForUser(userId, ct);
        return Ok(new { message = "Added to cart", cart = cartDto });
    }

    // PATCH /api/carts/items/{itemId}
    [HttpPatch("items/{itemId:guid}")]
    public async Task<IActionResult> UpdateItem(Guid itemId, [FromBody] UpdateQtyDto body, CancellationToken ct)
    {
        if (body is null) return BadRequest(new { error = "Body required" });
        var newQty = body.Quantity ?? body.Qty ?? 0;
        if (newQty <= 0) return BadRequest(new { error = "quantity must be > 0" });

        var userId = await ResolveUserIdAsync(ct);
        if (userId == Guid.Empty) return Unauthorized(new { error = "Cannot resolve user from JWT" });

        var item = await _db.CartItems
            .Join(_db.Carts, ci => ci.CartId, c => c.id, (ci, c) => new { ci, c })
            .Where(x => x.ci.id == itemId && x.c.UserId == userId)
            .Select(x => x.ci)
            .FirstOrDefaultAsync(ct);

        if (item is null) return NotFound();

        item.qty = newQty; // <-- use qty
        await _db.SaveChangesAsync(ct);

        var cartDto = await BuildCartForUser(userId, ct);
        return Ok(new { message = "Quantity updated", cart = cartDto });
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

        var cartDto = await BuildCartForUser(userId, ct);
        return Ok(new { message = "Removed from cart", cart = cartDto });
    }

    // DELETE /api/carts (clear)
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
            return Ok(new { message = "Cart is already empty", cart = new CartViewDto() });

        var items = _db.CartItems.Where(i => i.CartId == cartId);
        _db.CartItems.RemoveRange(items);
        await _db.SaveChangesAsync(ct);

        return Ok(new { message = "Cart cleared", cart = new CartViewDto { id = cartId } });
    }

    // ---------------- helpers ----------------

    private async Task<CartViewDto> BuildCartForUser(Guid userId, CancellationToken ct)
    {
        var cart = await _db.Carts.AsNoTracking().FirstOrDefaultAsync(c => c.UserId == userId, ct);
        if (cart is null) return new CartViewDto();

        var items = await _db.CartItems
            .Where(ci => ci.CartId == cart.id)
            .Join(_db.Products, ci => ci.ProductId, p => p.id, (ci, p) => new { ci, p })
            .Select(x => new CartItemViewDto
            {
                id = x.ci.id,
                quantity = x.ci.qty, // <-- use qty
                product = new ProductBriefDto
                {
                    id = x.p.id,
                    name = x.p.Name,
                    title = x.p.Name,
                    sku = x.p.SKU,
                    price = x.p.Price,                 // non-null
                    imageUrl = x.p.ImageUrl ?? "",
                    mainImageUrl = x.p.ImageUrl ?? "",
                    category = x.p.Category != null ? x.p.Category.Name : null,
                    type = x.p.Type,
                    stock = (int?)(x.p.Stock) ?? 0
                }
            })
            .ToListAsync(ct);

        var dto = new CartViewDto { id = cart.id, items = items };
        dto.RecalculateTotals();
        return dto;
    }

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

// ---------------- DTOs + totals ----------------

public sealed class AddItemRequestDto
{
    [Required] public Guid ProductId { get; set; }
    public int? Quantity { get; set; } // accept both names
    public int? Qty { get; set; }
}

public sealed class UpdateQtyDto
{
    public int? Quantity { get; set; }
    public int? Qty { get; set; }
}

public sealed class CartViewDto
{
    public Guid id { get; set; }
    public List<CartItemViewDto> items { get; set; } = new();

    public decimal subtotal { get; set; }
    public decimal tax { get; set; }
    public decimal shipping { get; set; }
    public decimal total { get; set; }

    public void RecalculateTotals(decimal taxRate = 0m, decimal shippingFlat = 0m)
    {
        subtotal = items.Sum(i => (i.product?.price ?? 0m) * i.quantity);
        tax = Math.Round(subtotal * taxRate, 2);
        shipping = items.Any() ? shippingFlat : 0m;
        total = subtotal + tax + shipping;
    }
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

    // legacy aliases for existing Angular bindings
    public string? title { get; set; }
    public string? sku { get; set; }
    public string? mainImageUrl { get; set; }
}
