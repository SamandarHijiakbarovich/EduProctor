using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using EduProctor.Services.Interfaces;
using EduProctor.Shared.Dtos;

namespace EduProctor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TestController : ControllerBase
{
    private readonly ITestService _testService;
    private readonly ILogger<TestController> _logger;

    public TestController(ITestService testService, ILogger<TestController> logger)
    {
        _testService = testService;
        _logger = logger;
    }

    /// <summary>
    /// Barcha testlar ro'yxatini olish
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetTests([FromQuery] string? status = null)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var tests = await _testService.GetTestsAsync(organizationId, status);

            return Ok(new
            {
                success = true,
                data = tests,
                count = tests.Count
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetTests xatosi");
            return StatusCode(500, new { success = false, message = "Server xatosi" });
        }
    }

    /// <summary>
    /// Bitta testni ID bo'yicha olish
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetTestById(Guid id)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var test = await _testService.GetTestByIdAsync(id, organizationId);

            if (test == null)
                return NotFound(new { success = false, message = "Test topilmadi" });

            return Ok(new
            {
                success = true,
                data = test
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetTestById xatosi: {TestId}", id);
            return StatusCode(500, new { success = false, message = "Server xatosi" });
        }
    }

    /// <summary>
    /// Testni barcha savollari bilan olish
    /// </summary>
    [HttpGet("{id}/details")]
    public async Task<IActionResult> GetTestWithQuestions(Guid id)
    {
        try
        {
            var organizationId = GetOrganizationId();
            var test = await _testService.GetTestWithQuestionsAsync(id, organizationId);

            return Ok(new
            {
                success = true,
                data = test
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetTestWithQuestions xatosi: {TestId}", id);
            return NotFound(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Yangi test yaratish (Admin va SuperAdmin uchun)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateTest([FromBody] CreateTestDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(new
                {
                    success = false,
                    errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                });

            var organizationId = GetOrganizationId();
            var test = await _testService.CreateTestAsync(organizationId, dto);

            return CreatedAtAction(nameof(GetTestById), new { id = test.Id }, new
            {
                success = true,
                message = "Test muvaffaqiyatli yaratildi",
                data = test
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateTest xatosi");
            return StatusCode(500, new { success = false, message = "Server xatosi" });
        }
    }

    /// <summary>
    /// Testni tahrirlash (Admin va SuperAdmin uchun)
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTest(Guid id, [FromBody] UpdateTestDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(new
                {
                    success = false,
                    errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                });

            var organizationId = GetOrganizationId();
            var test = await _testService.UpdateTestAsync(id, organizationId, dto);

            return Ok(new
            {
                success = true,
                message = "Test muvaffaqiyatli tahrirlandi",
                data = test
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateTest xatosi: {TestId}", id);
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Testni o'chirish (Admin va SuperAdmin uchun)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTest(Guid id)
    {
        try
        {
            var organizationId = GetOrganizationId();
            await _testService.DeleteTestAsync(id, organizationId);

            return Ok(new
            {
                success = true,
                message = "Test muvaffaqiyatli o'chirildi"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeleteTest xatosi: {TestId}", id);
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Testni nashr qilish (Admin va SuperAdmin uchun)
    /// </summary>
    [HttpPost("{id}/publish")]
    public async Task<IActionResult> PublishTest(Guid id, [FromBody] PublishTestDto dto)
    {
        try
        {
            var organizationId = GetOrganizationId();
            await _testService.PublishTestAsync(id, organizationId, dto);

            return Ok(new
            {
                success = true,
                message = "Test muvaffaqiyatli nashr qilindi"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PublishTest xatosi: {TestId}", id);
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    // ==================== QUESTION ENDPOINTS ====================

    /// <summary>
    /// Testga yangi savol qo'shish
    /// </summary>
    [HttpPost("{testId}/questions")]
    public async Task<IActionResult> AddQuestion(Guid testId, [FromBody] CreateQuestionDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(new
                {
                    success = false,
                    errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                });

            var organizationId = GetOrganizationId();
            var question = await _testService.AddQuestionAsync(testId, organizationId, dto);

            return Ok(new
            {
                success = true,
                message = "Savol muvaffaqiyatli qo'shildi",
                data = question
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AddQuestion xatosi: {TestId}", testId);
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Savolni tahrirlash
    /// </summary>
    [HttpPut("questions/{questionId}")]
    public async Task<IActionResult> UpdateQuestion(Guid questionId, [FromQuery] Guid testId, [FromBody] CreateQuestionDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(new
                {
                    success = false,
                    errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                });

            var organizationId = GetOrganizationId();
            await _testService.UpdateQuestionAsync(questionId, testId, organizationId, dto);

            return Ok(new
            {
                success = true,
                message = "Savol muvaffaqiyatli tahrirlandi"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateQuestion xatosi: {QuestionId}", questionId);
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Savolni o'chirish
    /// </summary>
    [HttpDelete("questions/{questionId}")]
    public async Task<IActionResult> DeleteQuestion(Guid questionId, [FromQuery] Guid testId)
    {
        try
        {
            var organizationId = GetOrganizationId();
            await _testService.DeleteQuestionAsync(questionId, testId, organizationId);

            return Ok(new
            {
                success = true,
                message = "Savol muvaffaqiyatli o'chirildi"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeleteQuestion xatosi: {QuestionId}", questionId);
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    // ==================== PRIVATE METHODS ====================

    private Guid GetOrganizationId()
    {
        var orgIdClaim = User.FindFirst("org_id")?.Value;
        if (string.IsNullOrEmpty(orgIdClaim))
            throw new UnauthorizedAccessException("Organization ID topilmadi");

        return Guid.Parse(orgIdClaim);
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
            throw new UnauthorizedAccessException("Foydalanuvchi topilmadi");

        return Guid.Parse(userIdClaim);
    }
}