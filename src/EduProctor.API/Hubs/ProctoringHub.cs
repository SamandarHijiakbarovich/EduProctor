using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using EduProctor.Services.Interfaces;
using EduProctor.Shared.DTOs;
using EduProctor.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace EduProctor.API.Hubs;

[Authorize]
public class ProctoringHub : Hub
{
    private readonly IProctoringService _proctoringService;
    private readonly ILogger<ProctoringHub> _logger;
    private static readonly Dictionary<string, string> _userConnections = new();

    public ProctoringHub(IProctoringService proctoringService, ILogger<ProctoringHub> logger)
    {
        _proctoringService = proctoringService;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value;
        var orgId = Context.User?.FindFirst("org_id")?.Value;

        if (userId != null)
        {
            _userConnections[userId] = Context.ConnectionId;

            // Admin va SuperAdmin o'z tashkilotining guruhiga qo'shiladi
            if (role == "Admin" || role == "SuperAdmin")
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"org_{orgId}");
                _logger.LogInformation("Admin {UserId} joined group org_{OrgId}", userId, orgId);
            }
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId != null)
        {
            _userConnections.Remove(userId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// O'quvchidan proctoring event qabul qilish
    /// </summary>
    public async Task SendProctoringEvent(ProctoringEventDto eventDto)
    {
        try
        {
            var userId = Guid.Parse(Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value;

            // Faqat Student yubora oladi
            if (role != "Student")
                throw new HubException("Faqat studentlar event yubora oladi");

            await _proctoringService.ProcessEventAsync(eventDto, userId);

            // Adminga xabar yuborish
            await Clients.Group($"org_{Context.User?.FindFirst("org_id")?.Value}")
                .SendAsync("ProctoringAlert", new
                {
                    eventDto.SessionId,
                    eventDto.Type,
                    eventDto.Level,
                    eventDto.Message,
                    eventDto.Timestamp
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SendProctoringEvent xatosi");
            throw new HubException(ex.Message);
        }
    }

    /// <summary>
    /// O'quvchini bloklash (Admin)
    /// </summary>
    public async Task BlockStudent(Guid sessionId, string reason)
    {
        var userId = Guid.Parse(Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
        var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value;
        var orgId = Guid.Parse(Context.User?.FindFirst("org_id")?.Value!);

        if (role != "Admin" && role != "SuperAdmin")
            throw new HubException("Ruxsat yo'q");

        await _proctoringService.BlockStudentAsync(sessionId, orgId, reason);

        // Bloklangan o'quvchiga xabar yuborish
        var session = await GetSessionByStudentAsync(sessionId);
        if (session != null && _userConnections.ContainsKey(session.UserId.ToString()))
        {
            await Clients.Client(_userConnections[session.UserId.ToString()])
                .SendAsync("SessionBlocked", new { reason });
        }
    }

    /// <summary>
    /// Faol studentlar ro'yxati (Admin)
    /// </summary>
    public async Task<List<ActiveStudentDto>> GetActiveStudents()
    {
        var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value;
        var orgId = Guid.Parse(Context.User?.FindFirst("org_id")?.Value!);

        if (role != "Admin" && role != "SuperAdmin")
            throw new HubException("Ruxsat yo'q");

        return await _proctoringService.GetActiveStudentsAsync(orgId);
    }

    private async Task<ExamSession?> GetSessionByStudentAsync(Guid sessionId)
    {
        using var scope = Context.GetHttpContext()?.RequestServices.CreateScope();
        var dbContext = scope?.ServiceProvider.GetRequiredService<Infrastructure.Data.AppDbContext>();

        return await dbContext!.ExamSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId);
    }
}