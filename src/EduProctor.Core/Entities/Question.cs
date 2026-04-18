using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace EduProctor.Core.Entities
{
    public class Question:BaseEntity
    {
        public Guid TestId { get; set; }
        public string Text { get; set; } = string.Empty;
        public QuestionType Type { get; set; }
        public JsonDocument? Options { get; set; }
        public string? CorrectAnswer { get; set; }
        public int Score { get; set; }
        public int OrderIndex { get; set; }
        public int? MinWords { get; set; }
        public int? MaxWords { get; set; }

        // Navigation
        public virtual Test Test { get; set; } = null!;
        public virtual ICollection<Answer> Answers { get; set; } = new List<Answer>();
    }
}
