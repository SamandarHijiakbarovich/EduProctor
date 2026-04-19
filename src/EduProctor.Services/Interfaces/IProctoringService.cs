using EduProctor.Shared.DTOs;

namespace EduProctor.Services.Interfaces;

public interface IProctoringService
{
    Task ProcessEventAsync(ProctoringEventDto eventDto, Guid userId);
    Task<List<ProctoringEventDto>> GetSessionEventsAsync(Guid sessionId, Guid userId, string? role);
    Task<ViolationSummaryDto> GetViolationSummaryAsync(Guid sessionId, Guid userId, string? role);
    Task<List<ActiveStudentDto>> GetActiveStudentsAsync(Guid organizationId);
    Task BlockStudentAsync(Guid sessionId, Guid organizationId, string reason);
    Task SendWarningAsync(Guid sessionId, string message);
}