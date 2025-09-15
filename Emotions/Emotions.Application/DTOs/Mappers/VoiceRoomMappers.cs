using Emotions.Application.DTOs.Rooms;
using Emotions.Domain.Entities;

namespace Emotions.Application.DTOs.Mappers
{
    public static class VoiceRoomMappers
    {
        public static RoomSummaryDto ToSummary(this VoiceRoom r, int? approxMemberCount = null) => new()
        {
            Id = r.Id,
            Title = r.Title,
            Theme = r.Theme.ToString().ToLowerInvariant(),
            IsLive = r.Status == VoiceRoomStatus.Live && !r.IsDeleted,
            MemberCount = approxMemberCount,
            MaxParticipants = r.MaxParticipants,
            LastActiveAt = r.LastActiveAt ?? r.UpdatedAt ?? r.CreatedAt,
            Language = r.Language,
            SpeakPolicy = r.SpeakPolicy.ToString() switch
            {
                nameof(VoiceRoomSpeakPolicy.ModeratorOnly) => "moderatorOnly",
                nameof(VoiceRoomSpeakPolicy.OpenButRateLimited) => "openButRateLimited",
                _ => "raiseHand"
            },
            ThumbnailUrl = r.ThumbnailUrl
        };

        public static RoomDetailDto ToDetail(this VoiceRoom r, int? approxMemberCount = null)
        {
            var d = ToSummary(r, approxMemberCount);
            return new RoomDetailDto
            {
                Id = d.Id,
                Title = d.Title,
                Theme = d.Theme,
                IsLive = d.IsLive,
                MemberCount = d.MemberCount,
                MaxParticipants = d.MaxParticipants,
                LastActiveAt = d.LastActiveAt,
                Language = d.Language,
                SpeakPolicy = d.SpeakPolicy,
                ThumbnailUrl = d.ThumbnailUrl,
                Prompt = r.Prompt,
                Topic = r.Topic,
                Description = r.Description,
                Rules = Array.Empty<string>()
            };
        }
    }
}