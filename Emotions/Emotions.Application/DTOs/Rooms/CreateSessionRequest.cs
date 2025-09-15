using Emotions.Domain.Enums;

namespace Emotions.Application.DTOs.Rooms;

public sealed record CreateSessionRequest(
    string Title,
    string? Description,
    SessionType Type,
    SpeakPolicy SpeakPolicy,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    int Capacity,
    bool AllowListeners,
    Guid HostId,
    Guid? TemplateId);