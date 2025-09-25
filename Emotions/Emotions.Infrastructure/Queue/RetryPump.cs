using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Emotions.Infrastructure.Queue;

public sealed class RetryPump : BackgroundService
{
    private readonly ILogger<RetryPump> _logger;
    private readonly RedisTranscriptionQueue _queue;

    public RetryPump(ILogger<RetryPump> logger, RedisTranscriptionQueue queue)
    {
        _logger = logger;
        _queue = queue;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RetryPump started");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                int moved = await _queue.MoveDueRetriesAsync();
                if (moved > 0) _logger.LogDebug("Moved {Count} due retry items", moved);
                await Task.Delay(1000, stoppingToken);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RetryPump error");
                await Task.Delay(2000, stoppingToken);
            }
        }

        _logger.LogInformation("RetryPump stopping");
    }
}