namespace Emotions.Domain.Entities;

public class SessionHost
{
    public Guid Id { get; set; }
    public bool IsAi { get; set; } // true => AI host


// Human host metadata
    public Guid? UserId { get; set; } // internal user
    public string? DisplayName { get; set; }
    public string? Credentials { get; set; } // e.g., LSW, LCSW
    public string? Bio { get; set; }


// AI config
    public string? AiPersonaKey { get; set; } // maps to prompt preset in Python service
}