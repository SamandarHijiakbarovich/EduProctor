using EduProctor.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EduProctor.Services.Interfaces;

public interface IUserService
{
    Task<UserResponseDto> CreateAsync(CreateUserDto dto);
    Task<UserResponseDto> UpdateAsync(Guid id, UpdateUserDto dto);
    Task DeleteAsync(Guid id);
    Task<UserResponseDto?> GetByIdAsync(Guid id);
    Task<List<UserResponseDto>> GetAllAsync(Guid organizationId);
    Task<List<UserResponseDto>> GetByRoleAsync(Guid organizationId, string role);
    Task<UserResponseDto?> GetByEmailAsync(string email);
    Task ChangeStatusAsync(Guid id, ChangeUserStatusDto dto);
    Task<int> GetCountByOrganizationAsync(Guid organizationId);
}
