using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EduProctor.Core.Entities
{
    public class Group:BaseEntity
    {
        public Guid OrganizationId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Year { get; set; }

        // Navigation
        public virtual Organization Organization { get; set; } = null!;
        public virtual ICollection<GroupMember> Members { get; set; } = new List<GroupMember>();
        public virtual ICollection<Test> Tests { get; set; } = new List<Test>();
    }
}
