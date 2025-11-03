using System.ComponentModel.DataAnnotations;

namespace Emotions.Domain.Entities;

public class MoodEntry
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public double Value { get; set; } // 0..1
}