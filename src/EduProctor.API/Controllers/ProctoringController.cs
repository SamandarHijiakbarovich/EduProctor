using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using EduProctor.Services.Interfaces;
using EduProctor.Shared.DTOs;

namespace EduProctor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProctoringController : ControllerBase
{
    private readonly IProctoringService _proctoringService;
    private readonly ILogger<ProctoringController> _logger;

    public ProctoringController(IProctoringService proctoringService, ILogger<ProctoringController> logger)
    {
        _proctoringService = proctoringService;
        _logger = logger;
    }

    /// <summary>
    /// Sessiyadagi barcha proctoring hodisalarini olish (Admin/SuperAdmin/Student)
    /// </summary>
    [HttpGet("sessions/{sessionId}/events")]
    public async Task<IActionResult> GetSessionEvents(Guid sessionId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var role = GetCurrentUserRole();

            var events = await _proctoringService.GetSessionEventsAsync(sessionId, userId, role);

            return Ok(new
            {
                success = true,
                data = events,
                count = events.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetSessionEvents xatosi: {SessionId}", sessionId);
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Sessiyadagi qoidabuzarliklar statistikasini olish (Admin/SuperAdmin/Student)
    /// </summary>
    [HttpGet("sessions/{sessionId}/summary")]
    public async Task<IActionResult> GetViolationSummary(Guid sessionId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var role = GetCurrentUserRole();

            var summary = await _proctoringService.GetViolationSummaryAsync(sessionId, userId, role);

            return Ok(new
            {
                success = true,
                data = summary
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetViolationSummary xatosi: {SessionId}", sessionId);
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Faol studentlar ro'yxati (Admin/SuperAdmin)
    /// </summary>
    [HttpGet("active-students")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> GetActiveStudents()
    {
        try
        {
            var organizationId = GetOrganizationId();
            var students = await _proctoringService.GetActiveStudentsAsync(organizationId);

            return Ok(new
            {
                success = true,
                data = students,
                count = students.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetActiveStudents xatosi");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Studentni bloklash (Admin/SuperAdmin)
    /// </summary>
    [HttpPost("sessions/{sessionId}/block")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> BlockStudent(Guid sessionId, [FromBody] BlockStudentDto dto)
    {
        try
        {
            var organizationId = GetOrganizationId();
            await _proctoringService.BlockStudentAsync(sessionId, organizationId, dto.Reason);

            return Ok(new
            {
                success = true,
                message = $"Student bloklandi: {dto.Reason}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BlockStudent xatosi: {SessionId}", sessionId);
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Studentga ogohlantirish yuborish (Admin/SuperAdmin)
    /// </summary>
    [HttpPost("sessions/{sessionId}/warn")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> SendWarning(Guid sessionId, [FromBody] SendWarningDto dto)
    {
        try
        {
            await _proctoringService.SendWarningAsync(sessionId, dto.Message);

            return Ok(new
            {
                success = true,
                message = "Ogohlantirish yuborildi"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SendWarning xatosi: {SessionId}", sessionId);
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    // ==================== PRIVATE METHODS ====================

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
            throw new UnauthorizedAccessException("Foydalanuvchi topilmadi");

        return Guid.Parse(userIdClaim);
    }

    private string GetCurrentUserRole()
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        if (string.IsNullOrEmpty(role))
            throw new UnauthorizedAccessException("Role topilmadi");

        return role;
    }

    private Guid GetOrganizationId()
    {
        var orgIdClaim = User.FindFirst("org_id")?.Value;
        if (string.IsNullOrEmpty(orgIdClaim))
            throw new UnauthorizedAccessException("Organization ID topilmadi");

        return Guid.Parse(orgIdClaim);
    }
}

// DTO'lar
public class BlockStudentDto
{
    public string Reason { get; set; } = string.Empty;
}

public class SendWarningDto
{
    public string Message { get; set; } = string.Empty;
}