using Microsoft.AspNetCore.Mvc;
using ECommerceApp.Core.Interfaces;
using ECommerceApp.Core.Entities;
using System.Security.Claims;

namespace ECommerceApp.API.Controllers;

[ApiController]
[Route("api/[controller]")] // Resolves to: /api/Categories
public class CategoriesController : ControllerBase
{
    private readonly IGenericRepository<Category> _repo;

    public CategoriesController(IGenericRepository<Category> repo)
    {
        _repo = repo;
    }

    /// <summary>
    /// Get all categories
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Category>>> GetAll(CancellationToken ct)
    {
        var categories = await _repo.GetAllAsync(ct);
        return Ok(categories);
    }

    // FIX 2: Added action to handle GET /api/Categories/main 404
    /// <summary>
    /// Get main/featured categories
    /// </summary>
    [HttpGet("main")] // Maps to: GET /api/Categories/main
    public async Task<ActionResult<IEnumerable<Category>>> GetMainCategories(CancellationToken ct)
    {
        var allCategories = await _repo.GetAllAsync(ct);

        // TODO: Add actual filtering logic (e.g., .Where(c => c.IsFeatured))

        return Ok(allCategories.Take(5));
    }

    /// <summary>
    /// Get category by Id
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Category>> GetById(Guid id, CancellationToken ct)
    {
        var category = await _repo.GetByIdAsync(id, ct);
        if (category is null)
            return NotFound();

        return Ok(category);
    }

    /// <summary>
    /// Create a new category
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Category>> Create([FromBody] Category category, CancellationToken ct)
    {
        if (category is null)
            return BadRequest("Category cannot be null.");

        var created = await _repo.AddAsync(category, ct);
        // Ensure core entity has a property named 'id' that is a Guid
        return CreatedAtAction(nameof(GetById), new { id = created.id }, created);
    }

    /// <summary>
    /// Update an existing category
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] Category category, CancellationToken ct)
    {
        if (category is null)
            return BadRequest("Category cannot be null.");

        category.id = id;
        await _repo.UpdateAsync(category, ct);
        return NoContent();
    }

    /// <summary>
    /// Delete category by Id
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _repo.DeleteAsync(id, ct);
        return NoContent();
    }
}