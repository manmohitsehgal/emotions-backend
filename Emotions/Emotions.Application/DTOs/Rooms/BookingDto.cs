using Emotions.Domain.Enums;

namespace Emotions.Application.DTOs.Rooms;

public sealed record BookingDto(Guid SessionId, BookingStatus Status, bool IsPremium);