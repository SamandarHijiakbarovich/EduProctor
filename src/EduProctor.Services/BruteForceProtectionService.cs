using EduProctor.Services.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace EduProctor.Services;

public class BruteForceProtectionService : IBruteForceProtectionService
{
    private readonly IDistributedCache _cache;
    private readonly int _maxAttempts = 5;      // Maksimal urinish
    private readonly int _blockMinutes = 15;    // Bloklash vaqti (daqiqa)

    public BruteForceProtectionService(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<bool> IsBlockedAsync(string key)
    {
        var blockKey = $"block:{key}";
        var blockExpiry = await _cache.GetStringAsync(blockKey);

        if (!string.IsNullOrEmpty(blockExpiry))
        {
            var expiry = JsonSerializer.Deserialize<DateTime>(blockExpiry);
            if (expiry > DateTime.UtcNow)
                return true;
        }

        return false;
    }

    public async Task RecordFailedAttemptAsync(string key)
    {
        var attemptKey = $"attempt:{key}";
        var attempts = await _cache.GetStringAsync(attemptKey);

        int attemptCount = 1;
        if (!string.IsNullOrEmpty(attempts))
        {
            attemptCount = JsonSerializer.Deserialize<int>(attempts) + 1;
        }

        await _cache.SetStringAsync(attemptKey, JsonSerializer.Serialize(attemptCount), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_blockMinutes)
        });

        if (attemptCount >= _maxAttempts)
        {
            await _cache.SetStringAsync($"block:{key}", JsonSerializer.Serialize(DateTime.UtcNow.AddMinutes(_blockMinutes)), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_blockMinutes)
            });
        }
    }

    public async Task ResetAttemptsAsync(string key)
    {
        var attemptKey = $"attempt:{key}";
        await _cache.RemoveAsync(attemptKey);
        await _cache.RemoveAsync($"block:{key}");
    }
}
