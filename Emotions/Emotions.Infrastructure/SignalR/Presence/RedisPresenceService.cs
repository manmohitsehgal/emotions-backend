using Emotions.Application.DTOs.Rooms;
using Emotions.Application.Interfaces;
using StackExchange.Redis;

namespace Emotions.Infrastructure.SignalR.Presence;

public sealed class RedisPresenceService : IPresenceService, IAsyncDisposable
{
    private readonly ConnectionMultiplexer _mux;
    private readonly IDatabase _db;
    private readonly string _prefix;

    // KEYS layout (per env via _prefix):
    // presence:{prefix}:room:{roomId}:users           (SET of userId)
    // presence:{prefix}:room:{roomId}:user:{userId}:conns  (SET of connectionId)
    // presence:{prefix}:room:{roomId}:user:{userId}:attrs  (HASH displayName,isMuted,isVideoOn)
    // presence:{prefix}:conn:{connectionId}                (HASH roomId,userId)

    public RedisPresenceService(string connectionString, string keyPrefix)
    {
        _mux = ConnectionMultiplexer.Connect(connectionString);
        _db = _mux.GetDatabase();
        _prefix = string.IsNullOrWhiteSpace(keyPrefix) ? "dev" : keyPrefix;
    }

    private RedisKey RoomUsers(Guid roomId) => $"presence:{_prefix}:room:{roomId}:users";
    private RedisKey RoomUserConns(Guid roomId, Guid userId) => $"presence:{_prefix}:room:{roomId}:user:{userId}:conns";
    private RedisKey RoomUserAttrs(Guid roomId, Guid userId) => $"presence:{_prefix}:room:{roomId}:user:{userId}:attrs";
    private RedisKey ConnIndex(string connId) => $"presence:{_prefix}:conn:{connId}";

    public async Task<bool> AddAsync(Guid roomId, ParticipantDto participant, string connectionId)
    {
        // Lua ensures atomicity across multiple keys
        const string lua = @"
            -- KEYS[1]=users, KEYS[2]=conns, KEYS[3]=connIdx, KEYS[4]=attrs
            -- ARGV[1]=userId, ARGV[2]=connectionId, ARGV[3]=roomId, ARGV[4]=displayName, ARGV[5]=isMuted, ARGV[6]=isVideoOn
            redis.call('SADD', KEYS[2], ARGV[2])
            redis.call('HMSET', KEYS[3], 'roomId', ARGV[3], 'userId', ARGV[1])
            redis.call('HMSET', KEYS[4], 'displayName', ARGV[4], 'isMuted', ARGV[5], 'isVideoOn', ARGV[6])
            local added = redis.call('SADD', KEYS[1], ARGV[1])
            return added
        ";

        var result = (int)(long)await _db.ScriptEvaluateAsync(
            lua,
            keys: new RedisKey[]
            {
                RoomUsers(roomId), RoomUserConns(roomId, Guid.Parse(participant.UserId)), ConnIndex(connectionId),
                RoomUserAttrs(roomId, Guid.Parse(participant.UserId))
            },
            values: new RedisValue[]
            {
                participant.UserId,
                connectionId,
                roomId.ToString(),
                participant.DisplayName ?? "",
                participant.IsMuted ? "1" : "0",
                participant.IsVideoOn ? "1" : "0"
            });

        return result == 1; // 1 means user newly present in the room
    }

    public async Task RemoveByConnectionAsync(string connectionId)
    {
        const string lua = @"
            -- KEYS[1]=connIdx
            local roomId = redis.call('HGET', KEYS[1], 'roomId')
            local userId = redis.call('HGET', KEYS[1], 'userId')
            if not roomId or not userId then
                redis.call('DEL', KEYS[1])
                return 0
            end
            local usersKey = 'presence:""$prefix"":room:' .. roomId .. ':users'
            local connsKey = 'presence:""$prefix"":room:' .. roomId .. ':user:' .. userId .. ':conns'
            local attrsKey = 'presence:""$prefix"":room:' .. roomId .. ':user:' .. userId .. ':attrs'
            -- remove conn from set
            redis.call('SREM', connsKey, ARGV[1])
            -- delete conn index
            redis.call('DEL', KEYS[1])
            -- if no more conns for that user, remove user from users set and attrs
            if redis.call('SCARD', connsKey) == 0 then
                redis.call('SREM', usersKey, userId)
                redis.call('DEL', attrsKey)
                redis.call('DEL', connsKey)
                return 1
            end
            return 0
        ";
        // Replace placeholder with prefix once to avoid string concat in Lua per call.
        var luaPrefixed = lua.Replace(@"""""$prefix""""", _prefix, StringComparison.Ordinal);

        _ = await _db.ScriptEvaluateAsync(
            luaPrefixed,
            keys: new RedisKey[] { ConnIndex(connectionId) },
            values: new RedisValue[] { connectionId });
    }

