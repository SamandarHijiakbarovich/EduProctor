using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EduProctor.Services.Interfaces;

public interface IBruteForceProtectionService
{
    Task<bool> IsBlockedAsync(string key);
    Task RecordFailedAttemptAsync(string key);
    Task ResetAttemptsAsync(string key);
}