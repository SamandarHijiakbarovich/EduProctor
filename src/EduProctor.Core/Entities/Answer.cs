using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EduProctor.Core.Entities
{
    public class Answer:BaseEntity
    {
        public Guid SessionId { get; set; }
        public Guid QuestionId { get; set; }
        public string? AnswerText { get; set; }
        public bool IsCorrect { get; set; }
        public int? ObtainedScore { get; set; }
        public DateTime AnsweredAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual ExamSession Session { get; set; } = null!;
        public virtual Question Question { get; set; } = null!;
    }
}
