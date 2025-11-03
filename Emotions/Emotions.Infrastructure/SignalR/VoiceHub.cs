using System.Security.Claims;
using Emotions.Application.DTOs.Rooms;
using Emotions.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Emotions.Infrastructure.SignalR
{
    [Authorize]
    public class VoiceHub : Hub<IVoiceClient>
    {
        private readonly IPresenceService _presence;
        private readonly ILogger<VoiceHub> _logger;

        public VoiceHub(IPresenceService presence, ILogger<VoiceHub> logger)
        {
            _presence = presence;
            _logger = logger;
        }

        public async Task RaiseHand(Guid roomId, Guid userId)
        {
            await Clients.Group(roomId.ToString()).HandRaised(new { roomId, userId });
        }

        public async Task GrantMic(Guid roomId, Guid userId)
        {
            await Clients.Group(roomId.ToString()).MicGranted(new { roomId, userId });
        }

        public async Task Kick(Guid roomId, Guid userId, string? reason = null)
        {
            await Clients.Group(roomId.ToString()).UserKicked(new { roomId, userId, reason });
        }

        public async Task Ban(Guid roomId, Guid userId, int minutes, string? reason = null)
        {
            await Clients.Group(roomId.ToString()).UserBanned(new { roomId, userId, minutes, reason });
        }

        public async Task DropToolcard(Guid roomId, string type, object payload)
        {
            await Clients.Group(roomId.ToString()).ToolcardDropped(new { roomId, type, payload });
        }

        // Client: invoke("JoinRoom", roomIdGuid)
        public async Task<(IReadOnlyList<ParticipantDto> roster, int count)> JoinRoom(Guid roomId)
        {
            var (userIdGuid, userIdStr) = GetUserIdGuidOrThrow();
            var connId = Context.ConnectionId;
            var group = roomId.ToString();

            await Groups.AddToGroupAsync(connId, group);

            var participant = new ParticipantDto
            {
                UserId = userIdStr, // GUID string
                DisplayName = GetDisplayName() ?? "User",
                IsMuted = true,
                IsVideoOn = false
            };

            bool isNewInRoom;
            try
            {
                isNewInRoom = await _presence.AddAsync(roomId, participant, connId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "JoinRoom presence failed for {Room} {Conn}", roomId, connId);
                await Groups.RemoveFromGroupAsync(connId, group);
                throw;
            }

            if (isNewInRoom)
            {
                await Clients.Group(group).MemberJoined(new
                {
                    roomId,
                    userId = userIdStr,
                    displayName = participant.DisplayName
                });
            }

            var count = _presence.GetApproxMemberCount(roomId) ?? 0;
            await Clients.Group(group).MemberCountUpdated(new MemberCountPayload(roomId, count));

            var roster = await _presence.GetParticipantsAsync(roomId);
            await Clients.Caller.RoomRoster(roster);

            _logger.LogInformation("JoinRoom: user {UserId} conn {Conn} room {Room}", userIdStr, connId, roomId);

            return (roster, count);
        }

        // Client: invoke("LeaveRoom", roomIdGuid)
        public async Task LeaveRoom(Guid roomId)
        {
            var (_, userIdStr) = GetUserIdGuidOrThrow();
            var connId = Context.ConnectionId;
            var group = roomId.ToString();

            try
            {
                await _presence.RemoveByConnectionAsync(connId);
            }
            finally
            {
                await Groups.RemoveFromGroupAsync(connId, group);
            }

            await Clients.Group(group).MemberLeft(new { roomId, userId = userIdStr });

            var count = _presence.GetApproxMemberCount(roomId) ?? 0;
            await Clients.Group(group).MemberCountUpdated(new MemberCountPayload(roomId, count));

            _logger.LogInformation("LeaveRoom: user {UserId} conn {Conn} room {Room}", userIdStr, connId, roomId);
        }

        // Client: invoke("ToggleMute", isMutedBool)
        public async Task ToggleMute(bool isMuted)
        {
            var (userIdGuid, userIdStr) = GetUserIdGuidOrThrow();
            var connId = Context.ConnectionId;

            var (roomIdMaybe, _) = await _presence.FindByConnectionAsync(connId);
            if (roomIdMaybe is null)
            {
                _logger.LogWarning("ToggleMute with no room: {Conn}", connId);
                throw new HubException("Not currently in a room.");
            }

            var roomId = roomIdMaybe.Value;

            await _presence.UpdateAsync(roomId, userIdGuid, isMuted: isMuted, isVideoOn: null);

            await Clients.Group(roomId.ToString()).MuteChanged(new
            {
                roomId,
                userId = userIdStr,
                isMuted
            });

            _logger.LogInformation("ToggleMute: user {UserId} mute={Muted} room {Room}", userIdStr, isMuted, roomId);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var connId = Context.ConnectionId;
            var (roomIdMaybe, participant) = await _presence.FindByConnectionAsync(connId);

            if (roomIdMaybe is Guid roomId && participant is not null)
            {
                var group = roomId.ToString();

                try
                {
                    await _presence.RemoveByConnectionAsync(connId);
                }
                finally
                {
                    await Groups.RemoveFromGroupAsync(connId, group);
                }

                await Clients.Group(group).MemberLeft(new { roomId, userId = participant.UserId });

                var count = _presence.GetApproxMemberCount(roomId) ?? 0;
                await Clients.Group(group).MemberCountUpdated(new MemberCountPayload(roomId, count));

                _logger.LogInformation("Disconnect: user {UserId} conn {Conn} room {Room}", participant.UserId, connId,
                    roomId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        [Authorize(Roles = "Admin,Host")]
        public Task BroadcastSessionPublished(Guid sessionId, DateTimeOffset startAt)
            => Clients.All.SessionPublished(sessionId, startAt);

        [Authorize(Roles = "Admin,Host")]
        public Task BroadcastSeatingUpdated(Guid sessionId, int seatsTaken, int waitlistCount)
            => Clients.All.SeatingUpdated(sessionId, seatsTaken, waitlistCount);

        [Authorize(Roles = "Admin,Host")]
        public Task BroadcastBookingPromoted(Guid sessionId, Guid userId)
            => Clients.User(userId.ToString()).BookingPromoted(sessionId);

        [Authorize(Roles = "Admin,Host")]
        public Task BroadcastSessionLive(Guid sessionId, Guid voiceRoomId)
            => Clients.All.SessionLive(sessionId, voiceRoomId);

        [Authorize(Roles = "Admin,Host")]
        public Task BroadcastSessionCompleted(Guid sessionId)
            => Clients.All.SessionCompleted(sessionId);

        // -------- helpers

        private (Guid guid, string str) GetUserIdGuidOrThrow()
        {
            var id = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? Context.User?.FindFirst("sub")?.Value;

            if (string.IsNullOrWhiteSpace(id))
                throw new HubException("Unauthorized: missing user id.");

            if (!Guid.TryParse(id, out var parsed))
                throw new HubException("Invalid user id. Expected GUID in user identity claim.");

            return (parsed, parsed.ToString());
        }

        private string? GetDisplayName()
        {
            return Context.User?.FindFirst("name")?.Value
                   ?? Context.User?.Identity?.Name
                   ?? Context.User?.FindFirst("preferred_username")?.Value
                   ?? Context.User?.FindFirst(ClaimTypes.Email)?.Value;
        }
    }
}