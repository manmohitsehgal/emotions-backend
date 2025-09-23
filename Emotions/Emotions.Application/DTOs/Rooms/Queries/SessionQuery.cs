using Emotions.Domain.Enums;

namespace Emotions.Application.DTOs.Rooms.Queries;

public sealed record SessionsQuery(
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    SessionStatus[]? Statuses = null,
    SessionType[]? Types = null);