using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EduProctor.Services.Interfaces;
using EduProctor.Shared.DTOs;

namespace EduProctor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SuperAdmin")]
public class OrganizationController : ControllerBase
{
    private readonly IOrganizationService _orgService;
    private readonly ILogger<OrganizationController> _logger;

    public OrganizationController(IOrganizationService orgService, ILogger<OrganizationController> logger)
    {
        _orgService = orgService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var orgs = await _orgService.GetAllAsync();
        return Ok(new { success = true, data = orgs, count = orgs.Count });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var org = await _orgService.GetByIdAsync(id);
        if (org == null)
            return NotFound(new { success = false, message = "Organization topilmadi" });

        return Ok(new { success = true, data = org });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrganizationDto dto)
    {
        var org = await _orgService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = org.Id }, new { success = true, data = org });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateOrganizationDto dto)
    {
        var org = await _orgService.UpdateAsync(id, dto);
        return Ok(new { success = true, data = org });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _orgService.DeleteAsync(id);
        return Ok(new { success = true, message = "Organization o'chirildi" });
    }
}