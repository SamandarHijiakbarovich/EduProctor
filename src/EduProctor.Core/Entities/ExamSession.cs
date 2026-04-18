using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EduProctor.Core.Entities;

public class ExamSession:BaseEntity
{
    public Guid TestId { get; set; }
    public Guid UserId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public int? Score { get; set; }
    public bool IsSubmitted { get; set; }
    public ExamSessionStatus Status { get; set; } = ExamSessionStatus.Active;
    public string? ClientIp { get; set; }
    public string? UserAgent { get; set; }

    // Navigation
    public virtual Test Test { get; set; } = null!;
    public virtual User User { get; set; } = null!;
    public virtual ICollection<Answer> Answers { get; set; } = new List<Answer>();
    public virtual ICollection<ProctoringEvent> ProctoringEvents { get; set; } = new List<ProctoringEvent>();
}
