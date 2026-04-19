using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using EduProctor.Core;
using EduProctor.Core.Entities;
using EduProctor.Infrastructure.Data;
using EduProctor.Services.Interfaces;
using EduProctor.Services.Settings;
using EduProctor.Shared.DTOs;
using EduProctor.Shared;

namespace EduProctor.Services;

public class AuthService : IAuthService
{
    // Dependency lar (bog'liq xizmatlar)
    private readonly AppDbContext _context;           // Database
    private readonly JwtSettings _jwtSettings;       // JWT sozlamalari
    private readonly IPasswordHasher _passwordHasher; // Parol hash qilish
    private readonly IBruteForceProtectionService _bruteForceService; // Brute force himoyasi

    // Constructor - dependency larni qabul qiladi
    public AuthService(
        AppDbContext context,
        IOptions<JwtSettings> jwtSettings,
        IPasswordHasher passwordHasher,
        IBruteForceProtectionService bruteForceService)
    {
        _context = context;
        _jwtSettings = jwtSettings.Value;
        _passwordHasher = passwordHasher;
        _bruteForceService = bruteForceService;
    }

    // ==================== LOGIN (Tizimga kirish) ====================
    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        // 1. Brute force tekshiruvi (5 marta xato urinish = blok)
        var blockKey = $"login:{dto.Email}";
        if (await _bruteForceService.IsBlockedAsync(blockKey))
            throw new Exception("Ko'p muvaffaqiyatsiz urinish. 15 daqiqadan keyin urinib ko'ring.");

        // 2. Foydalanuvchini email bo'yicha topish
        var user = await _context.Users
            .Include(u => u.Organization)  // Organization ni ham yuklash
            .FirstOrDefaultAsync(u => u.Email == dto.Email);

        // 3. Foydalanuvchi mavjudmi?
        if (user == null)
        {
            await _bruteForceService.RecordFailedAttemptAsync(blockKey);
            throw new Exception("Email yoki parol xato");
        }

        // 4. Parol to'g'rimi?
        if (!_passwordHasher.Verify(dto.Password, user.PasswordHash))
        {
            await _bruteForceService.RecordFailedAttemptAsync(blockKey);
            throw new Exception("Email yoki parol xato");
        }

        // 5. Foydalanuvchi faolmi? (Active, Blocked, Inactive)
        if (user.Status != UserStatus.Active)
            throw new Exception("Foydalanuvchi faol emas. Admin bilan bog'laning.");

        // 6. Muvaffaqiyatli login - brute force recordlarini tozalash
        await _bruteForceService.ResetAttemptsAsync(blockKey);

        // 7. Token yaratish
        var accessToken = GenerateAccessToken(user);
        var refreshToken = GenerateRefreshToken();

