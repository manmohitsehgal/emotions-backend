namespace Emotions.Domain.Entities;

public class UserInterest
{
    public Guid UserId { get; set; }
    public int InterestId { get; set; }
    public User User { get; set; } = default!;
    public Interest Interest { get; set; } = default!;
}