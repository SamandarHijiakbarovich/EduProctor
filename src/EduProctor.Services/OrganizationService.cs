using Microsoft.EntityFrameworkCore;
using EduProctor.Core.Entities;
using EduProctor.Infrastructure.Data;
using EduProctor.Services.Interfaces;
using EduProctor.Shared.DTOs;
using EduProctor.Core;

namespace EduProctor.Services;

public class OrganizationService : IOrganizationService
{
    private readonly AppDbContext _context;

    public OrganizationService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<OrganizationResponseDto> CreateAsync(CreateOrganizationDto dto)
    {
        var slug = dto.Slug ?? dto.Name.ToLower().Replace(" ", "-");

        var organization = new Organization
        {
            Name = dto.Name,
            Slug = slug,
            Email = dto.Email,
            Phone = dto.Phone,
            Address = dto.Address,
            Status = OrganizationStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        _context.Organizations.Add(organization);
        await _context.SaveChangesAsync();

        return MapToResponse(organization);
    }

    public async Task<OrganizationResponseDto> UpdateAsync(Guid id, UpdateOrganizationDto dto)
    {
        var organization = await _context.Organizations.FindAsync(id);
        if (organization == null)
            throw new Exception("Organization topilmadi");

        organization.Name = dto.Name;
        organization.Email = dto.Email;
        organization.Phone = dto.Phone;
        organization.Address = dto.Address;
        organization.Status = Enum.Parse<OrganizationStatus>(dto.Status);
        organization.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return MapToResponse(organization);
    }

    public async Task DeleteAsync(Guid id)
    {
        var organization = await _context.Organizations.FindAsync(id);
        if (organization != null)
        {
            organization.IsDeleted = true;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<OrganizationResponseDto?> GetByIdAsync(Guid id)
    {
        var organization = await _context.Organizations
            .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);

        return organization == null ? null : MapToResponse(organization);
    }

    public async Task<List<OrganizationResponseDto>> GetAllAsync()
    {
        var organizations = await _context.Organizations
            .Where(o => !o.IsDeleted)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return organizations.Select(MapToResponse).ToList();
    }

    public async Task<OrganizationResponseDto?> GetBySlugAsync(string slug)
    {
        var organization = await _context.Organizations
            .FirstOrDefaultAsync(o => o.Slug == slug && !o.IsDeleted);

        return organization == null ? null : MapToResponse(organization);
    }

    private OrganizationResponseDto MapToResponse(Organization org)
    {
        return new OrganizationResponseDto
        {
            Id = org.Id,
            Name = org.Name,
            Slug = org.Slug,
            Email = org.Email,
            Phone = org.Phone,
            Address = org.Address,
            AdminCount = org.AdminCount,
            StudentCount = org.StudentCount,
            TestCount = org.TestCount,
            Status = org.Status.ToString(),
            CreatedAt = org.CreatedAt
        };
    }
}