using Microsoft.EntityFrameworkCore;
using EduProctor.Core.Entities;
using EduProctor.Infrastructure.Data;
using EduProctor.Services.Interfaces;
using EduProctor.Shared.DTOs;
using EduProctor.Core;

namespace EduProctor.Services;

public class GroupService : IGroupService
{
    private readonly AppDbContext _context;

    public GroupService(AppDbContext context)
    {
        _context = context;
    }

    // ==================== GROUP CRUD ====================

    public async Task<GroupResponseDto> CreateAsync(Guid organizationId, CreateGroupDto dto)
    {
        var group = new Group
        {
            OrganizationId = organizationId,
            Name = dto.Name,
            Description = dto.Description,
            Year = dto.Year,
            CreatedAt = DateTime.UtcNow
        };

        _context.Groups.Add(group);
        await _context.SaveChangesAsync();

        return MapToResponse(group);
    }

    public async Task<GroupResponseDto> UpdateAsync(Guid groupId, Guid organizationId, UpdateGroupDto dto)
    {
        var group = await GetGroupByIdAndOrgAsync(groupId, organizationId);

        group.Name = dto.Name;
        group.Description = dto.Description;
        group.Year = dto.Year;
        group.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return MapToResponse(group);
    }

    public async Task DeleteAsync(Guid groupId, Guid organizationId)
    {
        var group = await GetGroupByIdAndOrgAsync(groupId, organizationId);
        group.IsDeleted = true;
        await _context.SaveChangesAsync();
    }

    public async Task<GroupResponseDto?> GetByIdAsync(Guid groupId, Guid organizationId)
    {
        var group = await _context.Groups
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == groupId && g.OrganizationId == organizationId && !g.IsDeleted);

        return group == null ? null : MapToResponse(group);
    }

    public async Task<List<GroupResponseDto>> GetAllAsync(Guid organizationId)
    {
        var groups = await _context.Groups
            .Include(g => g.Members)
            .Where(g => g.OrganizationId == organizationId && !g.IsDeleted)
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync();

        return groups.Select(MapToResponse).ToList();
    }

    public async Task<GroupDetailResponseDto> GetGroupWithMembersAsync(Guid groupId, Guid organizationId)
    {
        var group = await _context.Groups
            .Include(g => g.Members)
                .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(g => g.Id == groupId && g.OrganizationId == organizationId && !g.IsDeleted);

        if (group == null)
            throw new Exception("Guruh topilmadi");

        var response = new GroupDetailResponseDto
        {
            Id = group.Id,
            Name = group.Name,
            Description = group.Description,
            Year = group.Year,
            MemberCount = group.Members.Count,
            CreatedAt = group.CreatedAt,
            Members = group.Members.Select(m => new GroupMemberResponseDto
            {
                UserId = m.UserId,
                UserName = $"{m.User.FirstName} {m.User.LastName}",
                Email = m.User.Email,
                JoinedAt = m.JoinedAt
            }).ToList()
        };

        return response;
    }

    // ==================== MEMBER MANAGEMENT ====================

    public async Task AddMemberAsync(Guid groupId, Guid organizationId, AddMemberDto dto)
    {
        // Guruhni tekshirish
        var group = await GetGroupByIdAndOrgAsync(groupId, organizationId);

        // Foydalanuvchini tekshirish (o'sha tashkilotga tegishlimi)
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == dto.UserId && u.OrganizationId == organizationId && u.Role == UserRole.Student);

        if (user == null)
            throw new Exception("Student topilmadi");

        // A'zo allaqachon mavjudmi?
        var exists = await _context.GroupMembers
            .AnyAsync(m => m.GroupId == groupId && m.UserId == dto.UserId);

        if (exists)
            throw new Exception("Bu foydalanuvchi allaqachon guruhga qo'shilgan");

        var member = new GroupMember
        {
            GroupId = groupId,
            UserId = dto.UserId,
            JoinedAt = DateTime.UtcNow
        };

        _context.GroupMembers.Add(member);
        await _context.SaveChangesAsync();
    }

    public async Task RemoveMemberAsync(Guid groupId, Guid organizationId, Guid userId)
    {
        // Guruhni tekshirish
        await GetGroupByIdAndOrgAsync(groupId, organizationId);

        var member = await _context.GroupMembers
            .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == userId);

        if (member == null)
            throw new Exception("Foydalanuvchi guruhda topilmadi");

        _context.GroupMembers.Remove(member);
        await _context.SaveChangesAsync();
    }

    public async Task<List<GroupMemberResponseDto>> GetMembersAsync(Guid groupId, Guid organizationId)
    {
        await GetGroupByIdAndOrgAsync(groupId, organizationId);

        var members = await _context.GroupMembers
            .Include(m => m.User)
            .Where(m => m.GroupId == groupId)
            .Select(m => new GroupMemberResponseDto
            {
                UserId = m.UserId,
                UserName = $"{m.User.FirstName} {m.User.LastName}",
                Email = m.User.Email,
                JoinedAt = m.JoinedAt
            })
            .ToListAsync();

        return members;
    }

    public async Task<bool> IsMemberAsync(Guid groupId, Guid userId)
    {
        return await _context.GroupMembers
            .AnyAsync(m => m.GroupId == groupId && m.UserId == userId);
    }

    // ==================== PRIVATE METHODS ====================

    private async Task<Group> GetGroupByIdAndOrgAsync(Guid groupId, Guid organizationId)
    {
        var group = await _context.Groups
            .FirstOrDefaultAsync(g => g.Id == groupId && g.OrganizationId == organizationId && !g.IsDeleted);

        if (group == null)
            throw new Exception("Guruh topilmadi");

        return group;
    }

    private GroupResponseDto MapToResponse(Group group)
    {
        return new GroupResponseDto
        {
            Id = group.Id,
            Name = group.Name,
            Description = group.Description,
            Year = group.Year,
            MemberCount = group.Members?.Count ?? 0,
            CreatedAt = group.CreatedAt
        };
    }
}