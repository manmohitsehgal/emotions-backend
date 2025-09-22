namespace Emotions.Domain.Entities;

public class Plan
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string TemplateId { get; set; } = null!;
    public string Title { get; set; } = null!;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
}