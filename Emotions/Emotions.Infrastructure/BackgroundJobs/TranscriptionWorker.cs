using System.Threading.Channels;
using Emotions.Application.Interfaces.Transcription;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Emotions.Infrastructure.BackgroundJobs;

public sealed class InMemoryTranscriptionQueue : ITranscriptionJobQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>();
    public ValueTask QueueAsync(Guid attachmentId) => _channel.Writer.WriteAsync(attachmentId);

    public async ValueTask<Guid?> DequeueAsync(CancellationToken ct)
    {
        if (await _channel.Reader.WaitToReadAsync(ct) && _channel.Reader.TryRead(out var id))
            return id;
        return null;
    }
}

public sealed class TranscriptionWorker : BackgroundService
{
    private readonly ILogger<TranscriptionWorker> _logger;
    private readonly IServiceProvider _services;
    private readonly ITranscriptionJobQueue _queue;
    private readonly ITranscriptionService _service;

    public TranscriptionWorker(ILogger<TranscriptionWorker> logger, IServiceProvider services,
        ITranscriptionJobQueue queue, ITranscriptionService service)
    {
        _logger = logger;
        _services = services;
        _queue = queue;
        _service = service;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TranscriptionWorker started");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var id = await _queue.DequeueAsync(stoppingToken);
                if (id is null) continue;
                await _service.StartTranscriptionAsync(id.Value, stoppingToken);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TranscriptionWorker");
            }
        }

        _logger.LogInformation("TranscriptionWorker stopping");
    }
}