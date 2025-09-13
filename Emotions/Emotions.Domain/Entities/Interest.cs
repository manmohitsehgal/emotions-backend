namespace Emotions.Domain.Entities;

public class Interest
{
    public int Id { get; set; }
    public required string Name { get; set; } = default!; // e.g., "Stress"
    public required string Slug { get; set; } = default!; // e.g., "stress"
    public ICollection<UserInterest> UserInterests { get; set; } = new List<UserInterest>();
}