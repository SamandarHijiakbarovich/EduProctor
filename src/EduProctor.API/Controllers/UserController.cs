using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using EduProctor.Services.Interfaces;
using EduProctor.Shared.DTOs;
using EduProctor.Shared;

namespace EduProctor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UserController> _logger;

    public UserController(IUserService userService, ILogger<UserController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Barcha foydalanuvchilar ro'yxati (Admin/SuperAdmin)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> GetAll()
    {
        var organizationId = GetOrganizationId();
        var users = await _userService.GetAllAsync(organizationId);

        return Ok(new { success = true, data = users, count = users.Count });
    }

    /// <summary>
    /// Foydalanuvchini ID bo'yicha olish
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user == null)
            return NotFound(new { success = false, message = "Foydalanuvchi topilmadi" });

        // Faqat o'z tashkilotidagi foydalanuvchilarni ko'rish mumkin
        if (user.OrganizationId != GetOrganizationId() && !IsSuperAdmin())
            return Forbid();

        return Ok(new { success = true, data = user });
    }

    /// <summary>
    /// Yangi foydalanuvchi yaratish (Admin/SuperAdmin)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
    {
        try
        {
            // Admin yaratayotgan bo'lsa, o'z tashkilotiga biriktirish
            if (!IsSuperAdmin())
            {
                dto.OrganizationId = GetOrganizationId();
            }

            var user = await _userService.CreateAsync(dto);
            return StatusCode(201, new { success = true, data = user });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Foydalanuvchi ma'lumotlarini tahrirlash
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserDto dto)
    {
        try
        {
            var user = await _userService.UpdateAsync(id, dto);
            return Ok(new { success = true, data = user });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Foydalanuvchini o'chirish (Admin/SuperAdmin)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _userService.DeleteAsync(id);
        return Ok(new { success = true, message = "Foydalanuvchi o'chirildi" });
    }

    /// <summary>
    /// Foydalanuvchi statusini o'zgartirish (Block/Activate)
    /// </summary>
    [HttpPatch("{id}/status")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> ChangeStatus(Guid id, [FromBody] ChangeUserStatusDto dto)
    {
        try
        {
            await _userService.ChangeStatusAsync(id, dto);
            return Ok(new { success = true, message = $"Foydalanuvchi statusi {dto.Status} ga o'zgartirildi" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Rol bo'yicha foydalanuvchilar (Admin/SuperAdmin)
    /// </summary>
    [HttpGet("role/{role}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> GetByRole(string role)
    {
        var organizationId = GetOrganizationId();
        var users = await _userService.GetByRoleAsync(organizationId, role);

        return Ok(new { success = true, data = users, count = users.Count });
    }

    private Guid GetOrganizationId()
    {
        var orgIdClaim = User.FindFirst("org_id")?.Value;
        if (string.IsNullOrEmpty(orgIdClaim))
            throw new Exception("Organization ID topilmadi");

        return Guid.Parse(orgIdClaim);
    }

    private bool IsSuperAdmin()
    {
        return User.IsInRole("SuperAdmin");
    }
}