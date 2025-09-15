using System.Security.Claims;
using Emotions.Application.DTOs.Rooms;
using Emotions.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Emotions.Infrastructure.SignalR
{
    public partial class VoiceHub : Hub<IVoiceClient>
    {
        private readonly IPresenceService _presence; // or IVoicePresenceService if you prefer the interface
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
        public async Task JoinRoom(Guid roomId)
        {
            var (userIdGuid, userIdStr) = GetUserIdGuidOrThrow();
            var connId = Context.ConnectionId;
            var group = roomId.ToString();

            // Add to SignalR group first (so broadcasts reach caller too if needed)
            await Groups.AddToGroupAsync(connId, group);

            // Prepare participant attrs (adjust display name source as needed)
            var participant = new ParticipantDto
            {
                UserId = userIdStr, // must be a GUID string (see helper)
                DisplayName = GetDisplayName() ?? "User",
                IsMuted = true, // default self-muted on join
                IsVideoOn = false
            };

            // Update Redis presence & connection index
            var isNewInRoom = await _presence.AddAsync(roomId, participant, connId);

            // Broadcast membership events
            if (isNewInRoom)
            {
                await Clients.Group(group).MemberJoined(new
                {
                    roomId,
                    userId = userIdStr,
                    displayName = participant.DisplayName
                });
            }

            // Count & broadcast
            var count = _presence.GetApproxMemberCount(roomId) ?? 0;
            await Clients.Group(group).MemberCountUpdated(new { roomId, count });

            // Optional: send roster to the caller for immediate UI
            var roster = await _presence.GetParticipantsAsync(roomId);
            await Clients.Caller.RoomRoster(roster);

            _logger.LogInformation("JoinRoom: user {UserId} conn {Conn} room {Room}", userIdStr, connId, roomId);
        }

        // Client: invoke("LeaveRoom", roomIdGuid)
        public async Task LeaveRoom(Guid roomId)
        {
            var (_, userIdStr) = GetUserIdGuidOrThrow();
            var connId = Context.ConnectionId;
            var group = roomId.ToString();

            // Remove only THIS connection from presence
            // We need data for broadcasts BEFORE we erase the index:
            // but you already pass roomId, so we can use it directly.
            await _presence.RemoveByConnectionAsync(connId);

            await Groups.RemoveFromGroupAsync(connId, group);

            // Broadcast
            await Clients.Group(group).MemberLeft(new { roomId, userId = userIdStr });

            var count = _presence.GetApproxMemberCount(roomId) ?? 0;
            await Clients.Group(group).MemberCountUpdated(new { roomId, count });

            _logger.LogInformation("LeaveRoom: user {UserId} conn {Conn} room {Room}", userIdStr, connId, roomId);
        }

        // Client: invoke("ToggleMute", isMutedBool)
        public async Task ToggleMute(bool isMuted)
        {
            var (userIdGuid, userIdStr) = GetUserIdGuidOrThrow();
            var connId = Context.ConnectionId;

            // Find current room for this connection
            var (roomIdMaybe, _) = await _presence.FindByConnectionAsync(connId);
            if (roomIdMaybe is null)
                throw new HubException("Not currently in a room.");

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
            // We need room + user BEFORE we clear the connection index:
            var connId = Context.ConnectionId;
            var (roomIdMaybe, participant) = await _presence.FindByConnectionAsync(connId);

            if (roomIdMaybe is not null && participant is not null)
            {
                var roomId = roomIdMaybe.Value;
                var group = roomId.ToString();

                await _presence.RemoveByConnectionAsync(connId);
                await Groups.RemoveFromGroupAsync(connId, group);

                await Clients.Group(group).MemberLeft(new { roomId, userId = participant.UserId });

                var count = _presence.GetApproxMemberCount(roomId) ?? 0;
                await Clients.Group(group).MemberCountUpdated(new { roomId, count });

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

        // Returns (Guid, string) where string is the Guid.ToString()
        private (Guid guid, string str) GetUserIdGuidOrThrow()
        {
            // Prefer NameIdentifier; fall back to "sub"
            var id = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? Context.User?.FindFirst("sub")?.Value;

            if (string.IsNullOrWhiteSpace(id))
                throw new HubException("Unauthorized: missing user id.");

            // Your Redis layer requires GUID user ids (Guid.Parse is used internally).
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