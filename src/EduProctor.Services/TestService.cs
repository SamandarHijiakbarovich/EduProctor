using Microsoft.EntityFrameworkCore;
using EduProctor.Core.Entities;
using EduProctor.Infrastructure.Data;
using EduProctor.Services.Interfaces;
using System.Text.Json;
using EduProctor.Core;
using EduProctor.Shared.Dtos;

namespace EduProctor.Services;

public class TestService : ITestService
{
    private readonly AppDbContext _context;

    public TestService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<TestResponseDto> CreateTestAsync(Guid organizationId, CreateTestDto dto)
    {
        var test = new Test
        {
            OrganizationId = organizationId,
            Title = dto.Title,
            Description = dto.Description,
            Type = Enum.Parse<TestType>(dto.Type),
            DurationMinutes = dto.DurationMinutes,
            PassingScore = dto.PassingScore,
            ShuffleQuestions = dto.ShuffleQuestions,
            ShuffleOptions = dto.ShuffleOptions,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            Settings = dto.Settings,
            Status = TestStatus.Draft
        };

        _context.Tests.Add(test);
        await _context.SaveChangesAsync();

        return MapToResponseDto(test);
    }

    public async Task<TestResponseDto> UpdateTestAsync(Guid testId, Guid organizationId, UpdateTestDto dto)
    {
        var test = await GetTestByIdAndOrgAsync(testId, organizationId);

        test.Title = dto.Title;
        test.Description = dto.Description;
        test.DurationMinutes = dto.DurationMinutes;
        test.PassingScore = dto.PassingScore;
        test.ShuffleQuestions = dto.ShuffleQuestions;
        test.ShuffleOptions = dto.ShuffleOptions;
        test.StartTime = dto.StartTime;
        test.EndTime = dto.EndTime;
        test.Status = Enum.Parse<TestStatus>(dto.Status);
        test.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return MapToResponseDto(test);
    }

    public async Task DeleteTestAsync(Guid testId, Guid organizationId)
    {
        var test = await GetTestByIdAndOrgAsync(testId, organizationId);
        test.IsDeleted = true;
        await _context.SaveChangesAsync();
    }

    public async Task<TestResponseDto?> GetTestByIdAsync(Guid testId, Guid organizationId)
    {
        var test = await _context.Tests
            .Where(t => t.Id == testId && t.OrganizationId == organizationId && !t.IsDeleted)
            .FirstOrDefaultAsync();

        return test == null ? null : MapToResponseDto(test);
    }

    public async Task<List<TestResponseDto>> GetTestsAsync(Guid organizationId, string? status = null)
    {
        var query = _context.Tests
            .Where(t => t.OrganizationId == organizationId && !t.IsDeleted);

        if (!string.IsNullOrEmpty(status))
        {
            var statusEnum = Enum.Parse<TestStatus>(status);
            query = query.Where(t => t.Status == statusEnum);
        }

        var tests = await query
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return tests.Select(MapToResponseDto).ToList();
    }

    public async Task<TestDetailResponseDto> GetTestWithQuestionsAsync(Guid testId, Guid organizationId)
    {
        var test = await _context.Tests
            .Include(t => t.Questions.Where(q => !q.IsDeleted))
            .Where(t => t.Id == testId && t.OrganizationId == organizationId && !t.IsDeleted)
            .FirstOrDefaultAsync();

        if (test == null)
            throw new Exception("Test topilmadi");

        var response = MapToDetailResponseDto(test);
        response.Questions = test.Questions.OrderBy(q => q.OrderIndex).Select(MapToQuestionDto).ToList();
        response.TotalScore = test.Questions.Sum(q => q.Score);

        return response;
    }

    public async Task<QuestionDto> AddQuestionAsync(Guid testId, Guid organizationId, CreateQuestionDto dto)
    {
        // Testni tekshirish
        var test = await GetTestByIdAndOrgAsync(testId, organizationId);

        if (test.Status != TestStatus.Draft)
            throw new Exception("Faqat draft holatidagi testga savol qo'shish mumkin");

        var maxOrder = await _context.Questions
            .Where(q => q.TestId == testId)
            .MaxAsync(q => (int?)q.OrderIndex) ?? 0;

        var question = new Question
        {
            TestId = testId,
            Text = dto.Text,
            Type = Enum.Parse<QuestionType>(dto.Type),
            Options = dto.Options != null ? JsonSerializer.SerializeToDocument(dto.Options) : null,
            CorrectAnswer = dto.CorrectAnswer,
            Score = dto.Score,
            OrderIndex = maxOrder + 1,
            MinWords = dto.MinWords,
            MaxWords = dto.MaxWords
        };

        _context.Questions.Add(question);
        await _context.SaveChangesAsync();

        return MapToQuestionDto(question);
    }

