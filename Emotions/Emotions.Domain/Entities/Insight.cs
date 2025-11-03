using System.ComponentModel.DataAnnotations;

namespace Emotions.Domain.Entities;

public class Insight
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    [MaxLength(1000)] public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}