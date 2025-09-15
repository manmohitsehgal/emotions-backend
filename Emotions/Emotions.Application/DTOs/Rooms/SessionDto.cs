using Emotions.Domain.Enums;

namespace Emotions.Application.DTOs.Rooms;

public sealed record SessionDto(
    Guid Id,
    string Title,
    string? Description,
    SessionType Type,
    SessionStatus Status,
    SpeakPolicy SpeakPolicy,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    int Capacity,
    bool AllowListeners,
    Guid HostId,
    bool HostIsAi,
    string? HostDisplayName,
    string? HostCredentials,
    Guid? VoiceRoomId,
    int SeatsTaken,
    int WaitlistCount);