using EduProctor.Shared.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EduProctor.Services.Interfaces;

public interface ITestService
{
    Task<TestResponseDto> CreateTestAsync(Guid organizationId, CreateTestDto dto);
    Task<TestResponseDto> UpdateTestAsync(Guid testId, Guid organizationId, UpdateTestDto dto);
    Task DeleteTestAsync(Guid testId, Guid organizationId);
    Task<TestResponseDto?> GetTestByIdAsync(Guid testId, Guid organizationId);
    Task<List<TestResponseDto>> GetTestsAsync(Guid organizationId, string? status = null);
    Task<TestDetailResponseDto> GetTestWithQuestionsAsync(Guid testId, Guid organizationId);

    Task<QuestionDto> AddQuestionAsync(Guid testId, Guid organizationId, CreateQuestionDto dto);
    Task UpdateQuestionAsync(Guid questionId, Guid testId, Guid organizationId, CreateQuestionDto dto);
    Task DeleteQuestionAsync(Guid questionId, Guid testId, Guid organizationId);

    Task PublishTestAsync(Guid testId, Guid organizationId, PublishTestDto dto);
}
