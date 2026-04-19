using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using EduProctor.Services.Interfaces;
using EduProctor.Shared.DTOs;

namespace EduProctor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class GroupController : ControllerBase
{
    private readonly IGroupService _groupService;
    private readonly ILogger<GroupController> _logger;

    public GroupController(IGroupService groupService, ILogger<GroupController> logger)
    {
        _groupService = groupService;
        _logger = logger;
    }

    // ==================== GROUP ENDPOINTS ====================

    /// <summary>
    /// Barcha guruhlar ro'yxati
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var organizationId = GetOrganizationId();
        var groups = await _groupService.GetAllAsync(organizationId);

        return Ok(new { success = true, data = groups, count = groups.Count });
    }

    /// <summary>
    /// Guruhni ID bo'yicha olish
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var organizationId = GetOrganizationId();
        var group = await _groupService.GetByIdAsync(id, organizationId);

        if (group == null)
            return NotFound(new { success = false, message = "Guruh topilmadi" });

        return Ok(new { success = true, data = group });
    }

    /// <summary>
    /// Guruhni a'zolari bilan birga olish
    /// </summary>
    [HttpGet("{id}/details")]
    public async Task<IActionResult> GetGroupWithMembers(Guid id)
    {
        var organizationId = GetOrganizationId();
        var group = await _groupService.GetGroupWithMembersAsync(id, organizationId);

        return Ok(new { success = true, data = group });
    }

    /// <summary>
    /// Yangi guruh yaratish
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateGroupDto dto)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var group = await _groupService.CreateAsync(organizationId, dto);

            return StatusCode(201, new { success = true, data = group });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Guruhni tahrirlash
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateGroupDto dto)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var group = await _groupService.UpdateAsync(id, organizationId, dto);

            return Ok(new { success = true, data = group });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Guruhni o'chirish
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var organizationId = GetOrganizationId();
            await _groupService.DeleteAsync(id, organizationId);

            return Ok(new { success = true, message = "Guruh o'chirildi" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    // ==================== MEMBER ENDPOINTS ====================

    /// <summary>
    /// Guruhga a'zo qo'shish
    /// </summary>
    [HttpPost("{id}/members")]
    public async Task<IActionResult> AddMember(Guid id, [FromBody] AddMemberDto dto)
    {
        try
        {
            var organizationId = GetOrganizationId();
            await _groupService.AddMemberAsync(id, organizationId, dto);

            return Ok(new { success = true, message = "A'zo qo'shildi" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Guruhdan a'zo o'chirish
    /// </summary>
    [HttpDelete("{id}/members/{userId}")]
    public async Task<IActionResult> RemoveMember(Guid id, Guid userId)
    {
        try
        {
            var organizationId = GetOrganizationId();
            await _groupService.RemoveMemberAsync(id, organizationId, userId);

            return Ok(new { success = true, message = "A'zo o'chirildi" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Guruh a'zolari ro'yxati
    /// </summary>
    [HttpGet("{id}/members")]
    public async Task<IActionResult> GetMembers(Guid id)
    {
        var organizationId = GetOrganizationId();
        var members = await _groupService.GetMembersAsync(id, organizationId);

        return Ok(new { success = true, data = members, count = members.Count });
    }

    // ==================== PRIVATE METHODS ====================

    private Guid GetOrganizationId()
    {
        var orgIdClaim = User.FindFirst("org_id")?.Value;
        if (string.IsNullOrEmpty(orgIdClaim))
            throw new Exception("Organization ID topilmadi");

        return Guid.Parse(orgIdClaim);
    }
}