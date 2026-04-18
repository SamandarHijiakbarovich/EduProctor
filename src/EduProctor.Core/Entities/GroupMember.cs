using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EduProctor.Core.Entities
{
    public class GroupMember:BaseEntity
    {
        public Guid GroupId { get; set; }
        public Guid UserId { get; set; }
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual Group Group { get; set; } = null!;
        public virtual User User { get; set; } = null!;
    }
}
