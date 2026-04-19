using System.Collections.Concurrent;

namespace EduProctor.API.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private static readonly ConcurrentDictionary<string, ClientRequestInfo> _clients = new();
    private readonly int _maxRequests = 100;      // So'rovlar soni
    private readonly int _timeWindowSeconds = 60; // Vaqt oralig'i (1 daqiqa)

    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientIp = GetClientIp(context);
        var key = $"{clientIp}:{DateTime.UtcNow:yyyy-MM-dd-HH-mm}";

        if (_clients.TryGetValue(key, out var clientInfo))
        {
            if (clientInfo.RequestCount >= _maxRequests)
            {
                _logger.LogWarning("Rate limit exceeded for IP {ClientIp}", clientIp);
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.Response.WriteAsJsonAsync(new
                {
                    success = false,
                    message = "Ko'p so'rov yuborildi. Iltimos, birozdan keyin urinib ko'ring."
                });
                return;
            }
            clientInfo.RequestCount++;
        }
        else
        {
            _clients[key] = new ClientRequestInfo { RequestCount = 1, FirstRequestTime = DateTime.UtcNow };
        }

        await _next(context);
    }

    private string GetClientIp(HttpContext context)
    {
        var ip = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (string.IsNullOrEmpty(ip))
        {
            ip = context.Connection.RemoteIpAddress?.ToString();
        }
        return ip ?? "unknown";
    }
}

public class ClientRequestInfo
{
    public int RequestCount { get; set; }
    public DateTime FirstRequestTime { get; set; }
}