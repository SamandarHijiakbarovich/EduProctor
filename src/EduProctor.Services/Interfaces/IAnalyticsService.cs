using EduProctor.Shared.DTOs;

namespace EduProctor.Services.Interfaces;

public interface IAnalyticsService
{
    // SuperAdmin dashboard
    Task<DashboardStatsDto> GetDashboardStatsAsync();

    // Organization stats
    Task<List<OrganizationStatsDto>> GetAllOrganizationsStatsAsync();
    Task<OrganizationStatsDto> GetOrganizationStatsAsync(Guid organizationId);

    // Test stats
    Task<List<TestStatsDto>> GetTestsStatsAsync(Guid organizationId);
    Task<TestStatsDto> GetTestStatsAsync(Guid testId, Guid organizationId);

    // Student performance
    Task<List<StudentPerformanceDto>> GetStudentsPerformanceAsync(Guid organizationId);
    Task<StudentPerformanceDto> GetStudentPerformanceAsync(Guid studentId, Guid organizationId);

    // Trends
    Task<List<DailyStatsDto>> GetDailyStatsAsync(Guid organizationId, int days);
    Task<List<ViolationTrendDto>> GetViolationTrendsAsync(Guid organizationId, int days);

    // Export
    Task<byte[]> ExportReportAsync(ExportReportDto dto, Guid requesterId, string requesterRole);
}