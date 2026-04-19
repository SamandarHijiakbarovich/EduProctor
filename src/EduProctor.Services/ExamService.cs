using Microsoft.EntityFrameworkCore;
using EduProctor.Core.Entities;
using EduProctor.Infrastructure.Data;
using EduProctor.Services.Interfaces;
using EduProctor.Shared.DTOs;
using System.Text.Json;
using EduProctor.Core;
using Microsoft.Extensions.Logging;

namespace EduProctor.Services;

public class ExamService : IExamService
{
    private readonly AppDbContext _context;
    private readonly ILogger<ExamService> _logger;

    public ExamService(AppDbContext context, ILogger<ExamService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ExamSessionDto> StartExamAsync(Guid userId, StartExamDto dto)
    {
        // 1. Testni tekshirish
        var test = await _context.Tests
            .Include(t => t.Questions.Where(q => !q.IsDeleted))
            .FirstOrDefaultAsync(t => t.Id == dto.TestId && t.Status == TestStatus.Published);

        if (test == null)
            throw new Exception("Test topilmadi yoki nashr qilinmagan");

        // 2. Vaqtni tekshirish
        var now = DateTime.UtcNow;
        if (test.StartTime.HasValue && now < test.StartTime.Value)
            throw new Exception($"Test {test.StartTime:yyyy-MM-dd HH:mm} da boshlanadi");

        if (test.EndTime.HasValue && now > test.EndTime.Value)
            throw new Exception("Test vaqti tugagan");

        // 3. Faol sessiya borligini tekshirish
        var activeSession = await _context.ExamSessions
            .FirstOrDefaultAsync(s => s.UserId == userId && s.TestId == dto.TestId && s.Status == ExamSessionStatus.Active);

        if (activeSession != null)
            return MapToSessionDto(activeSession, test);

        // 4. Yangi sessiya yaratish
        var session = new ExamSession
        {
            TestId = dto.TestId,
            UserId = userId,
            StartedAt = now,
            Status = ExamSessionStatus.Active,
            ClientIp = "0.0.0.0" // IP ni keyin olish mumkin
        };

        _context.ExamSessions.Add(session);
        await _context.SaveChangesAsync();

        return MapToSessionDto(session, test);
    }

    public async Task SubmitAnswerAsync(Guid sessionId, Guid userId, SubmitAnswerDto dto)
    {
        // 1. Sessiyani tekshirish
        var session = await GetActiveSessionAsync(sessionId, userId);

        // 2. Savolni tekshirish
        var question = await _context.Questions
            .FirstOrDefaultAsync(q => q.Id == dto.QuestionId && q.TestId == session.TestId);

        if (question == null)
            throw new Exception("Savol topilmadi");

        // 3. Avvalgi javobni topish yoki yangi yaratish
        var answer = await _context.Answers
            .FirstOrDefaultAsync(a => a.SessionId == sessionId && a.QuestionId == dto.QuestionId);

        if (answer == null)
        {
            answer = new Answer
            {
                SessionId = sessionId,
                QuestionId = dto.QuestionId
            };
            _context.Answers.Add(answer);
        }

        // 4. Javobni saqlash
        answer.AnswerText = dto.AnswerText;
        answer.AnsweredAt = DateTime.UtcNow;

        // 5. MCQ savolni avtomatik tekshirish
        if (question.Type == QuestionType.MCQ && !string.IsNullOrEmpty(question.CorrectAnswer))
        {
            answer.IsCorrect = dto.AnswerText?.Trim().ToUpper() == question.CorrectAnswer.Trim().ToUpper();
            answer.ObtainedScore = answer.IsCorrect ? question.Score : 0;
        }

        await _context.SaveChangesAsync();
    }

    public async Task<ExamResultDto> SubmitExamAsync(Guid sessionId, Guid userId)
    {
        var session = await GetActiveSessionAsync(sessionId, userId);
        var test = await _context.Tests
            .Include(t => t.Questions.Where(q => !q.IsDeleted))
            .FirstAsync(t => t.Id == session.TestId);

        // 1. Barcha javoblarni yakuniy baholash
        var answers = await _context.Answers
            .Where(a => a.SessionId == sessionId)
            .ToListAsync();

        // Essay va FillIn savollarni qayta baholash (agar kerak bo'lsa)
        foreach (var answer in answers)
        {
            var question = test.Questions.First(q => q.Id == answer.QuestionId);

            if (question.Type == QuestionType.Essay && answer.ObtainedScore == null)
            {
                // O'qituvchi baholashi kerak
                answer.ObtainedScore = 0;
                answer.IsCorrect = false;
            }
            else if (question.Type == QuestionType.MCQ && answer.ObtainedScore == null)
            {
                answer.IsCorrect = answer.AnswerText?.Trim().ToUpper() == question.CorrectAnswer?.Trim().ToUpper();
                answer.ObtainedScore = answer.IsCorrect == true ? question.Score : 0;
            }
        }

        // 2. Jami ballni hisoblash
        var totalScore = answers.Sum(a => a.ObtainedScore ?? 0);
        var maxScore = test.Questions.Sum(q => q.Score);

        // 3. Qoidabuzarliklarni hisoblash
        var violationCount = await _context.ProctoringEvents
            .CountAsync(p => p.SessionId == sessionId && p.Level > ProctoringLevel.Info);

        // 4. Sessiyani yakunlash
        session.EndedAt = DateTime.UtcNow;
        session.Score = totalScore;
        session.IsSubmitted = true;
        session.Status = ExamSessionStatus.Completed;

        await _context.SaveChangesAsync();

        // 5. Natijalarni qaytarish
        return await BuildExamResultAsync(sessionId, session, test, answers, violationCount);
    }

    public async Task<ExamResultDto> GetExamResultAsync(Guid sessionId, Guid userId)
    {
        var session = await _context.ExamSessions
            .Include(s => s.Test)
            .ThenInclude(t => t.Questions.Where(q => !q.IsDeleted))
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId);

        if (session == null)
            throw new Exception("Sessiya topilmadi");

        var answers = await _context.Answers
            .Where(a => a.SessionId == sessionId)
            .ToListAsync();

        var violationCount = await _context.ProctoringEvents
            .CountAsync(p => p.SessionId == sessionId && p.Level > ProctoringLevel.Info);

        return await BuildExamResultAsync(sessionId, session, session.Test, answers, violationCount);
    }

