namespace Emotions.Domain.Entities;

public class RoomConnection
{
    // DB primary key (by convention) — no [Key] needed
    public Guid Id { get; set; }

    // Room FK + back-nav
    public Guid RoomId { get; set; }
    public Room Room { get; set; } = null!;

    // User identity (adjust type if yours isn’t Guid)
    public Guid UserId { get; set; }
    public string Username { get; set; } = null!;

    // Correlate with SignalR (useful for debugging and targeted disconnects)
    public string HubConnectionId { get; set; } = null!; // e.g., Context.ConnectionId

    public DateTimeOffset ConnectedAt { get; set; }
    public DateTimeOffset? DisconnectedAt { get; set; }

    public bool IsMuted { get; set; }
    public bool IsVideoOn { get; set; }
}