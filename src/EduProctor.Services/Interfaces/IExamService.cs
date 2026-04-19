using EduProctor.Shared.DTOs;

namespace EduProctor.Services.Interfaces;

public interface IExamService
{
    Task<ExamSessionDto> StartExamAsync(Guid userId, StartExamDto dto);
    Task SubmitAnswerAsync(Guid sessionId, Guid userId, SubmitAnswerDto dto);
    Task<ExamResultDto> SubmitExamAsync(Guid sessionId, Guid userId);
    Task<ExamResultDto> GetExamResultAsync(Guid sessionId, Guid userId);
    Task<ExamSessionDto> GetCurrentSessionAsync(Guid userId, Guid testId);
    Task<List<ExamQuestionDto>> GetExamQuestionsAsync(Guid sessionId, Guid userId);
    Task<bool> IsExamActiveAsync(Guid sessionId, Guid userId);
    Task AutoSubmitExpiredExamsAsync();
}