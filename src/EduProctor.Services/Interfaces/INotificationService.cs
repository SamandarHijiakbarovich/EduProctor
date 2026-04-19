using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EduProctor.Services.Interfaces;

public interface INotificationService
{
    Task SendEmailAsync(string to, string subject, string body);
    Task SendEmailTemplateAsync(string to, string templateName, Dictionary<string, string> placeholders);
    Task SendSmsAsync(string phoneNumber, string message);
    Task SendProctorAlertAsync(Guid sessionId, string studentName, string alertType, string message);
    Task SendExamResultAsync(string email, string studentName, string testTitle, int score, int totalScore);
}
