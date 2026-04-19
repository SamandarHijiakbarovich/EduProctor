namespace EduProctor.Shared.DTOs;

public class DashboardStatsDto
{
    public int TotalOrganizations { get; set; }
    public int TotalUsers { get; set; }
    public int TotalStudents { get; set; }
    public int TotalAdmins { get; set; }
    public int TotalTests { get; set; }
    public int ActiveTests { get; set; }
    public int TotalExamSessions { get; set; }
    public int ActiveSessions { get; set; }
    public int TotalViolations { get; set; }
    public double AverageScore { get; set; }
    public double PassRate { get; set; }
}

public class OrganizationStatsDto
{
    public Guid OrganizationId { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public int StudentCount { get; set; }
    public int AdminCount { get; set; }
    public int TestCount { get; set; }
    public int ExamCount { get; set; }
    public int ViolationCount { get; set; }
    public double AverageScore { get; set; }
    public double PassRate { get; set; }
    public double ViolationRate { get; set; }
}

public class TestStatsDto
{
    public Guid TestId { get; set; }
    public string TestTitle { get; set; } = string.Empty;
    public string TestType { get; set; } = string.Empty;
    public int TotalAttempts { get; set; }
    public int CompletedAttempts { get; set; }
    public double AverageScore { get; set; }
    public double HighestScore { get; set; }
    public double LowestScore { get; set; }
    public double PassRate { get; set; }
    public int AverageViolationsPerSession { get; set; }
}

public class StudentPerformanceDto
{
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int TotalExams { get; set; }
    public int CompletedExams { get; set; }
    public double AverageScore { get; set; }
    public double AveragePercentage { get; set; }
    public int TotalViolations { get; set; }
    public int WarningCount { get; set; }
    public int DangerCount { get; set; }
    public int CriticalCount { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class DailyStatsDto
{
    public DateTime Date { get; set; }
    public int ExamCount { get; set; }
    public int ViolationCount { get; set; }
    public double AverageScore { get; set; }
}

public class ViolationTrendDto
{
    public string ViolationType { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
}

public class ExportReportDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public Guid? OrganizationId { get; set; }
    public string Format { get; set; } = "PDF"; // PDF, Excel
}