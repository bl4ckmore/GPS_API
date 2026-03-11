using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using ECommerceApp.Infrastructure.Data;
using ProductEntity = ECommerceApp.Core.Entities.Product;

namespace ECommerceApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ProductsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _env;

    public ProductsController(ApplicationDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    public sealed class ProductQuery
    {
        public string? page { get; set; }
        public string? pageSize { get; set; }
        public string? search { get; set; }
        public string? category { get; set; }
        public string? minPrice { get; set; }
        public string? maxPrice { get; set; }
        public string? sort { get; set; }
        public string? isFeatured { get; set; }
        public string? includeInactive { get; set; }
    }

    private static string? Norm(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        s = s.Trim();
        return (s.Equals("undefined", StringComparison.OrdinalIgnoreCase) ||
                s.Equals("null", StringComparison.OrdinalIgnoreCase)) ? null : s;
    }
    private static int ParseInt(string? s, int def, int min = int.MinValue, int max = int.MaxValue)
        => int.TryParse(Norm(s), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v)
            ? Math.Clamp(v, min, max) : def;
    private static decimal? ParseDec(string? s)
        => decimal.TryParse(Norm(s), NumberStyles.Number, CultureInfo.InvariantCulture, out var v)
            ? v : (decimal?)null;
    private static bool? ParseBool(string? s)
        => bool.TryParse(Norm(s), out var v) ? v : (bool?)null;

    // ===== LIST
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> List([FromQuery] ProductQuery q)
    {
        var page = ParseInt(q.page, 1, min: 1);
        var size = ParseInt(q.pageSize, 12, min: 1, max: 100);
        var search = Norm(q.search);
        var category = Norm(q.category)?.ToLowerInvariant();
        var minPrice = ParseDec(q.minPrice);
        var maxPrice = ParseDec(q.maxPrice);
        var sort = (Norm(q.sort) ?? "createdAt-desc").ToLowerInvariant();
        var isFeatured = ParseBool(q.isFeatured);

        // FIX 1: Removed '|| User.IsInRole("Admin")'. 
        // Now admins must explicitly request 'includeInactive=true' (which the Admin Panel does).
        bool includeInactive = string.Equals(Norm(q.includeInactive), "true", StringComparison.OrdinalIgnoreCase);

        IQueryable<ProductEntity> query = _db.Products.AsNoTracking();

        if (!includeInactive) query = query.Where(p => p.IsActive);
        if (isFeatured.HasValue) query = query.Where(p => p.IsFeatured == isFeatured.Value);
        if (!string.IsNullOrEmpty(category)) query = query.Where(p => (p.Type ?? "").ToLower() == category);

        if (!string.IsNullOrEmpty(search))
        {
            var s = search.ToLowerInvariant();
            query = query.Where(p =>
                p.Name.ToLower().Contains(s) ||
                ((p.Description ?? string.Empty).ToLower().Contains(s)) ||
                ((p.LongDescription ?? string.Empty).ToLower().Contains(s)));
        }

        if (minPrice.HasValue) query = query.Where(p => p.Price >= minPrice.Value);
        if (maxPrice.HasValue) query = query.Where(p => p.Price <= maxPrice.Value);

        query = sort switch
        {
            "name-asc" => query.OrderBy(p => p.Name),
            "name-desc" => query.OrderByDescending(p => p.Name),
            "price-asc" => query.OrderBy(p => p.Price),
            "price-desc" => query.OrderByDescending(p => p.Price),
            "createdat-asc" => query.OrderBy(p => p.CreatedAt),
            _ => query.OrderByDescending(p => p.CreatedAt)
        };

        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * size).Take(size).ToListAsync();

        return Ok(new { items, total, page, pageSize = size });
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> Get(Guid id, [FromQuery] bool includeInactive = false)
    {
        var q = _db.Products.AsNoTracking();

        // FIX: Consistent logic with List method
        if (!includeInactive) q = q.Where(x => x.IsActive);

        var p = await q.FirstOrDefaultAsync(x => x.id == id);
        return p is null ? NotFound() : Ok(p);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] ProductEntity body)
    {
        body.id = Guid.NewGuid();
        body.CreatedAt = DateTime.UtcNow;
        if (!body.IsActive) body.IsActive = true;
        await _db.Products.AddAsync(body);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = body.id }, body);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ProductEntity body)
    {
        var p = await _db.Products.FirstOrDefaultAsync(x => x.id == id);
        if (p is null) return NotFound();

        // FIX 2: Prevent Null Overwrites ("Smart Update")
        // Only update fields if they are provided (or if it's a full update, assume provided)
        // Since table toggle sends partial, we check for meaningful values where possible.

        if (body.Name != null) p.Name = body.Name;
        if (body.Type != null) p.Type = body.Type;
        if (body.Description != null) p.Description = body.Description;
        if (body.LongDescription != null) p.LongDescription = body.LongDescription;

        // For value types, we assume the client sends the current value if it's a full edit form.
        // But for toggles, we just want to flip one switch. 
        // However, C# API deserialization sets missing numbers to 0.
        // The robust frontend fix (sending FULL object) handles this, but this is a backup:

        // We blindly update these because the Frontend will now send the FULL object.
        p.Price = body.Price;
        p.CompareAtPrice = body.CompareAtPrice;
        p.SalePrice = body.SalePrice;
        p.Stock = body.Stock;
        p.ImageUrl = body.ImageUrl;
        p.VideoUrl = body.VideoUrl;
        p.Images = body.Images;
        p.Features = body.Features;
        p.Parameters = body.Parameters;
        p.Rating = body.Rating;
        p.ReviewCount = body.ReviewCount;

        // Flags
        p.IsActive = body.IsActive;
        p.IsFeatured = body.IsFeatured;

        if (body.CategoryId != null) p.CategoryId = body.CategoryId;

        p.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(p);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id, [FromQuery] bool force = false)
    {
        var p = await _db.Products.FirstOrDefaultAsync(x => x.id == id);
        if (p is null) return NotFound();

        var inOrders = await _db.OrderItems.AnyAsync(oi => oi.ProductId == id);

        if (inOrders && !force)
        {
            if (!p.IsActive)
            {
                return StatusCode(202, new { message = "Product is already archived.", isSoft = true });
            }
            p.IsActive = false;
            await _db.SaveChangesAsync();
            return StatusCode(202, new { message = "Product is in order history. Archived instead.", isSoft = true });
        }

        var cartItems = await _db.CartItems.Where(ci => ci.ProductId == id).ToListAsync();
        if (cartItems.Any()) _db.CartItems.RemoveRange(cartItems);

        if (inOrders && force)
        {
            var orderItems = await _db.OrderItems.Where(oi => oi.ProductId == id).ToListAsync();
            _db.OrderItems.RemoveRange(orderItems);
        }

        _db.Products.Remove(p);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ... (Count, Upload methods remain the same) ...
    [HttpGet("count")]
    [AllowAnonymous]
    public async Task<int> Count() => await _db.Products.CountAsync(p => p.IsActive);

    public sealed class UploadDto { public IFormFile? file { get; set; } }

    [HttpPost("upload")]
    [Authorize(Roles = "Admin")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> Upload([FromForm] UploadDto dto)
    {
        var file = dto.file;
        if (file is null || file.Length == 0) return BadRequest(new { error = "No file received" });

        var allowedExt = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExt.Contains(ext)) return BadRequest(new { error = "Unsupported file type" });

        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var y = DateTime.UtcNow.ToString("yyyy");
        var m = DateTime.UtcNow.ToString("MM");
        var dir = Path.Combine(webRoot, "uploads", "products", y, m);
        Directory.CreateDirectory(dir);

        var fileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}-{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(dir, fileName);

        await using (var fs = System.IO.File.Create(fullPath)) await file.CopyToAsync(fs);

        var pathBase = Request.PathBase.HasValue ? Request.PathBase.Value : string.Empty;
        var relPath = $"{pathBase}/uploads/products/{y}/{m}/{fileName}";
        var absoluteUrl = $"{Request.Scheme}://{Request.Host}{relPath}";

        return Created(absoluteUrl, new { url = absoluteUrl });
    }
}