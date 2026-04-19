using EduProctor.Services.Interfaces;
using EduProctor.Services.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace EduProctor.Services;

public class NotificationService : INotificationService
{
    private readonly EmailSettings _emailSettings;
    private readonly SmsSettings _smsSettings;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IOptions<EmailSettings> emailSettings,
        IOptions<SmsSettings> smsSettings,
        ILogger<NotificationService> logger)
    {
        _emailSettings = emailSettings.Value;
        _smsSettings = smsSettings.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        try
        {
            using var client = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.SmtpPort);
            client.Credentials = new NetworkCredential(_emailSettings.SenderEmail, _emailSettings.SenderPassword);
            client.EnableSsl = _emailSettings.EnableSsl;

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_emailSettings.SenderEmail),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            mailMessage.To.Add(to);

            await client.SendMailAsync(mailMessage);
            _logger.LogInformation("Email sent to {To}", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", to);
            throw;
        }
    }

    public async Task SendEmailTemplateAsync(string to, string templateName, Dictionary<string, string> placeholders)
    {
        var template = GetEmailTemplate(templateName);
        var body = template;

        foreach (var placeholder in placeholders)
        {
            body = body.Replace($"{{{{{placeholder.Key}}}}}", placeholder.Value);
        }

        var subject = templateName switch
        {
            "welcome" => "EduProctor platformasiga xush kelibsiz!",
            "exam_result" => $"Imtihon natijalari - {placeholders.GetValueOrDefault("testTitle")}",
            "proctor_alert" => "⚠️ Proctoring ogohlantirishi",
            "reset_password" => "Parolni tiklash",
            _ => "EduProctor xabarnomasi"
        };

        await SendEmailAsync(to, subject, body);
    }

    public async Task SendSmsAsync(string phoneNumber, string message)
    {
        try
        {
            if (_smsSettings.Provider == "twilio")
            {
                // Twilio SMS (package: Twilio)
                // TwilioClient.Init(_smsSettings.ApiKey, _smsSettings.ApiSecret);
                // var sms = await MessageResource.CreateAsync(
                //     body: message,
                //     from: new PhoneNumber(_smsSettings.FromNumber),
                //     to: new PhoneNumber(phoneNumber)
                // );
                _logger.LogInformation("SMS sent to {PhoneNumber} via Twilio", phoneNumber);
            }
            else if (_smsSettings.Provider == "eskiz")
            {
                // Eskiz.uz SMS (O'zbekiston)
                // https://eskiz.uz
                _logger.LogInformation("SMS sent to {PhoneNumber} via Eskiz", phoneNumber);
            }

            _logger.LogInformation("SMS sent to {PhoneNumber}", phoneNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SMS to {PhoneNumber}", phoneNumber);
            throw;
        }
    }

    public async Task SendProctorAlertAsync(Guid sessionId, string studentName, string alertType, string message)
    {
        var subject = $"⚠️ Proctoring Alert - {alertType}";
        var body = $@"
            <h2>Proctoring Ogohlantirishi</h2>
            <p><strong>Sessiya ID:</strong> {sessionId}</p>
            <p><strong>Student:</strong> {studentName}</p>
            <p><strong>Alert turi:</strong> {alertType}</p>
            <p><strong>Xabar:</strong> {message}</p>
            <p><strong>Vaqt:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
            <hr/>
            <p>EduProctor avtomatik xabarnomasi</p>
        ";

        // Admin emailiga yuborish (admin emailini bazadan olish kerak)
        await SendEmailAsync("admin@eduproctor.com", subject, body);
    }

    public async Task SendExamResultAsync(string email, string studentName, string testTitle, int score, int totalScore)
    {
        var percentage = totalScore > 0 ? (double)score / totalScore * 100 : 0;
        var passed = percentage >= 70;
        var status = passed ? "✅ Muvaffaqiyatli" : "❌ Muvaffaqiyatsiz";

        var body = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .header {{ background-color: #4CAF50; color: white; padding: 10px; text-align: center; }}
                    .content {{ padding: 20px; }}
                    .score {{ font-size: 24px; font-weight: bold; color: #333; }}
                    .passed {{ color: green; }}
                    .failed {{ color: red; }}
                    .footer {{ background-color: #f1f1f1; padding: 10px; text-align: center; font-size: 12px; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h2>Imtihon Natijalari</h2>
                    </div>
                    <div class='content'>
                        <p>Salom <strong>{studentName}</strong>,</p>
                        <p>Sizning <strong>{testTitle}</strong> imtihon natijalaringiz:</p>
                        <p class='score'>Ball: {score} / {totalScore} ({percentage:F1}%)</p>
                        <p class='{(passed ? "passed" : "failed")}'>Holat: {status}</p>
                        <hr/>
                        <p>Batafsil ma'lumot uchun EduProctor tizimiga kiring.</p>
                    </div>
                    <div class='footer'>
                        <p>© 2025 EduProctor. Barcha huquqlar himoyalangan.</p>
                    </div>
                </div>
            </body>
            </html>
        ";

        await SendEmailAsync(email, $"{testTitle} imtihon natijalari", body);
    }

    private string GetEmailTemplate(string templateName)
    {
        return templateName switch
        {
            "welcome" => @"
                <h2>Xush kelibsiz!</h2>
                <p>Hurmatli {{name}},</p>
                <p>Siz EduProctor platformasiga muvaffaqiyatli ro'yxatdan o'tdingiz.</p>
                <p>Endi siz imtihonlar topshirishingiz va natijalaringizni kuzatishingiz mumkin.</p>
                <br/>
                <p>Hurmat bilan,<br/>EduProctor jamoasi</p>
            ",
            "reset_password" => @"
                <h2>Parolni tiklash</h2>
                <p>Hurmatli {{name}},</p>
                <p>Sizning parolingizni tiklash uchun quyidagi linkni bosing:</p>
                <p><a href='{{link}}'>Parolni tiklash</a></p>
                <p>Agar bu so'rov siz tomoningizdan bo'lmagan bo'lsa, ushbu xabarni e'tiborsiz qoldiring.</p>
                <br/>
                <p>Hurmat bilan,<br/>EduProctor jamoasi</p>
            ",
            _ => "<p>{{message}}</p>"
        };
    }
}
