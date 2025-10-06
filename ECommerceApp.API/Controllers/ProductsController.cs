using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using ECommerceApp.Infrastructure.Data;
// alias to avoid DTO name collisions
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

    // ===== list query shape
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
        public string? includeInactive { get; set; }   // NEW
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

        // Allow admins or explicit query flag
        bool includeInactive =
            string.Equals(Norm(q.includeInactive), "true", StringComparison.OrdinalIgnoreCase)
            || User.IsInRole("admin");

        IQueryable<ProductEntity> query = _db.Products.AsNoTracking();

        if (!includeInactive)
            query = query.Where(p => p.IsActive);

        if (isFeatured.HasValue)
            query = query.Where(p => p.IsFeatured == isFeatured.Value);

        if (!string.IsNullOrEmpty(category))
            query = query.Where(p => (p.Type ?? "").ToLower() == category);

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

        var items = await query
            .Skip((page - 1) * size)
            .Take(size)
            .Select(p => new
            {
                id = p.id,
                name = p.Name,
                type = p.Type,
                description = p.Description,
                longDescription = p.LongDescription,
                price = p.Price,
                compareAtPrice = p.CompareAtPrice,
                salePrice = p.SalePrice,
                stock = p.Stock,
                imageUrl = p.ImageUrl,
                images = p.Images,
                features = p.Features,
                parameters = p.Parameters,
                rating = p.Rating,
                reviewCount = p.ReviewCount,
                categoryId = p.CategoryId,
                isActive = p.IsActive,
                isFeatured = p.IsFeatured,
                createdAt = p.CreatedAt,
                updatedAt = p.UpdatedAt
            })
            .ToListAsync();

        return Ok(new { items, total, page, pageSize = size });
    }

    // ===== GET
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> Get(Guid id, [FromQuery] bool includeInactive = false)
    {
        var q = _db.Products.AsNoTracking();

        if (!(includeInactive || User.IsInRole("admin")))
            q = q.Where(x => x.IsActive);

        var p = await q.FirstOrDefaultAsync(x => x.id == id);
        return p is null ? NotFound() : Ok(p);
    }

    // ===== CREATE (admin)
    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Create([FromBody] ProductEntity body)
    {
        body.id = Guid.NewGuid();
        body.CreatedAt = DateTime.UtcNow;
        if (!body.IsActive) body.IsActive = true;

        await _db.Products.AddAsync(body);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = body.id }, body);
    }

    // ===== UPDATE (admin)
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ProductEntity body)
    {
        var p = await _db.Products.FirstOrDefaultAsync(x => x.id == id);
        if (p is null) return NotFound();

        p.Name = body.Name;
        p.Type = body.Type;
        p.Description = body.Description;
        p.LongDescription = body.LongDescription;
        p.Price = body.Price;
        p.CompareAtPrice = body.CompareAtPrice;
        p.SalePrice = body.SalePrice;
        p.Stock = body.Stock;
        p.ImageUrl = body.ImageUrl;
        p.Images = body.Images;
        p.Features = body.Features;
        p.Parameters = body.Parameters;
        p.Rating = body.Rating;
        p.ReviewCount = body.ReviewCount;
        p.IsActive = body.IsActive;
        p.IsFeatured = body.IsFeatured;
        p.CategoryId = body.CategoryId;
        p.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(p);
    }

    // ===== DELETE (admin)
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var p = await _db.Products.FirstOrDefaultAsync(x => x.id == id);
        if (p is null) return NotFound();

        _db.Products.Remove(p);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ===== COUNT
    [HttpGet("count")]
    [AllowAnonymous]
    public async Task<int> Count() => await _db.Products.CountAsync(p => p.IsActive);

    // ===== UPLOAD (admin)
    public sealed class UploadDto
    {
        public IFormFile? file { get; set; } // FormData key must be "file"
    }

    [HttpPost("upload")]
    [Authorize(Roles = "admin")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> Upload([FromForm] UploadDto dto)
    {
        var file = dto.file;
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "No file received" });

        var allowedExt = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExt.Contains(ext))
            return BadRequest(new { error = "Unsupported file type" });

        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var y = DateTime.UtcNow.ToString("yyyy");
        var m = DateTime.UtcNow.ToString("MM");
        var dir = Path.Combine(webRoot, "uploads", "products", y, m);
        Directory.CreateDirectory(dir);

        var baseName = Path.GetFileNameWithoutExtension(file.FileName);
        var safeBase = string.Join("-", baseName.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries)).Trim();
        if (string.IsNullOrWhiteSpace(safeBase)) safeBase = "image";

        var fileName = $"{safeBase}-{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(dir, fileName);

        await using (var fs = System.IO.File.Create(fullPath))
            await file.CopyToAsync(fs);

        var pathBase = Request.PathBase.HasValue ? Request.PathBase.Value : string.Empty;
        var relPath = $"{pathBase}/uploads/products/{y}/{m}/{fileName}";
        var absoluteUrl = $"{Request.Scheme}://{Request.Host}{relPath}";

        return Created(absoluteUrl, new { url = absoluteUrl, path = relPath, fileName, size = file.Length });
    }
}
