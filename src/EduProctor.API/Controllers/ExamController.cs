using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using EduProctor.Services.Interfaces;
using EduProctor.Shared.DTOs;

namespace EduProctor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ExamController : ControllerBase
{
    private readonly IExamService _examService;
    private readonly ILogger<ExamController> _logger;

    public ExamController(IExamService examService, ILogger<ExamController> logger)
    {
        _examService = examService;
        _logger = logger;
    }

    /// <summary>
    /// Imtihonni boshlash (Student)
    /// </summary>
    [HttpPost("start")]
    public async Task<IActionResult> StartExam([FromBody] StartExamDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var session = await _examService.StartExamAsync(userId, dto);

            return Ok(new
            {
                success = true,
                message = "Imtihon boshlandi",
                data = session
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "StartExam xatosi");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Javob yuborish (Student)
    /// </summary>
    [HttpPost("{sessionId}/answer")]
    public async Task<IActionResult> SubmitAnswer(Guid sessionId, [FromBody] SubmitAnswerDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _examService.SubmitAnswerAsync(sessionId, userId, dto);

            return Ok(new
            {
                success = true,
                message = "Javob qabul qilindi"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SubmitAnswer xatosi: {SessionId}", sessionId);
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Imtihonni topshirish (Student)
    /// </summary>
    [HttpPost("{sessionId}/submit")]
    public async Task<IActionResult> SubmitExam(Guid sessionId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _examService.SubmitExamAsync(sessionId, userId);

            return Ok(new
            {
                success = true,
                message = "Imtihon topshirildi",
                data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SubmitExam xatosi: {SessionId}", sessionId);
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Imtihon natijasini ko'rish (Student/Admin)
    /// </summary>
    [HttpGet("{sessionId}/result")]
    public async Task<IActionResult> GetResult(Guid sessionId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _examService.GetExamResultAsync(sessionId, userId);

            return Ok(new
            {
                success = true,
                data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetResult xatosi: {SessionId}", sessionId);
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Faol imtihon sessiyasini olish (Student)
    /// </summary>
    [HttpGet("current/{testId}")]
    public async Task<IActionResult> GetCurrentSession(Guid testId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var session = await _examService.GetCurrentSessionAsync(userId, testId);

            return Ok(new
            {
                success = true,
                data = session
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetCurrentSession xatosi");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Imtihon savollarini olish (Student)
    /// </summary>
    [HttpGet("{sessionId}/questions")]
    public async Task<IActionResult> GetQuestions(Guid sessionId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var questions = await _examService.GetExamQuestionsAsync(sessionId, userId);

            return Ok(new
            {
                success = true,
                data = questions
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetQuestions xatosi: {SessionId}", sessionId);
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Imtihon faolligini tekshirish (Student)
    /// </summary>
    [HttpGet("{sessionId}/active")]
    public async Task<IActionResult> IsActive(Guid sessionId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var isActive = await _examService.IsExamActiveAsync(sessionId, userId);

            return Ok(new
            {
                success = true,
                data = new { isActive }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "IsActive xatosi: {SessionId}", sessionId);
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
            throw new UnauthorizedAccessException("Foydalanuvchi topilmadi");

        return Guid.Parse(userIdClaim);
    }
}