    public async Task RemoveAsync(Guid roomId, Guid userId)
    {
        // remove all conns for user, their attrs, and user from users set
        var connsKey = RoomUserConns(roomId, userId);
        var connIds = await _db.SetMembersAsync(connsKey);
        if (connIds.Length > 0)
        {
            var batch = _db.CreateBatch();
            foreach (var c in connIds)
                batch.KeyDeleteAsync(ConnIndex(c!));
            batch.Execute();
        }

        await _db.KeyDeleteAsync(connsKey);
        await _db.KeyDeleteAsync(RoomUserAttrs(roomId, userId));
        await _db.SetRemoveAsync(RoomUsers(roomId), userId.ToString());
    }

    public async Task<IReadOnlyList<ParticipantDto>> GetParticipantsAsync(Guid roomId)
    {
        var users = await _db.SetMembersAsync(RoomUsers(roomId));
        if (users.Length == 0) return Array.Empty<ParticipantDto>();

        var tasks = new Task<HashEntry[]>[users.Length];
        for (int i = 0; i < users.Length; i++)
        {
            var uid = Guid.Parse(users[i]!);
            tasks[i] = _db.HashGetAllAsync(RoomUserAttrs(roomId, uid));
        }

        await Task.WhenAll(tasks);

        var list = new List<ParticipantDto>(users.Length);
        for (int i = 0; i < users.Length; i++)
        {
            var uid = users[i]!.ToString();
            var attrs = tasks[i].Result;
            list.Add(new ParticipantDto
            {
                UserId = uid,
                DisplayName = attrs.FirstOrDefault(h => h.Name == "displayName").Value.ToString(),
                IsMuted = ToBool(attrs.FirstOrDefault(h => h.Name == "isMuted").Value),
                IsVideoOn = ToBool(attrs.FirstOrDefault(h => h.Name == "isVideoOn").Value)
            });
        }

        return list;
    }

    public async Task<ParticipantDto?> GetAsync(Guid roomId, Guid userId)
    {
        var attrs = await _db.HashGetAllAsync(RoomUserAttrs(roomId, userId));
        if (attrs.Length == 0) return null;

        return new ParticipantDto
        {
            UserId = userId.ToString(),
            DisplayName = attrs.FirstOrDefault(h => h.Name == "displayName").Value.ToString(),
            IsMuted = ToBool(attrs.FirstOrDefault(h => h.Name == "isMuted").Value),
            IsVideoOn = ToBool(attrs.FirstOrDefault(h => h.Name == "isVideoOn").Value)
        };
    }

    public async Task UpdateAsync(Guid roomId, Guid userId, bool? isMuted = null, bool? isVideoOn = null)
    {
        var fields = new List<HashEntry>();
        if (isMuted is not null) fields.Add(new HashEntry("isMuted", isMuted.Value ? "1" : "0"));
        if (isVideoOn is not null) fields.Add(new HashEntry("isVideoOn", isVideoOn.Value ? "1" : "0"));
        if (fields.Count > 0)
            await _db.HashSetAsync(RoomUserAttrs(roomId, userId), fields.ToArray());
    }

    public async Task<(Guid? roomId, ParticipantDto? participant)> FindByConnectionAsync(string connectionId)
    {
        var idx = await _db.HashGetAllAsync(ConnIndex(connectionId));
        if (idx.Length == 0) return (null, null);

        var roomId = Guid.Parse(idx.First(h => h.Name == "roomId").Value!);
        var userId = Guid.Parse(idx.First(h => h.Name == "userId").Value!);
        var p = await GetAsync(roomId, userId);
        return (roomId, p);
    }

    // Counts for summaries
    public int? GetApproxMemberCount(Guid roomId)
        => (int)_db.SetLength(RoomUsers(roomId));

    public IDictionary<Guid, int?> GetApproxMemberCounts(IEnumerable<Guid> roomIds)
    {
        var ids = roomIds.ToArray();
        var results = new Dictionary<Guid, int?>(ids.Length);
        var batch = _db.CreateBatch();
        var tasks = new Task<long>[ids.Length];

        for (int i = 0; i < ids.Length; i++)
            tasks[i] = batch.SetLengthAsync(RoomUsers(ids[i]));

        batch.Execute();
        for (int i = 0; i < ids.Length; i++)
            results[ids[i]] = (int)tasks[i].Result;

        return results;
    }

    private static bool ToBool(RedisValue v)
        => v.HasValue && (v.ToString() == "1" || v.ToString().Equals("true", StringComparison.OrdinalIgnoreCase));

    public async ValueTask DisposeAsync()
    {
        await _mux.CloseAsync();
        _mux.Dispose();
    }
}