    public async Task<ExamSessionDto> GetCurrentSessionAsync(Guid userId, Guid testId)
    {
        var session = await _context.ExamSessions
            .FirstOrDefaultAsync(s => s.UserId == userId && s.TestId == testId && s.Status == ExamSessionStatus.Active);

        if (session == null)
            throw new Exception("Faol sessiya topilmadi");

        var test = await _context.Tests.FindAsync(testId);

        return MapToSessionDto(session, test!);
    }

    public async Task<List<ExamQuestionDto>> GetExamQuestionsAsync(Guid sessionId, Guid userId)
    {
        var session = await GetActiveSessionAsync(sessionId, userId);

        var questions = await _context.Questions
            .Where(q => q.TestId == session.TestId && !q.IsDeleted)
            .OrderBy(q => q.OrderIndex)
            .ToListAsync();

        var result = questions.Select(q => new ExamQuestionDto
        {
            Id = q.Id,
            Text = q.Text,
            Type = q.Type.ToString(),
            Options = DeserializeOptions(q.Options),
            Score = q.Score,
            OrderIndex = q.OrderIndex,
            MinWords = q.MinWords,
            MaxWords = q.MaxWords
        }).ToList();

        return result;
    }

    private List<string>? DeserializeOptions(JsonDocument? options)
    {
        if (options == null) return null;

        try
        {
            return JsonSerializer.Deserialize<List<string>>(options.RootElement.GetRawText());
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> IsExamActiveAsync(Guid sessionId, Guid userId)
    {
        var session = await _context.ExamSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId);

        if (session == null)
            return false;

        // Vaqt tugaganligini tekshirish
        var test = await _context.Tests.FindAsync(session.TestId);
        if (test != null)
        {
            var elapsed = (DateTime.UtcNow - session.StartedAt).TotalMinutes;
            if (elapsed >= test.DurationMinutes)
            {
                session.Status = ExamSessionStatus.Completed;
                await _context.SaveChangesAsync();
                return false;
            }
        }

        return session.Status == ExamSessionStatus.Active;
    }

    public async Task AutoSubmitExpiredExamsAsync()
    {
        var expiredSessions = await _context.ExamSessions
            .Include(s => s.Test)
            .Where(s => s.Status == ExamSessionStatus.Active)
            .ToListAsync();

        foreach (var session in expiredSessions)
        {
            var elapsed = (DateTime.UtcNow - session.StartedAt).TotalMinutes;
            if (elapsed >= session.Test.DurationMinutes)
            {
                try
                {
                    await SubmitExamAsync(session.Id, session.UserId);
                    _logger.LogInformation("Auto-submitted expired exam session {SessionId}", session.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Auto-submit failed for session {SessionId}", session.Id);
                }
            }
        }
    }

    // ==================== PRIVATE METHODS ====================

    private async Task<ExamSession> GetActiveSessionAsync(Guid sessionId, Guid userId)
    {
        var session = await _context.ExamSessions
            .Include(s => s.Test)
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId);

        if (session == null)
            throw new Exception("Sessiya topilmadi");

        if (session.Status != ExamSessionStatus.Active)
            throw new Exception("Sessiya faol emas");

        // Vaqt tugaganligini tekshirish
        var elapsed = (DateTime.UtcNow - session.StartedAt).TotalMinutes;
        if (elapsed >= session.Test.DurationMinutes)
        {
            session.Status = ExamSessionStatus.Completed;
            await _context.SaveChangesAsync();
            throw new Exception("Test vaqti tugagan");
        }

        return session;
    }

    private ExamSessionDto MapToSessionDto(ExamSession session, Test test)
    {
        var elapsed = (DateTime.UtcNow - session.StartedAt).TotalMinutes;
        var remaining = Math.Max(0, test.DurationMinutes - (int)elapsed);

        var answeredCount = _context.Answers.Count(a => a.SessionId == session.Id);
        var totalQuestions = _context.Questions.Count(q => q.TestId == session.TestId && !q.IsDeleted);

        return new ExamSessionDto
        {
            Id = session.Id,
            TestId = session.TestId,
            TestTitle = test.Title,
            StartedAt = session.StartedAt,
            EndedAt = session.EndedAt,
            Score = session.Score,
            IsSubmitted = session.IsSubmitted,
            Status = session.Status.ToString(),
            RemainingMinutes = remaining,
            TotalQuestions = totalQuestions,
            AnsweredQuestions = answeredCount
        };
    }

    private async Task<ExamResultDto> BuildExamResultAsync(Guid sessionId, ExamSession session, Test test, List<Answer> answers, int violationCount)
    {
        var totalScore = answers.Sum(a => a.ObtainedScore ?? 0);
        var maxScore = test.Questions.Sum(q => q.Score);
        var percentage = maxScore > 0 ? (double)totalScore / maxScore * 100 : 0;

        var answerResults = new List<AnswerResultDto>();

        foreach (var question in test.Questions)
        {
            var answer = answers.FirstOrDefault(a => a.QuestionId == question.Id);

            answerResults.Add(new AnswerResultDto
            {
                QuestionId = question.Id,
                QuestionText = question.Text,
                YourAnswer = answer?.AnswerText,
                CorrectAnswer = question.Type == QuestionType.MCQ ? question.CorrectAnswer : null,
                IsCorrect = answer?.IsCorrect ?? false,
                ObtainedScore = answer?.ObtainedScore ?? 0,
                MaxScore = question.Score
            });
        }

        return new ExamResultDto
        {
            SessionId = sessionId,
            Score = totalScore,
            TotalScore = maxScore,
            Percentage = percentage,
            IsPassed = totalScore >= test.PassingScore,
            CorrectAnswers = answers.Count(a => a.IsCorrect == true),
            WrongAnswers = answers.Count(a => a.IsCorrect == false && a.ObtainedScore == 0),
            Unanswered = test.Questions.Count - answers.Count,
            ViolationCount = violationCount,
            StartedAt = session.StartedAt,
            EndedAt = session.EndedAt,
            Answers = answerResults
        };
    }
}