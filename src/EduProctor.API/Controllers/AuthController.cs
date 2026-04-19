using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using EduProctor.Services.Interfaces;
using EduProctor.Shared.DTOs;
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
            // 1. Ma'lumotlarni tekshirish
            if (!ModelState.IsValid)
                return BadRequest(new
                {
                    success = false,
                    message = "Ma'lumotlar to'liq emas",
                    errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                });

            // 2. Login qilish
            var result = await _authService.LoginAsync(dto);

            // 3. Refresh tokenni HTTP-only cookie ga saqlash (xavfsizroq)
            SetRefreshTokenCookie(result.RefreshToken);

            // 4. Natijani qaytarish (Access token va user ma'lumotlari)
            return Ok(new
            {
                success = true,
                message = "Tizimga muvaffaqiyatli kirdingiz",
                data = new
                {
                    accessToken = result.AccessToken,
                    expiresIn = result.ExpiresIn,
                    user = result.User
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login xatosi: {Email}", dto.Email);
            return BadRequest(new
            {
                success = false,
                message = ex.Message
            });
        }
    }

    /// <summary>
    /// Ro'yxatdan o'tish (Register)
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        try
        {
            // 1. Ma'lumotlarni tekshirish
            if (!ModelState.IsValid)
                return BadRequest(new
                {
                    success = false,
                    message = "Ma'lumotlar to'liq emas",
                    errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                });

            // 2. Parol uzunligini tekshirish
            if (dto.Password.Length < 6)
                return BadRequest(new
                {
                    success = false,
                    message = "Parol kamida 6 belgidan iborat bo'lishi kerak"
                });

            // 3. Ro'yxatdan o'tkazish
            var result = await _authService.RegisterAsync(dto);

            // 4. Refresh tokenni cookie ga saqlash
            SetRefreshTokenCookie(result.RefreshToken);

            // 5. Natijani qaytarish
            return StatusCode(201, new
            {
                success = true,
                message = "Foydalanuvchi muvaffaqiyatli ro'yxatdan o'tdi",
                data = new
                {
                    accessToken = result.AccessToken,
                    expiresIn = result.ExpiresIn,
                    user = result.User
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Register xatosi: {Email}", dto.Email);
            return BadRequest(new
            {
                success = false,
                message = ex.Message
            });
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
            // 1. Cookie dan refresh tokenni olish
            var refreshToken = Request.Cookies["refreshToken"];
            if (string.IsNullOrEmpty(refreshToken))
                return BadRequest(new
                {
                    success = false,
                    message = "Refresh token topilmadi. Qaytadan kiring."
                });

            // 2. Token yangilash
            var result = await _authService.RefreshTokenAsync(refreshToken);

            // 3. Yangi refresh tokenni cookie ga saqlash
            SetRefreshTokenCookie(result.RefreshToken);

            // 4. Natijani qaytarish
            return Ok(new
            {
                success = true,
                message = "Token muvaffaqiyatli yangilandi",
                data = new
                {
                    accessToken = result.AccessToken,
                    expiresIn = result.ExpiresIn,
                    user = result.User
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Refresh token xatosi");
            return Unauthorized(new
            {
                success = false,
                message = ex.Message
            });
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
            // 1. Joriy foydalanuvchi ID sini olish
            var userId = GetCurrentUserId();

            // 2. Logout qilish
            await _authService.LogoutAsync(userId);

            // 3. Cookie ni o'chirish
            Response.Cookies.Delete("refreshToken");

            // 4. Natijani qaytarish
            return Ok(new
            {
                success = true,
                message = "Tizimdan muvaffaqiyatli chiqdingiz"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Logout xatosi");
            return BadRequest(new
            {
                success = false,
                message = ex.Message
            });
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
            // 1. Joriy foydalanuvchi ID sini olish
            var userId = GetCurrentUserId();

            // 2. Foydalanuvchi ma'lumotlarini olish
            var user = await _authService.GetCurrentUserAsync(userId);

            // 3. Natijani qaytarish
            return Ok(new
            {
                success = true,
                data = user
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetCurrentUser xatosi");
            return NotFound(new
            {
                success = false,
                message = ex.Message
            });
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
            // 1. Ma'lumotlarni tekshirish
            if (!ModelState.IsValid)
                return BadRequest(new
                {
                    success = false,
                    message = "Ma'lumotlar to'liq emas",
                    errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                });

            // 2. Joriy foydalanuvchi ID sini olish
            var userId = GetCurrentUserId();

            // 3. Parolni o'zgartirish
            await _authService.ChangePasswordAsync(userId, dto);

            // 4. Natijani qaytarish
            return Ok(new
            {
                success = true,
                message = "Parol muvaffaqiyatli o'zgartirildi. Qaytadan kiring."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ChangePassword xatosi");
            return BadRequest(new
            {
                success = false,
                message = ex.Message
            });
        }
    }

    // ==================== PRIVATE METHODS ====================

    /// <summary>
    /// Joriy foydalanuvchi ID sini tokendan olish
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
            HttpOnly = true,              // JavaScript bilan o'qib bo'lmaydi (xavfsiz)
            Secure = true,               // Faqat HTTPS da yuboriladi
            SameSite = SameSiteMode.Strict, // CSRF himoyasi
            Expires = DateTime.UtcNow.AddDays(7) // 7 kun umr
        };

        Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
    }
}