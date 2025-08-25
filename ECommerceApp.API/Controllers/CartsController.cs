using ECommerceApp.Application.DTOs;
using ECommerceApp.Core.Entities;
using ECommerceApp.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CartsController : ControllerBase
{
    private readonly IGenericRepository<Cart> _carts;

    public CartsController(IGenericRepository<Cart> carts)
    {
        _carts = carts;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(await _carts.GetAllAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var cart = await _carts.GetByIdAsync(id, ct);
        return cart is null ? NotFound() : Ok(cart);
    }

    [HttpPost]
    public async Task<IActionResult> Create(ProductDto dto)
    {
        if (dto == null)
        {
            return BadRequest();
        }

        // TODO: call your service to create product
        var created = dto; // placeholder

        return Ok(created);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, ProductDto dto)
    {
        if (dto == null)
        {
            return BadRequest();
        }

        // TODO: update logic
        bool updated = true; // placeholder

        if (!updated)
            return NotFound();

        return Ok(dto);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        // TODO: delete logic
        bool deleted = true; // placeholder

        if (!deleted)
            return NotFound();

        return NoContent();
    }
}
