namespace Emotions.Domain.Entities;

public class PlanStepStatus
{
    public Guid PlanStepId { get; set; }
    public Guid UserId { get; set; }
    public bool IsDone { get; set; }
    public DateTime? CompletedAt { get; set; }
    public PlanStep PlanStep { get; set; } = null!;
}