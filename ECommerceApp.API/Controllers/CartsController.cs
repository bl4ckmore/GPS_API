using ECommerceApp.Application.DTOs;
using ECommerceApp.Core.Entities;
using ECommerceApp.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;


namespace ECommerceApp.API.Controllers;

[ApiController]
[Route("api/cart")] // FIX 1: Fixes the 404 by matching Angular's /api/cart
public class CartsController : ControllerBase
{
    // The DI container will now know how to inject this due to the Program.cs fix
    private readonly IGenericRepository<Cart> _carts;

    public CartsController(IGenericRepository<Cart> carts)
    {
        _carts = carts;
    }

    [HttpGet] // Now resolves to: GET /api/cart
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(await _carts.GetAllAsync(ct));

    [HttpGet("{id:guid}")] // Resolves to: GET /api/cart/{id}
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var cart = await _carts.GetByIdAsync(id, ct);
        return cart is null ? NotFound() : Ok(cart);
    }

    [HttpPost] // Now resolves to: POST /api/cart
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

    [HttpPut("{id}")] // Resolves to: PUT /api/cart/{id}
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

    [HttpDelete("{id}")] // Resolves to: DELETE /api/cart/{id}
    public async Task<IActionResult> Delete(int id)
    {
        // TODO: delete logic
        bool deleted = true; // placeholder

        if (!deleted)
            return NotFound();

        return NoContent(); // Correct status for a successful delete
    }
}