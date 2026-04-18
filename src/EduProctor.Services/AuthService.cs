using EduProctor.Core.Entities;
using EduProctor.Core;
using EduProctor.Infrastructure.Data;
using EduProctor.Services.Settings;
using EduProctor.Shared;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace EduProctor.Services;

public class AuthService:IAuthService
{
    private readonly AppDbContext _context;
    private readonly JwtSettings _jwtSettings;
    private readonly IPasswordHasher _passwordHasher;

    public AuthService(
        AppDbContext context,
        IOptions<JwtSettings> jwtSettings,
        IPasswordHasher passwordHasher)
    {
        _context = context;
        _jwtSettings = jwtSettings.Value;
        _passwordHasher = passwordHasher;
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var user = await _context.Users
            .Include(u => u.Organization)
            .FirstOrDefaultAsync(u => u.Email == dto.Email);

        if (user == null)
            throw new Exception("Email yoki parol xato");

        if (!_passwordHasher.Verify(dto.Password, user.PasswordHash))
            throw new Exception("Email yoki parol xato");

        if (user.Status != UserStatus.Active)
            throw new Exception("Foydalanuvchi faol emas");

        var accessToken = GenerateAccessToken(user);
        var refreshToken = GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays);
        user.LastLoginAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = _jwtSettings.AccessTokenExpiryMinutes * 60,
            User = MapToUserDto(user)
        };
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
    {
        var existingUser = await _context.Users.AnyAsync(u => u.Email == dto.Email);
        if (existingUser)
            throw new Exception("Bu email allaqachon ro'yxatdan o'tgan");

        var role = dto.Role.ToLower() switch
        {
            "admin" => UserRole.Admin,
            "student" => UserRole.Student,
            _ => UserRole.Student
        };

        var user = new User
        {
            Email = dto.Email,
            PasswordHash = _passwordHasher.Hash(dto.Password),
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            OrganizationId = dto.OrganizationId,
            Role = role,
            Status = UserStatus.Active
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Yangilash uchun organization ni yuklaymiz
        await _context.Entry(user).Reference(u => u.Organization).LoadAsync();

        var accessToken = GenerateAccessToken(user);
        var refreshToken = GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays);
        await _context.SaveChangesAsync();

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = _jwtSettings.AccessTokenExpiryMinutes * 60,
            User = MapToUserDto(user)
        };
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(string refreshToken)
    {
        var user = await _context.Users
            .Include(u => u.Organization)
            .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);

        if (user == null || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            throw new Exception("Refresh token yaroqsiz");

        var newAccessToken = GenerateAccessToken(user);
        var newRefreshToken = GenerateRefreshToken();

        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays);
        await _context.SaveChangesAsync();

        return new AuthResponseDto
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            ExpiresIn = _jwtSettings.AccessTokenExpiryMinutes * 60,
            User = MapToUserDto(user)
        };
    }

    public async Task LogoutAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<UserDto> GetCurrentUserAsync(Guid userId)
    {
        var user = await _context.Users
            .Include(u => u.Organization)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            throw new Exception("Foydalanuvchi topilmadi");

        return MapToUserDto(user);
    }

    private string GenerateAccessToken(User user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("org_id", user.OrganizationId.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpiryMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

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

    public async Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordDto dto)
    {
        // 1. Foydalanuvchini topish
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            throw new Exception("Foydalanuvchi topilmadi");

        // 2. Hozirgi parolni tekshirish
        if (!_passwordHasher.Verify(dto.CurrentPassword, user.PasswordHash))
            throw new Exception("Joriy parol noto'g'ri");

        // 3. Yangi parolni hash qilish
        if (string.IsNullOrWhiteSpace(dto.NewPassword))
            throw new Exception("Yangi parol bo'sh bo'lishi mumkin emas");

        if (dto.NewPassword.Length < 6)
            throw new Exception("Yangi parol kamida 6 belgidan iborat bo'lishi kerak");

        // 4. Parolni yangilash
        user.PasswordHash = _passwordHasher.Hash(dto.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        // 5. Saqlash
        await _context.SaveChangesAsync();

        return true;
    }

}
