using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace EduProctor.Core.Entities
{
    public class Test:BaseEntity
    {
        public Guid OrganizationId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public TestType Type { get; set; }
        public int DurationMinutes { get; set; }
        public int TotalScore { get; set; }
        public int PassingScore { get; set; }
        public bool ShuffleQuestions { get; set; }
        public bool ShuffleOptions { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TestStatus Status { get; set; } = TestStatus.Draft;
        public JsonDocument? Settings { get; set; }
        public DateTime? PublishedAt { get; set; }

        // Navigation
        public virtual Organization Organization { get; set; } = null!;
        public virtual ICollection<Question> Questions { get; set; } = new List<Question>();
        public virtual ICollection<ExamSession> ExamSessions { get; set; } = new List<ExamSession>();
    }
}
