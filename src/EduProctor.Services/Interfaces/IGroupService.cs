using EduProctor.Shared.DTOs;

namespace EduProctor.Services.Interfaces;

public interface IGroupService
{
    // Group CRUD
    Task<GroupResponseDto> CreateAsync(Guid organizationId, CreateGroupDto dto);
    Task<GroupResponseDto> UpdateAsync(Guid groupId, Guid organizationId, UpdateGroupDto dto);
    Task DeleteAsync(Guid groupId, Guid organizationId);
    Task<GroupResponseDto?> GetByIdAsync(Guid groupId, Guid organizationId);
    Task<List<GroupResponseDto>> GetAllAsync(Guid organizationId);
    Task<GroupDetailResponseDto> GetGroupWithMembersAsync(Guid groupId, Guid organizationId);

    // Member management
    Task AddMemberAsync(Guid groupId, Guid organizationId, AddMemberDto dto);
    Task RemoveMemberAsync(Guid groupId, Guid organizationId, Guid userId);
    Task<List<GroupMemberResponseDto>> GetMembersAsync(Guid groupId, Guid organizationId);
    Task<bool> IsMemberAsync(Guid groupId, Guid userId);
}