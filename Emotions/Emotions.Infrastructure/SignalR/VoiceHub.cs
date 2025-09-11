using Microsoft.AspNetCore.SignalR;

namespace Emotions.Infrastructure.SignalR
{
    public partial class VoiceHub : Hub
    {
        public async Task RaiseHand(Guid roomId, Guid userId)
        {
            await Clients.Group(roomId.ToString()).SendAsync("HandRaised", new { roomId, userId });
        }

        public async Task GrantMic(Guid roomId, Guid userId)
        {
            await Clients.Group(roomId.ToString()).SendAsync("MicGranted", new { roomId, userId });
        }

        public async Task Kick(Guid roomId, Guid userId, string? reason = null)
        {
            await Clients.Group(roomId.ToString()).SendAsync("UserKicked", new { roomId, userId, reason });
        }

        public async Task Ban(Guid roomId, Guid userId, int minutes, string? reason = null)
        {
            await Clients.Group(roomId.ToString()).SendAsync("UserBanned", new { roomId, userId, minutes, reason });
        }

        public async Task DropToolcard(Guid roomId, string type, object payload)
        {
            await Clients.Group(roomId.ToString()).SendAsync("ToolcardDropped", new { roomId, type, payload });
        }
    }
}