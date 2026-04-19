using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using EduProctor.Services.Interfaces;
using EduProctor.Shared.DTOs;

namespace EduProctor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;
    private readonly ILogger<AnalyticsController> _logger;

    public AnalyticsController(IAnalyticsService analyticsService, ILogger<AnalyticsController> logger)
    {
        _analyticsService = analyticsService;
        _logger = logger;
    }

    /// <summary>
    /// Dashboard statistikasi (SuperAdmin uchun)
    /// </summary>
    [HttpGet("dashboard")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> GetDashboardStats()
    {
        try
        {
            var stats = await _analyticsService.GetDashboardStatsAsync();

            return Ok(new
            {
                success = true,
                data = stats
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetDashboardStats xatosi");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Barcha tashkilotlar statistikasi (SuperAdmin uchun)
    /// </summary>
    [HttpGet("organizations")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> GetAllOrganizationsStats()
    {
        try
        {
            var stats = await _analyticsService.GetAllOrganizationsStatsAsync();

            return Ok(new
            {
                success = true,
                data = stats,
                count = stats.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetAllOrganizationsStats xatosi");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Tashkilot statistikasi (Admin va SuperAdmin uchun)
    /// </summary>
    [HttpGet("organizations/current")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> GetOrganizationStats()
    {
        try
        {
            var organizationId = GetOrganizationId();
            var stats = await _analyticsService.GetOrganizationStatsAsync(organizationId);

            return Ok(new
            {
                success = true,
                data = stats
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetOrganizationStats xatosi");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Testlar statistikasi (Admin uchun)
    /// </summary>
    [HttpGet("tests")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> GetTestsStats()
    {
        try
        {
            var organizationId = GetOrganizationId();
            var stats = await _analyticsService.GetTestsStatsAsync(organizationId);

            return Ok(new
            {
                success = true,
                data = stats,
                count = stats.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetTestsStats xatosi");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Bitta test statistikasi (Admin uchun)
    /// </summary>
    [HttpGet("tests/{testId}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> GetTestStats(Guid testId)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var stats = await _analyticsService.GetTestStatsAsync(testId, organizationId);

            return Ok(new
            {
                success = true,
                data = stats
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetTestStats xatosi: {TestId}", testId);
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Studentlar statistikasi (Admin uchun)
    /// </summary>
    [HttpGet("students")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> GetStudentsPerformance()
    {
        try
        {
            var organizationId = GetOrganizationId();
            var stats = await _analyticsService.GetStudentsPerformanceAsync(organizationId);

            return Ok(new
            {
                success = true,
                data = stats,
                count = stats.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetStudentsPerformance xatosi");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Bitta student statistikasi (Admin uchun)
    /// </summary>
    [HttpGet("students/{studentId}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> GetStudentPerformance(Guid studentId)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var stats = await _analyticsService.GetStudentPerformanceAsync(studentId, organizationId);

            return Ok(new
            {
                success = true,
                data = stats
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetStudentPerformance xatosi: {StudentId}", studentId);
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Kunlik statistika (Admin uchun)
    /// </summary>
    [HttpGet("daily")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> GetDailyStats([FromQuery] int days = 30)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var stats = await _analyticsService.GetDailyStatsAsync(organizationId, days);

            return Ok(new
            {
                success = true,
                data = stats
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetDailyStats xatosi");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Qoidabuzarlik trendlari (Admin uchun)
    /// </summary>
    [HttpGet("violation-trends")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> GetViolationTrends([FromQuery] int days = 30)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var trends = await _analyticsService.GetViolationTrendsAsync(organizationId, days);

            return Ok(new
            {
                success = true,
                data = trends
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetViolationTrends xatosi");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Hisobot eksport qilish (Admin/SuperAdmin)
    /// </summary>
    [HttpPost("export")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> ExportReport([FromBody] ExportReportDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var role = GetCurrentUserRole();

            var report = await _analyticsService.ExportReportAsync(dto, userId, role);

            return File(report, "application/json", $"report_{DateTime.Now:yyyyMMdd}.json");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ExportReport xatosi");
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