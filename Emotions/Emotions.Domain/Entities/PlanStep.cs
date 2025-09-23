namespace Emotions.Domain.Entities;

public class PlanStep
{
    public Guid Id { get; set; }
    public Guid PlanId { get; set; }
    public int DayNumber { get; set; }
    public string Text { get; set; } = null!;
}