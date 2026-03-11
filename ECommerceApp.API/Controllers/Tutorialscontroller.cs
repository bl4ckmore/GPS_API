//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using ECommerceApp.Infrastructure.Data;
//using ECommerceApp.Core.Entities;

//namespace ECommerceApp.API.Controllers;

//[ApiController]
//[Route("api/[controller]")]
//[Produces("application/json")]
//public class TutorialsController : ControllerBase
//{
//    private readonly ApplicationDbContext _db;

//    public TutorialsController(ApplicationDbContext db)
//    {
//        _db = db;
//    }

//    // GET api/tutorials  — public, returns active videos ordered by SortOrder
//    [HttpGet]
//    [AllowAnonymous]
//    public async Task<IActionResult> List([FromQuery] bool includeInactive = false)
//    {
//        var query = _db.TutorialVideos.AsNoTracking();
//        if (!includeInactive) query = query.Where(v => v.IsActive);
//        var items = await query.OrderBy(v => v.SortOrder).ThenBy(v => v.CreatedAt).ToListAsync();
//        return Ok(items);
//    }

//    // GET api/tutorials/{id}
//    [HttpGet("{id:guid}")]
//    [AllowAnonymous]
//    public async Task<IActionResult> Get(Guid id)
//    {
//        var v = await _db.TutorialVideos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
//        return v is null ? NotFound() : Ok(v);
//    }

//    // POST api/tutorials — Admin only
//    [HttpPost]
//    [Authorize(Roles = "Admin")]
//    public async Task<IActionResult> Create([FromBody] TutorialVideo body)
//    {
//        body.Id = Guid.NewGuid();
//        body.CreatedAt = DateTime.UtcNow;
//        await _db.TutorialVideos.AddAsync(body);
//        await _db.SaveChangesAsync();
//        return CreatedAtAction(nameof(Get), new { id = body.Id }, body);
//    }

//    // PUT api/tutorials/{id} — Admin only
//    [HttpPut("{id:guid}")]
//    [Authorize(Roles = "Admin")]
//    public async Task<IActionResult> Update(Guid id, [FromBody] TutorialVideo body)
//    {
//        var v = await _db.TutorialVideos.FirstOrDefaultAsync(x => x.Id == id);
//        if (v is null) return NotFound();

//        v.Title = body.Title;
//        v.Description = body.Description;
//        v.Url = body.Url;
//        v.SortOrder = body.SortOrder;
//        v.IsActive = body.IsActive;
//        v.UpdatedAt = DateTime.UtcNow;

//        await _db.SaveChangesAsync();
//        return Ok(v);
//    }

//    // DELETE api/tutorials/{id} — Admin only
//    [HttpDelete("{id:guid}")]
//    [Authorize(Roles = "Admin")]
//    public async Task<IActionResult> Delete(Guid id)
//    {
//        var v = await _db.TutorialVideos.FirstOrDefaultAsync(x => x.Id == id);
//        if (v is null) return NotFound();
//        _db.TutorialVideos.Remove(v);
//        await _db.SaveChangesAsync();
//        return NoContent();
//    }
//}