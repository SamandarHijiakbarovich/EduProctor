using System.Text.Json;

namespace EduProctor.Shared.DTOs;

public class ProctoringEventDto
{
    public Guid SessionId { get; set; }
    public string Type { get; set; } = string.Empty;  // HeadTurn, GazeAway, TabSwitch, etc.
    public string Level { get; set; } = string.Empty; // Info, Warning, Danger, Critical
    public string Message { get; set; } = string.Empty;
    public JsonDocument? Metadata { get; set; }  // duration, angle, etc.
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class ProctoringAlertDto
{
    public Guid SessionId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentEmail { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int ViolationCount { get; set; }
    public DateTime Timestamp { get; set; }
}

public class ActiveStudentDto
{
    public Guid SessionId { get; set; }
    public Guid UserId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string TestTitle { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public int RemainingMinutes { get; set; }
    public int ViolationCount { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class ViolationSummaryDto
{
    public Guid SessionId { get; set; }
    public int TotalViolations { get; set; }
    public int Warnings { get; set; }
    public int Dangers { get; set; }
    public int Critical { get; set; }
    public Dictionary<string, int> ByType { get; set; } = new();
}