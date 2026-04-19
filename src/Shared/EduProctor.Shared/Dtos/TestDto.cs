using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace EduProctor.Shared.Dtos;

public class CreateTestDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = "MCQ"; // MCQ, Essay, FillIn, Mixed
    public int DurationMinutes { get; set; }
    public int PassingScore { get; set; }
    public bool ShuffleQuestions { get; set; } = false;
    public bool ShuffleOptions { get; set; } = false;
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public JsonDocument? Settings { get; set; }
}

public class UpdateTestDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DurationMinutes { get; set; }
    public int PassingScore { get; set; }
    public bool ShuffleQuestions { get; set; }
    public bool ShuffleOptions { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string Status { get; set; } = "Draft";
}

public class TestResponseDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public int TotalScore { get; set; }
    public int PassingScore { get; set; }
    public string Status { get; set; } = string.Empty;
    public int QuestionCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
}

public class TestDetailResponseDto : TestResponseDto
{
    public List<QuestionDto> Questions { get; set; } = new();
}

public class QuestionDto
{
    public Guid Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public List<string>? Options { get; set; }
    public int Score { get; set; }
    public int OrderIndex { get; set; }
    public int? MinWords { get; set; }
    public int? MaxWords { get; set; }
}

public class CreateQuestionDto
{
    public string Text { get; set; } = string.Empty;
    public string Type { get; set; } = "MCQ";
    public List<string>? Options { get; set; }
    public string? CorrectAnswer { get; set; }
    public int Score { get; set; }
    public int? MinWords { get; set; }
    public int? MaxWords { get; set; }
}

public class PublishTestDto
{
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public List<Guid>? GroupIds { get; set; }
}