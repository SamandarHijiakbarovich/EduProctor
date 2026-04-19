using Microsoft.EntityFrameworkCore;
using EduProctor.Core;
using EduProctor.Core.Entities;
using EduProctor.Infrastructure.Data;
using EduProctor.Services.Interfaces;
using EduProctor.Shared.DTOs;
using EduProctor.Shared;

namespace EduProctor.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _context;
    private readonly IPasswordHasher _passwordHasher;

    public UserService(AppDbContext context, IPasswordHasher passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public async Task<UserResponseDto> CreateAsync(CreateUserDto dto)
    {
        // Email mavjudligini tekshirish
        var existingUser = await _context.Users.AnyAsync(u => u.Email == dto.Email);
        if (existingUser)
            throw new Exception("Bu email allaqachon ro'yxatdan o'tgan");

        // Rolni aniqlash
        var role = dto.Role.ToLower() switch
        {
            "admin" => UserRole.Admin,
            "superadmin" => UserRole.SuperAdmin,
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
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Organization ni yuklash
        await _context.Entry(user).Reference(u => u.Organization).LoadAsync();

        return await MapToResponseAsync(user);
    }

    public async Task<UserResponseDto> UpdateAsync(Guid id, UpdateUserDto dto)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            throw new Exception("Foydalanuvchi topilmadi");

        user.FirstName = dto.FirstName;
        user.LastName = dto.LastName;
        user.AvatarUrl = dto.AvatarUrl;
        user.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(dto.Status))
            user.Status = Enum.Parse<UserStatus>(dto.Status);

        if (!string.IsNullOrEmpty(dto.Role))
            user.Role = Enum.Parse<UserRole>(dto.Role);

        await _context.SaveChangesAsync();
        await _context.Entry(user).Reference(u => u.Organization).LoadAsync();

        return await MapToResponseAsync(user);
    }

    public async Task DeleteAsync(Guid id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user != null)
        {
            user.IsDeleted = true;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<UserResponseDto?> GetByIdAsync(Guid id)
    {
        var user = await _context.Users
            .Include(u => u.Organization)
            .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);

        return user == null ? null : await MapToResponseAsync(user);
    }

    public async Task<List<UserResponseDto>> GetAllAsync(Guid organizationId)
    {
        var users = await _context.Users
            .Include(u => u.Organization)
            .Where(u => u.OrganizationId == organizationId && !u.IsDeleted)
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();

        var result = new List<UserResponseDto>();
        foreach (var user in users)
        {
            result.Add(await MapToResponseAsync(user));
        }
        return result;
    }

    public async Task<List<UserResponseDto>> GetByRoleAsync(Guid organizationId, string role)
    {
        var userRole = Enum.Parse<UserRole>(role);

        var users = await _context.Users
            .Include(u => u.Organization)
            .Where(u => u.OrganizationId == organizationId && u.Role == userRole && !u.IsDeleted)
            .ToListAsync();

        var result = new List<UserResponseDto>();
        foreach (var user in users)
        {
            result.Add(await MapToResponseAsync(user));
        }
        return result;
    }

    public async Task<UserResponseDto?> GetByEmailAsync(string email)
    {
        var user = await _context.Users
            .Include(u => u.Organization)
            .FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted);

        return user == null ? null : await MapToResponseAsync(user);
    }

    public async Task ChangeStatusAsync(Guid id, ChangeUserStatusDto dto)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            throw new Exception("Foydalanuvchi topilmadi");

        user.Status = Enum.Parse<UserStatus>(dto.Status);
        user.UpdatedAt = DateTime.UtcNow;

        // Agar bloklangan bo'lsa, refresh tokenni o'chirish
        if (dto.Status == "Blocked")
        {
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
        }

        await _context.SaveChangesAsync();
    }

    public async Task<int> GetCountByOrganizationAsync(Guid organizationId)
    {
        return await _context.Users
            .CountAsync(u => u.OrganizationId == organizationId && !u.IsDeleted);
    }

    private async Task<UserResponseDto> MapToResponseAsync(User user)
    {
        var orgName = user.Organization?.Name;
        if (string.IsNullOrEmpty(orgName) && user.OrganizationId != Guid.Empty)
        {
            var org = await _context.Organizations.FindAsync(user.OrganizationId);
            orgName = org?.Name;
        }

        return new UserResponseDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            AvatarUrl = user.AvatarUrl,
            Role = user.Role.ToString(),
            Status = user.Status.ToString(),
            OrganizationId = user.OrganizationId,
            OrganizationName = orgName ?? string.Empty,
            LastLoginAt = user.LastLoginAt,
            CreatedAt = user.CreatedAt
        };
    }
}