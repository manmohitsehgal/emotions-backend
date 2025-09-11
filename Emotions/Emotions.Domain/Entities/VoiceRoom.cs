using System.ComponentModel.DataAnnotations;

namespace Emotions.Domain.Entities
{
    public enum VoiceRoomStatus
    {
        Draft = 0,
        Live = 1,
        Archived = 2
    }

    public enum VoiceRoomSpeakPolicy
    {
        ModeratorOnly = 0,
        RaiseHand = 1,
        OpenButRateLimited = 2
    }

    public enum VoiceRoomTheme
    {
        General = 0,
        Parenthood = 1,
        Loneliness = 2,
        Anxiety = 3,
        Depression = 4
    }


    public class VoiceRoom
    {
        [Key] public Guid Id { get; set; } = Guid.NewGuid();
        [Required, MaxLength(120)] public string Title { get; set; } = null!; // canonical display name
        [MaxLength(240)] public string? Prompt { get; set; }
        [MaxLength(120)] public string? Topic { get; set; }
        [MaxLength(2048)] public string? Description { get; set; }
        [MaxLength(2083)] public string? ThumbnailUrl { get; set; }
        public VoiceRoomTheme Theme { get; set; } = VoiceRoomTheme.General;
        public string Language { get; set; } = "en"; // ISO code
        public int? MaxParticipants { get; set; } = 6;
        public VoiceRoomSpeakPolicy SpeakPolicy { get; set; } = VoiceRoomSpeakPolicy.RaiseHand;
        public VoiceRoomStatus Status { get; set; } = VoiceRoomStatus.Live;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }
        public DateTimeOffset? LastActiveAt { get; set; }
        public bool IsDeleted { get; set; } = false;
        [Timestamp] public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public ICollection<VoiceRoomMember> Members { get; set; } = new List<VoiceRoomMember>();
        public ICollection<VoiceRoomConnection> Connections { get; set; } = new List<VoiceRoomConnection>();
    }
}