namespace Emotions.Domain.Entities;

public class RoomMember
{
    public Guid Id { get; set; } // PK
    public Guid RoomId { get; set; }
    public Room Room { get; set; } = null!;
    public Guid UserId { get; set; }

    public string Name { get; set; } = null!; // required, 128
    public string Role { get; set; } = "member"; // required, 32 (owner|moderator|member)

    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}