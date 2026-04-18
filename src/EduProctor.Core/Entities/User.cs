using System.Text.Json;
using EduProctor.Core;  // <-- BaseEntity uchun

namespace EduProctor.Core.Entities;

public class User : BaseEntity   // <-- BaseEntity dan meros
{
    public Guid OrganizationId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public UserRole Role { get; set; }
    public UserStatus Status { get; set; } = UserStatus.Active;
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }
    public DateTime? LastLoginAt { get; set; }

    // Navigation
    public virtual Organization Organization { get; set; } = null!;
    public virtual ICollection<ExamSession> ExamSessions { get; set; } = new List<ExamSession>();
    public virtual ICollection<GroupMember> GroupMembers { get; set; } = new List<GroupMember>();
}