    public async Task UpdateQuestionAsync(Guid questionId, Guid testId, Guid organizationId, CreateQuestionDto dto)
    {
        var test = await GetTestByIdAndOrgAsync(testId, organizationId);

        if (test.Status != TestStatus.Draft)
            throw new Exception("Faqat draft holatidagi testga savol o'zgartirish mumkin");

        var question = await _context.Questions
            .FirstOrDefaultAsync(q => q.Id == questionId && q.TestId == testId);

        if (question == null)
            throw new Exception("Savol topilmadi");

        question.Text = dto.Text;
        question.Type = Enum.Parse<QuestionType>(dto.Type);
        question.Options = dto.Options != null ? JsonSerializer.SerializeToDocument(dto.Options) : null;
        question.CorrectAnswer = dto.CorrectAnswer;
        question.Score = dto.Score;
        question.MinWords = dto.MinWords;
        question.MaxWords = dto.MaxWords;
        question.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteQuestionAsync(Guid questionId, Guid testId, Guid organizationId)
    {
        var test = await GetTestByIdAndOrgAsync(testId, organizationId);

        if (test.Status != TestStatus.Draft)
            throw new Exception("Faqat draft holatidagi testdan savol o'chirish mumkin");

        var question = await _context.Questions
            .FirstOrDefaultAsync(q => q.Id == questionId && q.TestId == testId);

        if (question != null)
        {
            question.IsDeleted = true;
            await _context.SaveChangesAsync();
        }
    }

    public async Task PublishTestAsync(Guid testId, Guid organizationId, PublishTestDto dto)
    {
        var test = await GetTestByIdAndOrgAsync(testId, organizationId);

        if (!test.Questions.Any(q => !q.IsDeleted))
            throw new Exception("Testda kamida bitta savol bo'lishi kerak");

        test.Status = TestStatus.Published;
        test.PublishedAt = DateTime.UtcNow;
        test.StartTime = dto.StartTime;
        test.EndTime = dto.EndTime;
        test.UpdatedAt = DateTime.UtcNow;

        // Guruhlarga testni biriktirish
        if (dto.GroupIds != null && dto.GroupIds.Any())
        {
            var groups = await _context.Groups
                .Where(g => dto.GroupIds.Contains(g.Id) && g.OrganizationId == organizationId)
                .ToListAsync();

            test.Groups = groups;
        }

        await _context.SaveChangesAsync();
    }

    // Private helper methods
    private async Task<Test> GetTestByIdAndOrgAsync(Guid testId, Guid organizationId)
    {
        var test = await _context.Tests
            .Include(t => t.Questions)
            .FirstOrDefaultAsync(t => t.Id == testId && t.OrganizationId == organizationId && !t.IsDeleted);

        if (test == null)
            throw new Exception("Test topilmadi");

        return test;
    }

    private TestResponseDto MapToResponseDto(Test test)
    {
        return new TestResponseDto
        {
            Id = test.Id,
            Title = test.Title,
            Description = test.Description,
            Type = test.Type.ToString(),
            DurationMinutes = test.DurationMinutes,
            TotalScore = test.Questions?.Sum(q => q.Score) ?? 0,
            PassingScore = test.PassingScore,
            Status = test.Status.ToString(),
            QuestionCount = test.Questions?.Count(q => !q.IsDeleted) ?? 0,
            CreatedAt = test.CreatedAt,
            PublishedAt = test.PublishedAt
        };
    }

    private TestDetailResponseDto MapToDetailResponseDto(Test test)
    {
        return new TestDetailResponseDto
        {
            Id = test.Id,
            Title = test.Title,
            Description = test.Description,
            Type = test.Type.ToString(),
            DurationMinutes = test.DurationMinutes,
            TotalScore = test.Questions?.Sum(q => q.Score) ?? 0,
            PassingScore = test.PassingScore,
            Status = test.Status.ToString(),
            QuestionCount = test.Questions?.Count(q => !q.IsDeleted) ?? 0,
            CreatedAt = test.CreatedAt,
            PublishedAt = test.PublishedAt,
            Questions = new List<QuestionDto>()
        };
    }

    private QuestionDto MapToQuestionDto(Question question)
    {
        return new QuestionDto
        {
            Id = question.Id,
            Text = question.Text,
            Type = question.Type.ToString(),
            Options = question.Options != null ? JsonSerializer.Deserialize<List<string>>(question.Options.RootElement.GetRawText()) : null,
            Score = question.Score,
            OrderIndex = question.OrderIndex,
            MinWords = question.MinWords,
            MaxWords = question.MaxWords
        };
    }
}