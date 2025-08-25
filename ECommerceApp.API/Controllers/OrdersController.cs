using Microsoft.AspNetCore.Mvc;
using ECommerceApp.Core.Interfaces;
using ECommerceApp.Core.Entities;

namespace ECommerceApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IGenericRepository<Order> _repo;
    public OrdersController(IGenericRepository<Order> repo) => _repo = repo;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(await _repo.GetAllAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var order = await _repo.GetByIdAsync(id, ct);
        return order is null ? NotFound() : Ok(order);
    }

    // Simple create — adapt to your checkout logic later
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Order order, CancellationToken ct)
    {
        var created = await _repo.AddAsync(order, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.id }, created);
    }
}
