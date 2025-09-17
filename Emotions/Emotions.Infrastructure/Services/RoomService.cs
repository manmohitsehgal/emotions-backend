using Emotions.Application.Common;
using Emotions.Application.DTOs;
using Emotions.Application.DTOs.Mappers;
using Emotions.Application.DTOs.Rooms;
using Emotions.Application.DTOs.VoiceRooms.Queries;
using Emotions.Application.Interfaces;
using Emotions.Domain.Entities;
using Emotions.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Emotions.Infrastructure.Services
{
    public sealed class RoomService : IRoomService
    {
        private readonly AppDbContext _db;
        private readonly IPresenceService _presence;


        public RoomService(AppDbContext db, IPresenceService presence)
        {
            _db = db;
            _presence = presence;
        }


        public async Task<PagedResult<RoomSummaryDto>> ListAsync(RoomListQuery query,
            CancellationToken ct = default)
        {
            var q = _db.VoiceRooms.AsNoTracking().Where(r => !r.IsDeleted);

            if (!string.IsNullOrWhiteSpace(query.Theme) &&
                Enum.TryParse<RoomTheme>(query.Theme, true, out var theme))
                q = q.Where(r => r.Theme == theme);


            if (!string.IsNullOrWhiteSpace(query.Language))
                q = q.Where(r => r.Language == query.Language);


            if (!string.IsNullOrWhiteSpace(query.Status) &&
                Enum.TryParse<RoomStatus>(query.Status, true, out var status))
                q = q.Where(r => r.Status == status);
            else
                q = q.Where(r => r.Status == RoomStatus.Live);


            if (!string.IsNullOrWhiteSpace(query.Q))
                q = q.Where(r => r.Title.Contains(query.Q));


            q = q.OrderByDescending(r => r.LastActiveAt ?? r.UpdatedAt ?? r.CreatedAt);


            var page = Math.Max(1, query.Page);
            var size = Math.Clamp(query.PageSize, 1, 100);
            var items = await q.Skip((page - 1) * size).Take(size).ToListAsync(ct);


            var counts = _presence.GetApproxMemberCounts(items.Select(i => i.Id));
            var summaries = items.Select(i => i.ToSummary(counts.TryGetValue(i.Id, out var c) ? c : null)).ToList();


            return new PagedResult<RoomSummaryDto> { Items = summaries, Page = page, PageSize = size };
        }


        public async Task<RoomDetailDto?> GetAsync(Guid id, CancellationToken ct = default)
        {
            var r = await _db.VoiceRooms.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
            if (r is null) return null;
            var count = _presence.GetApproxMemberCount(id);
            return r.ToDetail(count);
        }

        public async Task<ParticipantsListDto> GetParticipantsAsync(Guid id, CancellationToken ct = default)
        {
            var participants = await _db.VoiceRoomConnections
                .AsNoTracking()
                .Where(c => c.RoomId == id && c.DisconnectedAt == null) // online only
                .OrderByDescending(c => c.ConnectedAt)
                .Select(c => new ParticipantDto
                {
                    UserId = c.UserId.ToString(),
                    DisplayName = c.Username,
                    IsMuted = c.IsMuted,
                    IsVideoOn = c.IsVideoOn
                })
                .ToListAsync(ct); // <-- async terminal op makes it awaitable

            return new ParticipantsListDto
            {
                RoomId = id,
                Participants = participants
            };
        }
    }
}