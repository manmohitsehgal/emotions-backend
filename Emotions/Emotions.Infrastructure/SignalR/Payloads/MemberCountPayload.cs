namespace Emotions.Infrastructure.SignalR
{
    public readonly record struct MemberCountPayload(Guid RoomId, int Count);
}