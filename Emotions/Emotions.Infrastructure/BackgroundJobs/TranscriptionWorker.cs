using Emotions.Application.Interfaces.Transcription;
using Emotions.Domain.Entities;
using Emotions.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Emotions.Infrastructure.BackgroundJobs;

public sealed class TranscriptionWorker : BackgroundService
{
    private readonly ILogger<TranscriptionWorker> _logger;
    private readonly IServiceProvider _services;
    private readonly ITranscriptionJobQueue _queue;
    private readonly IRetryScheduler _retry;
    private const int MaxAttempts = 5;

    public TranscriptionWorker(ILogger<TranscriptionWorker> logger, IServiceProvider services,
        ITranscriptionJobQueue queue, IRetryScheduler retry)
    {
        _logger = logger;
        _services = services;
        _queue = queue;
        _retry = retry;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TranscriptionWorker started (Redis-backed)");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var id = await _queue.DequeueAsync(stoppingToken);
                if (id is null) continue;

                using var scope = _services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var svc = scope.ServiceProvider.GetRequiredService<ITranscriptionService>();

                var tr = await db.Set<JournalAttachmentTranscripts>()
                    .FirstOrDefaultAsync(t => t.AttachmentId == id.Value, stoppingToken);

                if (tr is null)
                {
                    tr = new JournalAttachmentTranscripts { AttachmentId = id.Value, Status = "Processing" };
                    db.Add(tr);
                    await db.SaveChangesAsync(stoppingToken);
                }

                if (tr.Attempts >= MaxAttempts)
                {
                    _logger.LogWarning("Attachment {AttachmentId} exceeded max attempts", id);
                    tr.Status = "Failed";
                    tr.LastError = "Max attempts exceeded";
                    await db.SaveChangesAsync(stoppingToken);
                    continue;
                }

                tr.Status = "Processing";
                tr.Attempts++;
                tr.LastTriedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(stoppingToken);

                try
                {
                    await svc.StartTranscriptionAsync(id.Value, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Transcription failed for {AttachmentId} (attempt {Attempt})", id,
                        tr.Attempts);
                    tr.Status = "Pending";
                    tr.LastError = ex.Message?.Length > 500 ? ex.Message.Substring(0, 500) : ex.Message;
                    await db.SaveChangesAsync(stoppingToken);

                    var delay = TimeSpan.FromSeconds(Math.Min(60, Math.Pow(2, tr.Attempts)));
                    await _retry.ScheduleRetryAsync(id.Value, delay, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Uncaught error in TranscriptionWorker");
                try
                {
                    await Task.Delay(2000, stoppingToken);
                }
                catch
                {
                }
            }
        }

        _logger.LogInformation("TranscriptionWorker stopping");
    }
}