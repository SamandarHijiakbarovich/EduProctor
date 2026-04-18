using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace EduProctor.Core.Entities
{
    public class ProctoringEvent:BaseEntity
    {
        public Guid SessionId { get; set; }
        public ProctoringEventType Type { get; set; }
        public ProctoringLevel Level { get; set; }
        public string Message { get; set; } = string.Empty;
        public JsonDocument? Metadata { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public bool IsNotified { get; set; }

        // Navigation
        public virtual ExamSession Session { get; set; } = null!;
    }
}
