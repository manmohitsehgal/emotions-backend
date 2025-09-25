using Emotions.Application.Interfaces.Transcription;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Emotions.Infrastructure.Queue;

public sealed class RedisTranscriptionQueue(
    IConnectionMultiplexer mux,
    ILogger<RedisTranscriptionQueue> logger,
    string queueKey = "transcription:queue",
    string retryZsetKey = "transcription:retry")
    : ITranscriptionJobQueue, IRetryScheduler
{
    private readonly ILogger<RedisTranscriptionQueue> _logger = logger;
    private readonly IDatabase _db = mux.GetDatabase();
    private readonly string _queueKey = queueKey;
    private readonly string _retryZsetKey = retryZsetKey;

    public async ValueTask QueueAsync(Guid attachmentId)
    {
        await _db.ListLeftPushAsync(_queueKey, attachmentId.ToString("D"));
    }

    public async ValueTask<Guid?> DequeueAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var res = await _db.ListRightPopAsync(_queueKey);
            if (!res.IsNullOrEmpty)
            {
                if (Guid.TryParse(res!, out var id)) return id;
                _logger.LogWarning("Invalid Guid popped from queue: {Value}", (string)res);
            }

            try
            {
                await Task.Delay(1000, ct);
            }
            catch (OperationCanceledException)
            {
            }
        }

        return null;
    }

    public async ValueTask ScheduleRetryAsync(Guid attachmentId, TimeSpan delay, CancellationToken ct = default)
    {
        var when = DateTimeOffset.UtcNow.Add(delay).ToUnixTimeMilliseconds();
        await _db.SortedSetAddAsync(_retryZsetKey, attachmentId.ToString("D"), when);
    }

    public async Task<int> MoveDueRetriesAsync()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var due = await _db.SortedSetRangeByScoreAsync(_retryZsetKey, stop: now, take: 50);
        int moved = 0;
        foreach (var item in due)
        {
            if (Guid.TryParse(item!, out var id))
            {
                var tran = _db.CreateTransaction();
                _ = tran.SortedSetRemoveAsync(_retryZsetKey, item);
                _ = tran.ListLeftPushAsync(_queueKey, id.ToString("D"));
                bool ok = await tran.ExecuteAsync();
                if (ok) moved++;
            }
            else
            {
                await _db.SortedSetRemoveAsync(_retryZsetKey, item);
            }
        }

        return moved;
    }
}