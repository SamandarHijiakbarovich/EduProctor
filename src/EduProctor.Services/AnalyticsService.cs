using Microsoft.EntityFrameworkCore;
using EduProctor.Core.Entities;
using EduProctor.Infrastructure.Data;
using EduProctor.Services.Interfaces;
using EduProctor.Shared.DTOs;
using EduProctor.Core;
using Microsoft.Extensions.Logging;

namespace EduProctor.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly AppDbContext _context;
    private readonly ILogger<AnalyticsService> _logger;

    public AnalyticsService(AppDbContext context, ILogger<AnalyticsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<DashboardStatsDto> GetDashboardStatsAsync()
    {
        var totalOrganizations = await _context.Organizations.CountAsync();
        var totalUsers = await _context.Users.CountAsync();
        var totalStudents = await _context.Users.CountAsync(u => u.Role == UserRole.Student);
        var totalAdmins = await _context.Users.CountAsync(u => u.Role == UserRole.Admin);
        var totalTests = await _context.Tests.CountAsync(t => !t.IsDeleted);
        var activeTests = await _context.Tests.CountAsync(t => t.Status == TestStatus.Published && !t.IsDeleted);

        var totalExamSessions = await _context.ExamSessions.CountAsync();
        var activeSessions = await _context.ExamSessions.CountAsync(s => s.Status == ExamSessionStatus.Active);

        var totalViolations = await _context.ProctoringEvents
            .CountAsync(p => p.Level > ProctoringLevel.Info);

        var completedSessions = await _context.ExamSessions
            .Where(s => s.IsSubmitted && s.Score.HasValue)
            .ToListAsync();

        var averageScore = completedSessions.Any()
            ? completedSessions.Average(s => s.Score ?? 0)
            : 0;

        var passCount = completedSessions.Count(s => s.Score >= (s.Test?.PassingScore ?? 0));
        var passRate = completedSessions.Any()
            ? (double)passCount / completedSessions.Count * 100
            : 0;

        return new DashboardStatsDto
        {
            TotalOrganizations = totalOrganizations,
            TotalUsers = totalUsers,
            TotalStudents = totalStudents,
            TotalAdmins = totalAdmins,
            TotalTests = totalTests,
            ActiveTests = activeTests,
            TotalExamSessions = totalExamSessions,
            ActiveSessions = activeSessions,
            TotalViolations = totalViolations,
            AverageScore = Math.Round(averageScore, 2),
            PassRate = Math.Round(passRate, 2)
        };
    }

    public async Task<List<OrganizationStatsDto>> GetAllOrganizationsStatsAsync()
    {
        var organizations = await _context.Organizations
            .Include(o => o.Users)
            .Include(o => o.Tests)
            .ThenInclude(t => t.ExamSessions)
            .ToListAsync();

        var result = new List<OrganizationStatsDto>();

        foreach (var org in organizations)
        {
            var examSessions = org.Tests.SelectMany(t => t.ExamSessions).ToList();
            var completedSessions = examSessions.Where(s => s.IsSubmitted && s.Score.HasValue).ToList();

            var violations = await _context.ProctoringEvents
                .Where(p => examSessions.Select(s => s.Id).Contains(p.SessionId) && p.Level > ProctoringLevel.Info)
                .CountAsync();

            var averageScore = completedSessions.Any()
                ? completedSessions.Average(s => s.Score ?? 0)
                : 0;

            var passCount = completedSessions.Count(s => s.Score >= (s.Test?.PassingScore ?? 0));
            var passRate = completedSessions.Any()
                ? (double)passCount / completedSessions.Count * 100
                : 0;

            result.Add(new OrganizationStatsDto
            {
                OrganizationId = org.Id,
                OrganizationName = org.Name,
                StudentCount = org.Users.Count(u => u.Role == UserRole.Student),
                AdminCount = org.Users.Count(u => u.Role == UserRole.Admin),
                TestCount = org.Tests.Count,
                ExamCount = examSessions.Count,
                ViolationCount = violations,
                AverageScore = Math.Round(averageScore, 2),
                PassRate = Math.Round(passRate, 2),
                ViolationRate = examSessions.Any() ? Math.Round((double)violations / examSessions.Count, 2) : 0
            });
        }

        return result;
    }

    public async Task<OrganizationStatsDto> GetOrganizationStatsAsync(Guid organizationId)
    {
        var org = await _context.Organizations
            .Include(o => o.Users)
            .Include(o => o.Tests)
            .ThenInclude(t => t.ExamSessions)
            .FirstOrDefaultAsync(o => o.Id == organizationId);

        if (org == null)
            throw new Exception("Organization topilmadi");

        var examSessions = org.Tests.SelectMany(t => t.ExamSessions).ToList();
        var completedSessions = examSessions.Where(s => s.IsSubmitted && s.Score.HasValue).ToList();

        var violations = await _context.ProctoringEvents
            .Where(p => examSessions.Select(s => s.Id).Contains(p.SessionId) && p.Level > ProctoringLevel.Info)
            .CountAsync();

        var averageScore = completedSessions.Any()
            ? completedSessions.Average(s => s.Score ?? 0)
            : 0;

        var passCount = completedSessions.Count(s => s.Score >= (s.Test?.PassingScore ?? 0));
        var passRate = completedSessions.Any()
            ? (double)passCount / completedSessions.Count * 100
            : 0;

        return new OrganizationStatsDto
        {
            OrganizationId = org.Id,
            OrganizationName = org.Name,
            StudentCount = org.Users.Count(u => u.Role == UserRole.Student),
            AdminCount = org.Users.Count(u => u.Role == UserRole.Admin),
            TestCount = org.Tests.Count,
            ExamCount = examSessions.Count,
            ViolationCount = violations,
            AverageScore = Math.Round(averageScore, 2),
            PassRate = Math.Round(passRate, 2),
            ViolationRate = examSessions.Any() ? Math.Round((double)violations / examSessions.Count, 2) : 0
        };
    }

    public async Task<List<TestStatsDto>> GetTestsStatsAsync(Guid organizationId)
    {
        var tests = await _context.Tests
            .Include(t => t.ExamSessions)
            .Where(t => t.OrganizationId == organizationId && !t.IsDeleted)
            .ToListAsync();

        var result = new List<TestStatsDto>();

        foreach (var test in tests)
        {
            var completedSessions = test.ExamSessions
                .Where(s => s.IsSubmitted && s.Score.HasValue)
                .ToList();

            var violations = await _context.ProctoringEvents
                .Where(p => test.ExamSessions.Select(s => s.Id).Contains(p.SessionId) && p.Level > ProctoringLevel.Info)
                .CountAsync();

            result.Add(new TestStatsDto
            {
                TestId = test.Id,
                TestTitle = test.Title,
                TestType = test.Type.ToString(),
                TotalAttempts = test.ExamSessions.Count,
                CompletedAttempts = completedSessions.Count,
                AverageScore = completedSessions.Any() ? Math.Round(completedSessions.Average(s => s.Score ?? 0), 2) : 0,
                HighestScore = completedSessions.Any() ? completedSessions.Max(s => s.Score ?? 0) : 0,
                LowestScore = completedSessions.Any() ? completedSessions.Min(s => s.Score ?? 0) : 0,
                PassRate = completedSessions.Any()
                    ? Math.Round((double)completedSessions.Count(s => s.Score >= test.PassingScore) / completedSessions.Count * 100, 2)
                    : 0,
                AverageViolationsPerSession = test.ExamSessions.Any()
                    ? violations / test.ExamSessions.Count
                    : 0
            });
        }

        return result;
    }

    public async Task<TestStatsDto> GetTestStatsAsync(Guid testId, Guid organizationId)
    {
        var test = await _context.Tests
            .Include(t => t.ExamSessions)
            .FirstOrDefaultAsync(t => t.Id == testId && t.OrganizationId == organizationId && !t.IsDeleted);

        if (test == null)
            throw new Exception("Test topilmadi");

        var completedSessions = test.ExamSessions
            .Where(s => s.IsSubmitted && s.Score.HasValue)
            .ToList();

        var violations = await _context.ProctoringEvents
            .Where(p => test.ExamSessions.Select(s => s.Id).Contains(p.SessionId) && p.Level > ProctoringLevel.Info)
            .CountAsync();

        return new TestStatsDto
        {
            TestId = test.Id,
            TestTitle = test.Title,
            TestType = test.Type.ToString(),
            TotalAttempts = test.ExamSessions.Count,
            CompletedAttempts = completedSessions.Count,
            AverageScore = completedSessions.Any() ? Math.Round(completedSessions.Average(s => s.Score ?? 0), 2) : 0,
            HighestScore = completedSessions.Any() ? completedSessions.Max(s => s.Score ?? 0) : 0,
            LowestScore = completedSessions.Any() ? completedSessions.Min(s => s.Score ?? 0) : 0,
            PassRate = completedSessions.Any()
                ? Math.Round((double)completedSessions.Count(s => s.Score >= test.PassingScore) / completedSessions.Count * 100, 2)
                : 0,
            AverageViolationsPerSession = test.ExamSessions.Any() ? violations / test.ExamSessions.Count : 0
        };
    }

    public async Task<List<StudentPerformanceDto>> GetStudentsPerformanceAsync(Guid organizationId)
    {
        var students = await _context.Users
            .Include(u => u.ExamSessions)
            .ThenInclude(s => s.Test)
            .Where(u => u.OrganizationId == organizationId && u.Role == UserRole.Student)
            .ToListAsync();

        var result = new List<StudentPerformanceDto>();

        foreach (var student in students)
        {
            var completedSessions = student.ExamSessions
                .Where(s => s.IsSubmitted && s.Score.HasValue)
                .ToList();

            var violations = await _context.ProctoringEvents
                .Where(p => student.ExamSessions.Select(s => s.Id).Contains(p.SessionId))
                .ToListAsync();

            result.Add(new StudentPerformanceDto
            {
                StudentId = student.Id,
                StudentName = $"{student.FirstName} {student.LastName}",
                Email = student.Email,
                TotalExams = student.ExamSessions.Count,
                CompletedExams = completedSessions.Count,
                AverageScore = completedSessions.Any() ? Math.Round(completedSessions.Average(s => s.Score ?? 0), 2) : 0,
                AveragePercentage = completedSessions.Any()
                    ? Math.Round(completedSessions.Average(s => (s.Score ?? 0) / (double)(s.Test?.TotalScore ?? 1) * 100), 2)
                    : 0,
                TotalViolations = violations.Count,
                WarningCount = violations.Count(v => v.Level == ProctoringLevel.Warning),
                DangerCount = violations.Count(v => v.Level == ProctoringLevel.Danger),
                CriticalCount = violations.Count(v => v.Level == ProctoringLevel.Critical),
                Status = student.Status.ToString()
            });
        }

        return result;
    }

    public async Task<StudentPerformanceDto> GetStudentPerformanceAsync(Guid studentId, Guid organizationId)
    {
        var student = await _context.Users
            .Include(u => u.ExamSessions)
            .ThenInclude(s => s.Test)
            .FirstOrDefaultAsync(u => u.Id == studentId && u.OrganizationId == organizationId && u.Role == UserRole.Student);

        if (student == null)
            throw new Exception("Student topilmadi");

        var completedSessions = student.ExamSessions
            .Where(s => s.IsSubmitted && s.Score.HasValue)
            .ToList();

        var violations = await _context.ProctoringEvents
            .Where(p => student.ExamSessions.Select(s => s.Id).Contains(p.SessionId))
            .ToListAsync();

        return new StudentPerformanceDto
        {
            StudentId = student.Id,
            StudentName = $"{student.FirstName} {student.LastName}",
            Email = student.Email,
            TotalExams = student.ExamSessions.Count,
            CompletedExams = completedSessions.Count,
            AverageScore = completedSessions.Any() ? Math.Round(completedSessions.Average(s => s.Score ?? 0), 2) : 0,
            AveragePercentage = completedSessions.Any()
                ? Math.Round(completedSessions.Average(s => (s.Score ?? 0) / (double)(s.Test?.TotalScore ?? 1) * 100), 2)
                : 0,
            TotalViolations = violations.Count,
            WarningCount = violations.Count(v => v.Level == ProctoringLevel.Warning),
            DangerCount = violations.Count(v => v.Level == ProctoringLevel.Danger),
            CriticalCount = violations.Count(v => v.Level == ProctoringLevel.Critical),
            Status = student.Status.ToString()
        };
    }

    public async Task<List<DailyStatsDto>> GetDailyStatsAsync(Guid organizationId, int days)
    {
        var startDate = DateTime.UtcNow.AddDays(-days);

        var sessions = await _context.ExamSessions
            .Include(s => s.Test)
            .Where(s => s.Test.OrganizationId == organizationId && s.StartedAt >= startDate)
            .ToListAsync();

        var violations = await _context.ProctoringEvents
            .Include(p => p.Session)
            .ThenInclude(s => s!.Test)
            .Where(p => p.Session!.Test.OrganizationId == organizationId && p.Timestamp >= startDate)
            .ToListAsync();

        var result = new List<DailyStatsDto>();

        for (var date = startDate.Date; date <= DateTime.UtcNow.Date; date = date.AddDays(1))
        {
            var daySessions = sessions.Where(s => s.StartedAt.Date == date).ToList();
            var dayViolations = violations.Where(v => v.Timestamp.Date == date).Count();

            var avgScore = daySessions.Where(s => s.IsSubmitted && s.Score.HasValue)
                .Select(s => s.Score ?? 0)
                .DefaultIfEmpty(0)
                .Average();

            result.Add(new DailyStatsDto
            {
                Date = date,
                ExamCount = daySessions.Count,
                ViolationCount = dayViolations,
                AverageScore = Math.Round(avgScore, 2)
            });
        }

        return result;
    }

    public async Task<List<ViolationTrendDto>> GetViolationTrendsAsync(Guid organizationId, int days)
    {
        var startDate = DateTime.UtcNow.AddDays(-days);

        var violations = await _context.ProctoringEvents
            .Include(p => p.Session)
            .ThenInclude(s => s!.Test)
            .Where(p => p.Session!.Test.OrganizationId == organizationId && p.Timestamp >= startDate)
            .ToListAsync();

        var total = violations.Count;

        var trends = violations
            .GroupBy(p => p.Type.ToString())
            .Select(g => new ViolationTrendDto
            {
                ViolationType = g.Key,
                Count = g.Count(),
                Percentage = total > 0 ? Math.Round((double)g.Count() / total * 100, 2) : 0
            })
            .OrderByDescending(t => t.Count)
            .ToList();

        return trends;
    }

    public async Task<byte[]> ExportReportAsync(ExportReportDto dto, Guid requesterId, string requesterRole)
    {
        // PDF yoki Excel export (keyingi bosqichda to'liq implementatsiya)
        _logger.LogInformation("Export report requested by {RequesterId}, role: {Role}", requesterId, requesterRole);

        // Hozircha oddiy JSON qaytaramiz
        var stats = new
        {
            dto.StartDate,
            dto.EndDate,
            OrganizationId = dto.OrganizationId,
            GeneratedAt = DateTime.UtcNow,
            RequestedBy = requesterId
        };

        var json = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(stats);
        return json;
    }
}