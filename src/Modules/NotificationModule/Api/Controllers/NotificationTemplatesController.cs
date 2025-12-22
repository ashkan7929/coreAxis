using CoreAxis.Modules.NotificationModule.Domain.Entities;
using CoreAxis.Modules.NotificationModule.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoreAxis.Modules.NotificationModule.Api.Controllers;

[ApiController]
[Route("api/notifications/templates")]
public class NotificationTemplatesController : ControllerBase
{
    private readonly NotificationDbContext _context;

    public NotificationTemplatesController(NotificationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await _context.Templates.ToListAsync());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var template = await _context.Templates.FindAsync(id);
        if (template == null) return NotFound();
        return Ok(template);
    }

    [HttpPost]
    public async Task<IActionResult> Create(NotificationTemplate template)
    {
        _context.Templates.Add(template);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = template.Id }, template);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, NotificationTemplate template)
    {
        if (id != template.Id) return BadRequest();

        _context.Entry(template).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
