using ECommerceApp.Application.DTOs;
using ECommerceApp.Core.Entities;
using ECommerceApp.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace ECommerceApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    // Example repo, inject your own
    private readonly IGenericRepository<Product> _repo;

    public ProductsController(IGenericRepository<Product> repo)
    {
        _repo = repo;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ProductDto dto, CancellationToken ct)
    {
        if (dto is null)
            return BadRequest("Product cannot be null.");

        // TODO: map DTO -> entity and save
        var entity = new Product
        {
            id = Guid.NewGuid(),
            Name = dto.Name,
            Price = dto.Price
        };

        var created = await _repo.AddAsync(entity, ct);

        return CreatedAtAction(nameof(GetById), new { id = created.id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ProductDto dto, CancellationToken ct)
    {
        if (dto is null)
            return BadRequest("Product cannot be null.");

        var existing = await _repo.GetByIdAsync(id, ct);
        if (existing is null)
            return NotFound();

        // TODO: update fields
        existing.Name = dto.Name;
        existing.Price = dto.Price;

        await _repo.UpdateAsync(existing, ct);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var existing = await _repo.GetByIdAsync(id, ct);
        if (existing is null)
            return NotFound();

        await _repo.DeleteAsync(id, ct);

        return NoContent();
    }

    // Example for GetById so CreatedAtAction works
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var product = await _repo.GetByIdAsync(id, ct);
        return product is null ? NotFound() : Ok(product);
    }
}