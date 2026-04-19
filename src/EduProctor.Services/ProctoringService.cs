using Microsoft.EntityFrameworkCore;
using EduProctor.Core.Entities;
using EduProctor.Infrastructure.Data;
using EduProctor.Services.Interfaces;
using EduProctor.Shared.DTOs;
using System.Text.Json;
using EduProctor.Core;
using Microsoft.Extensions.Logging;

namespace EduProctor.Services;

public class ProctoringService : IProctoringService
{
    private readonly AppDbContext _context;
    private readonly ILogger<ProctoringService> _logger;

    // Proctoring chegaralari
    private const int WARNING_THRESHOLD_MS = 3000;
    private const int DANGER_THRESHOLD_MS = 5000;
    private const int CRITICAL_THRESHOLD_MS = 7000;
    private const int DEBOUNCE_MS = 2000;

    public ProctoringService(AppDbContext context, ILogger<ProctoringService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task ProcessEventAsync(ProctoringEventDto eventDto, Guid userId)
    {
        try
        {
            // 1. Sessiyani tekshirish
            var session = await _context.ExamSessions
                .Include(s => s.Test)
                .ThenInclude(t => t.Organization)
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == eventDto.SessionId && s.UserId == userId);

            if (session == null)
                throw new Exception("Sessiya topilmadi");

            if (session.Status != ExamSessionStatus.Active)
                throw new Exception("Sessiya faol emas");

            // 2. Debounce tekshiruvi (bir xil eventni 2 soniyada qayta yubormaslik)
            var lastEvent = await _context.ProctoringEvents
                .Where(p => p.SessionId == eventDto.SessionId && p.Type.ToString() == eventDto.Type)
                .OrderByDescending(p => p.Timestamp)
                .FirstOrDefaultAsync();

            if (lastEvent != null && (DateTime.UtcNow - lastEvent.Timestamp).TotalMilliseconds < DEBOUNCE_MS)
            {
                return; // Debounce - xabar yubormaymiz
            }

            // 3. Level aniqlash (agar frontenddan kelmagan bo'lsa)
            var level = DetermineLevel(eventDto.Type, eventDto.Metadata);

            // 4. Eventni saqlash
            var proctoringEvent = new ProctoringEvent
            {
                SessionId = eventDto.SessionId,
                Type = Enum.Parse<ProctoringEventType>(eventDto.Type),
                Level = level,
                Message = eventDto.Message,
                Metadata = eventDto.Metadata,
                Timestamp = DateTime.UtcNow,
                IsNotified = false
            };

            _context.ProctoringEvents.Add(proctoringEvent);
            await _context.SaveChangesAsync();

            // 5. SignalR orqali adminga xabar yuborish (WebSocket)
            // Bu SignalR Hub orqali amalga oshiriladi

            _logger.LogWarning("Proctoring event: {Type}, Level: {Level}, Session: {SessionId}",
                eventDto.Type, level, eventDto.SessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ProcessEventAsync xatosi");
            throw;
        }
    }

    public async Task<List<ProctoringEventDto>> GetSessionEventsAsync(Guid sessionId, Guid userId, string? role)
    {
        // Admin yoki SuperAdmin faqat o'z sessionlarini ko'ra oladi
        if (role != "SuperAdmin")
        {
            var session = await _context.ExamSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId);

            if (session == null && role != "Admin")
                throw new Exception("Sessiyaga ruxsat yo'q");
        }

        var events = await _context.ProctoringEvents
            .Where(p => p.SessionId == sessionId)
            .OrderByDescending(p => p.Timestamp)
            .Take(500)
            .Select(p => new ProctoringEventDto
            {
                SessionId = p.SessionId,
                Type = p.Type.ToString(),
                Level = p.Level.ToString(),
                Message = p.Message,
                Metadata = p.Metadata,
                Timestamp = p.Timestamp
            })
            .ToListAsync();

        return events;
    }

    public async Task<ViolationSummaryDto> GetViolationSummaryAsync(Guid sessionId, Guid userId, string? role)
    {
        // Ruxsat tekshiruvi
        if (role != "SuperAdmin")
        {
            var session = await _context.ExamSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId);

            if (session == null && role != "Admin")
                throw new Exception("Ruxsat yo'q");
        }

