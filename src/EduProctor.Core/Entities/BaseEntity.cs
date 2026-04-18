namespace EduProctor.Core;

public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();  // <-- Id property BO'LISHI KERAK
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;
}