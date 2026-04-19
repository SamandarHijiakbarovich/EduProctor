using EduProctor.Shared.DTOs;

namespace EduProctor.Services.Interfaces;

public interface IOrganizationService
{
    Task<OrganizationResponseDto> CreateAsync(CreateOrganizationDto dto);
    Task<OrganizationResponseDto> UpdateAsync(Guid id, UpdateOrganizationDto dto);
    Task DeleteAsync(Guid id);
    Task<OrganizationResponseDto?> GetByIdAsync(Guid id);
    Task<List<OrganizationResponseDto>> GetAllAsync();
    Task<OrganizationResponseDto?> GetBySlugAsync(string slug);
}