        var events = await _context.ProctoringEvents
            .Where(p => p.SessionId == sessionId)
            .ToListAsync();

        var byType = events
            .GroupBy(p => p.Type.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        return new ViolationSummaryDto
        {
            SessionId = sessionId,
            TotalViolations = events.Count,
            Warnings = events.Count(p => p.Level == ProctoringLevel.Warning),
            Dangers = events.Count(p => p.Level == ProctoringLevel.Danger),
            Critical = events.Count(p => p.Level == ProctoringLevel.Critical),
            ByType = byType
        };
    }

    public async Task<List<ActiveStudentDto>> GetActiveStudentsAsync(Guid organizationId)
    {
        var activeSessions = await _context.ExamSessions
            .Include(s => s.User)
            .Include(s => s.Test)
            .Where(s => s.Status == ExamSessionStatus.Active && s.Test.OrganizationId == organizationId)
            .ToListAsync();

        var result = new List<ActiveStudentDto>();

        foreach (var session in activeSessions)
        {
            var elapsed = (DateTime.UtcNow - session.StartedAt).TotalMinutes;
            var remaining = Math.Max(0, session.Test.DurationMinutes - (int)elapsed);

            var violationCount = await _context.ProctoringEvents
                .CountAsync(p => p.SessionId == session.Id && p.Level > ProctoringLevel.Info);

            result.Add(new ActiveStudentDto
            {
                SessionId = session.Id,
                UserId = session.UserId,
                StudentName = $"{session.User.FirstName} {session.User.LastName}",
                TestTitle = session.Test.Title,
                StartedAt = session.StartedAt,
                RemainingMinutes = remaining,
                ViolationCount = violationCount,
                Status = session.Status.ToString()
            });
        }

        return result;
    }

    public async Task BlockStudentAsync(Guid sessionId, Guid organizationId, string reason)
    {
        var session = await _context.ExamSessions
            .Include(s => s.Test)
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.Test.OrganizationId == organizationId);

        if (session == null)
            throw new Exception("Sessiya topilmadi");

        session.Status = ExamSessionStatus.Terminated;
        session.EndedAt = DateTime.UtcNow;

        // Bloklanganligi haqida event qo'shish
        var blockEvent = new ProctoringEvent
        {
            SessionId = sessionId,
            Type = ProctoringEventType.TabSwitch, // Maxsus event
            Level = ProctoringLevel.Critical,
            Message = $"Student blocked: {reason}",
            Timestamp = DateTime.UtcNow,
            IsNotified = true
        };

        _context.ProctoringEvents.Add(blockEvent);
        await _context.SaveChangesAsync();

        _logger.LogWarning("Student blocked: Session {SessionId}, Reason: {Reason}", sessionId, reason);
    }

    public async Task SendWarningAsync(Guid sessionId, string message)
    {
        var warningEvent = new ProctoringEvent
        {
            SessionId = sessionId,
            Type = ProctoringEventType.HeadTurn,
            Level = ProctoringLevel.Warning,
            Message = message,
            Timestamp = DateTime.UtcNow,
            IsNotified = true
        };

        _context.ProctoringEvents.Add(warningEvent);
        await _context.SaveChangesAsync();
    }

    // ==================== PRIVATE METHODS ====================

    private ProctoringLevel DetermineLevel(string eventType, JsonDocument? metadata)
    {
        int duration = 0;
        if (metadata != null && metadata.RootElement.TryGetProperty("duration", out var durationElement))
        {
            duration = durationElement.GetInt32();
        }

        return eventType switch
        {
            "FaceLost" when duration >= CRITICAL_THRESHOLD_MS => ProctoringLevel.Critical,
            "FaceLost" when duration >= DANGER_THRESHOLD_MS => ProctoringLevel.Danger,
            "MicrophoneSpeech" => ProctoringLevel.Danger,
            "MicrophoneWhisper" => ProctoringLevel.Warning,
            "TabSwitch" or "WindowBlur" => ProctoringLevel.Danger,
            "CopyPaste" or "ScreenshotAttempt" => ProctoringLevel.Critical,
            _ when duration >= DANGER_THRESHOLD_MS => ProctoringLevel.Danger,
            _ when duration >= WARNING_THRESHOLD_MS => ProctoringLevel.Warning,
            _ => ProctoringLevel.Info
        };
    }
}