using System.ComponentModel.DataAnnotations;

namespace Emotions.Domain.Entities
{
    public enum RoomStatus
    {
        Draft = 0,
        Live = 1,
        Archived = 2
    }

    public enum RoomSpeakPolicy
    {
        ModeratorOnly = 0,
        RaiseHand = 1,
        OpenButRateLimited = 2
    }

    public enum RoomTheme
    {
        General = 0,
        Parenthood = 1,
        Loneliness = 2,
        Anxiety = 3,
        Depression = 4
    }


    public class Room
    {
        [Key] public Guid Id { get; set; } = Guid.NewGuid();
        [Required, MaxLength(120)] public string Title { get; set; } = null!; // canonical display name
        [MaxLength(240)] public string? Prompt { get; set; }
        [MaxLength(120)] public string? Topic { get; set; }
        [MaxLength(2048)] public string? Description { get; set; }
        [MaxLength(2083)] public string? ThumbnailUrl { get; set; }
        public RoomTheme Theme { get; set; } = RoomTheme.General;
        public string Language { get; set; } = "en"; // ISO code
        public int? MaxParticipants { get; set; } = 6;
        public RoomSpeakPolicy SpeakPolicy { get; set; } = RoomSpeakPolicy.RaiseHand;
        public RoomStatus Status { get; set; } = RoomStatus.Live;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }
        public DateTimeOffset? LastActiveAt { get; set; }
        public bool IsDeleted { get; set; } = false;
        [Timestamp] public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public ICollection<RoomMember> Members { get; set; } = new List<RoomMember>();
        public ICollection<RoomConnection> Connections { get; set; } = new List<RoomConnection>();
    }
}