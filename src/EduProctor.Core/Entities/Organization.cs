using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace EduProctor.Core.Entities;

public class Organization:BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public int AdminCount { get; set; } = 0;
    public int StudentCount { get; set; } = 0;
    public OrganizationStatus Status { get; set; } = OrganizationStatus.Pending;
    public JsonDocument? Settings { get; set; }

    // Navigation
    public virtual ICollection<User> Users { get; set; } = new List<User>();
    public virtual ICollection<Test> Tests { get; set; } = new List<Test>();
}