        // 8. Refresh tokenni bazaga saqlash
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays);
        user.LastLoginAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // 9. Natijani qaytarish
        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = _jwtSettings.AccessTokenExpiryMinutes * 60, // sekundlarda
            User = MapToUserDto(user)
        };
    }

    // ==================== REGISTER (Ro'yxatdan o'tish) ====================
    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
    {
        // 1. Email allaqachon ro'yxatdan o'tganmi?
        var existingUser = await _context.Users.AnyAsync(u => u.Email == dto.Email);
        if (existingUser)
            throw new Exception("Bu email allaqachon ro'yxatdan o'tgan");

        // 2. Rolni aniqlash (agar kelmagan bo'lsa Student qilib qo'yamiz)
        var role = dto.Role?.ToLower() switch
        {
            "admin" => UserRole.Admin,
            "superadmin" => UserRole.SuperAdmin,
            _ => UserRole.Student
        };

        // 3. Yangi foydalanuvchi yaratish
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = dto.Email,
            PasswordHash = _passwordHasher.Hash(dto.Password),
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            OrganizationId = dto.OrganizationId,
            Role = role,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        // 4. Bazaga saqlash
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // 5. Organization ma'lumotlarini yuklash (DTO uchun kerak)
        await _context.Entry(user).Reference(u => u.Organization).LoadAsync();

        // 6. Token yaratish
        var accessToken = GenerateAccessToken(user);
        var refreshToken = GenerateRefreshToken();

        // 7. Refresh tokenni saqlash
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays);
        await _context.SaveChangesAsync();

        // 8. Natijani qaytarish
        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = _jwtSettings.AccessTokenExpiryMinutes * 60,
            User = MapToUserDto(user)
        };
    }

    // ==================== REFRESH TOKEN (Token yangilash) ====================
    public async Task<AuthResponseDto> RefreshTokenAsync(string refreshToken)
    {
        // 1. Refresh token bo'yicha foydalanuvchini topish
        var user = await _context.Users
            .Include(u => u.Organization)
            .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);

        // 2. Token mavjudmi va muddati tugamaganmi?
        if (user == null || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            throw new Exception("Refresh token yaroqsiz. Qaytadan kirishingiz kerak.");

        // 3. Yangi tokenlar yaratish
        var newAccessToken = GenerateAccessToken(user);
        var newRefreshToken = GenerateRefreshToken();

        // 4. Yangi refresh tokenni saqlash
        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays);
        await _context.SaveChangesAsync();

        // 5. Natijani qaytarish
        return new AuthResponseDto
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            ExpiresIn = _jwtSettings.AccessTokenExpiryMinutes * 60,
            User = MapToUserDto(user)
        };
    }

    // ==================== LOGOUT (Tizimdan chiqish) ====================
    public async Task LogoutAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            // Refresh tokenni o'chirish
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
            await _context.SaveChangesAsync();
        }
    }

    // ==================== GET CURRENT USER (Joriy foydalanuvchi) ====================
    public async Task<UserDto> GetCurrentUserAsync(Guid userId)
    {
        var user = await _context.Users
            .Include(u => u.Organization)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            throw new Exception("Foydalanuvchi topilmadi");

        return MapToUserDto(user);
    }

    // ==================== CHANGE PASSWORD (Parolni o'zgartirish) ====================
    public async Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordDto dto)
    {
        // 1. Foydalanuvchini topish
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            throw new Exception("Foydalanuvchi topilmadi");

        // 2. Hozirgi parolni tekshirish
        if (!_passwordHasher.Verify(dto.CurrentPassword, user.PasswordHash))
            throw new Exception("Joriy parol noto'g'ri");

        // 3. Yangi parolni tekshirish
        if (string.IsNullOrWhiteSpace(dto.NewPassword))
            throw new Exception("Yangi parol bo'sh bo'lishi mumkin emas");

        if (dto.NewPassword.Length < 6)
            throw new Exception("Yangi parol kamida 6 belgidan iborat bo'lishi kerak");

        // 4. Parolni yangilash
        user.PasswordHash = _passwordHasher.Hash(dto.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        // 5. Refresh tokenni o'chirish (qayta login qilish kerak bo'ladi)
        user.RefreshToken = null;
        user.RefreshTokenExpiryTime = null;

        // 6. Bazaga saqlash
        await _context.SaveChangesAsync();

        return true;
    }

    // ==================== PRIVATE METHODS (Yordamchi metodlar) ====================

    // Access Token yaratish (15 daqiqa umrga ega)
    private string GenerateAccessToken(User user)
    {
        // Token ichidagi ma'lumotlar (claims)
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),  // User ID
            new Claim(ClaimTypes.Email, user.Email),                   // Email
            new Claim(ClaimTypes.Role, user.Role.ToString()),          // Rol (Admin, Student, SuperAdmin)
            new Claim("org_id", user.OrganizationId.ToString())        // Organization ID
        };

        // Secret key dan simmetrik kalit yaratish
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Token yaratish
        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,              // Token egasi (EduProctor)
            audience: _jwtSettings.Audience,          // Token qabul qiluvchi (EduProctorAPI)
            claims: claims,                           // Ma'lumotlar
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpiryMinutes), // Muddati
            signingCredentials: creds                 // Imzo
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // Refresh Token yaratish (tasodifiy 32 bayt)
    private string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    // User => UserDto ga o'tkazish
    private UserDto MapToUserDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role.ToString(),
            OrganizationId = user.OrganizationId,
            OrganizationName = user.Organization?.Name
        };
    }
}