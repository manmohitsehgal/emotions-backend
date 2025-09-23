namespace Emotions.Domain.Entities;

public class PlanAdherence
{
    public Guid Id { get; set; }
    public Guid PlanId { get; set; }
    public DateTime Date { get; set; }
    public bool Done { get; set; }
}