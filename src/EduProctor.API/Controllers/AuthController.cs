using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using EduProctor.Services;
using EduProctor.Shared;

namespace EduProctor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Tizimga kirish (Login)
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(new { errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)) });

            var result = await _authService.LoginAsync(dto);

            // Refresh tokenni HTTP-only cookie ga saqlash
            SetRefreshTokenCookie(result.RefreshToken);

            return Ok(new
            {
                success = true,
                message = "Tizimga muvaffaqiyatli kirdingiz",
                data = new
                {
                    result.AccessToken,
                    result.ExpiresIn,
                    result.User
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login xatosi: {Email}", dto.Email);
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Yangi foydalanuvchi ro'yxatdan o'tkazish (Register)
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(new { errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)) });

            var result = await _authService.RegisterAsync(dto);
            SetRefreshTokenCookie(result.RefreshToken);

            return StatusCode(201, new
            {
                success = true,
                message = "Foydalanuvchi muvaffaqiyatli ro'yxatdan o'tdi",
                data = new
                {
                    result.AccessToken,
                    result.ExpiresIn,
                    result.User
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Register xatosi: {Email}", dto.Email);
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Token yangilash (Refresh Token)
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh()
    {
        try
        {
            var refreshToken = Request.Cookies["refreshToken"];
            if (string.IsNullOrEmpty(refreshToken))
                return BadRequest(new { success = false, message = "Refresh token topilmadi" });

            var result = await _authService.RefreshTokenAsync(refreshToken);
            SetRefreshTokenCookie(result.RefreshToken);

            return Ok(new
            {
                success = true,
                message = "Token muvaffaqiyatli yangilandi",
                data = new
                {
                    result.AccessToken,
                    result.ExpiresIn,
                    result.User
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Refresh token xatosi");
            return Unauthorized(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Tizimdan chiqish (Logout)
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        try
        {
            var userId = GetCurrentUserId();
            await _authService.LogoutAsync(userId);

            // Cookie ni o'chirish
            Response.Cookies.Delete("refreshToken");

            return Ok(new
            {
                success = true,
                message = "Tizimdan muvaffaqiyatli chiqdingiz"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Logout xatosi");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Joriy foydalanuvchi ma'lumotlarini olish
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        try
        {
            var userId = GetCurrentUserId();
            var user = await _authService.GetCurrentUserAsync(userId);

            return Ok(new
            {
                success = true,
                data = user
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetCurrentUser xatosi");
            return NotFound(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Parolni o'zgartirish
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(new { errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)) });

            var userId = GetCurrentUserId();
            await _authService.ChangePasswordAsync(userId, dto);

            return Ok(new
            {
                success = true,
                message = "Parol muvaffaqiyatli o'zgartirildi"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ChangePassword xatosi");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Joriy foydalanuvchi ID sini olish
    /// </summary>
    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
            throw new Exception("Foydalanuvchi topilmadi");

        return Guid.Parse(userIdClaim);
    }

    /// <summary>
    /// Refresh tokenni HTTP-only cookie ga saqlash
    /// </summary>
    private void SetRefreshTokenCookie(string refreshToken)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddDays(7)
        };

        Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
    }
}