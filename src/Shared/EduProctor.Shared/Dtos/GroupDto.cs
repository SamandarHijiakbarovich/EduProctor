namespace EduProctor.Shared.DTOs;

public class CreateGroupDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Year { get; set; }
}

public class UpdateGroupDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Year { get; set; }
}

public class GroupResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Year { get; set; }
    public int MemberCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AddMemberDto
{
    public Guid UserId { get; set; }
}

public class GroupMemberResponseDto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
}

public class GroupDetailResponseDto : GroupResponseDto
{
    public List<GroupMemberResponseDto> Members { get; set; } = new();
}