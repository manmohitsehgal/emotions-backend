using Emotions.Domain.Enums;

namespace Emotions.Domain.Entities;

public class SupportSession
{
    public Guid Id { get; set; }
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public SessionType Type { get; set; }
    public SessionStatus Status { get; set; }
    public SpeakPolicy SpeakPolicy { get; set; }


    public DateTimeOffset StartAt { get; set; }
    public DateTimeOffset EndAt { get; set; }
    public int Capacity { get; set; } = 15; // seated talkers
    public bool AllowListeners { get; set; } = true; // listen‑only overflow


    public Guid? TemplateId { get; set; }
    public SessionTemplate? Template { get; set; }


// Host link (AI or human)
    public Guid HostId { get; set; } // references SessionHost.Id
    public SessionHost Host { get; set; } = default!;


// VoiceRoom mapping (reusing hub infra)
    public Guid? VoiceRoomId { get; set; }
    public VoiceRoom? VoiceRoom { get; set; }


// Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PublishedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}