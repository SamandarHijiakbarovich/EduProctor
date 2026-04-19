namespace EduProctor.Shared.DTOs;

public class StartExamDto
{
    public Guid TestId { get; set; }
}

public class SubmitAnswerDto
{
    public Guid QuestionId { get; set; }
    public string? AnswerText { get; set; }
}

public class ExamSessionDto
{
    public Guid Id { get; set; }
    public Guid TestId { get; set; }
    public string TestTitle { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public int? Score { get; set; }
    public bool IsSubmitted { get; set; }
    public string Status { get; set; } = string.Empty;
    public int RemainingMinutes { get; set; }
    public int TotalQuestions { get; set; }
    public int AnsweredQuestions { get; set; }
}

public class ExamResultDto
{
    public Guid SessionId { get; set; }
    public int Score { get; set; }
    public int TotalScore { get; set; }
    public double Percentage { get; set; }
    public bool IsPassed { get; set; }
    public int CorrectAnswers { get; set; }
    public int WrongAnswers { get; set; }
    public int Unanswered { get; set; }
    public int ViolationCount { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public List<AnswerResultDto> Answers { get; set; } = new();
}

public class AnswerResultDto
{
    public Guid QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string? YourAnswer { get; set; }
    public string? CorrectAnswer { get; set; }
    public bool IsCorrect { get; set; }
    public int ObtainedScore { get; set; }
    public int MaxScore { get; set; }
}

public class ExamQuestionDto
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