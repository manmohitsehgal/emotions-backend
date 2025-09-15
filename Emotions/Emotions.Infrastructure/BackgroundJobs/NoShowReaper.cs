using Emotions.Application.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Emotions.Infrastructure.BackgroundJobs;

public sealed class NoShowReaperOptions
{
    // use set; so Configure<T> can assign
    public TimeSpan Grace { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan Period { get; set; } = TimeSpan.FromSeconds(60);
}

public class NoShowReaper : BackgroundService
{
    private readonly IBookingsService _bookings;
    private readonly ILogger<NoShowReaper> _log;
    private readonly NoShowReaperOptions _opts;

    public NoShowReaper(
        IBookingsService bookings,
        IOptions<NoShowReaperOptions> opts,
        ILogger<NoShowReaper> log)
    {
        _bookings = bookings;
        _log = log;
        _opts = opts.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_opts.Period);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                var changed = await _bookings.AutoReleaseNoShowsAsync(_opts.Grace, stoppingToken);
                if (changed > 0)
                    _log.LogInformation("NoShowReaper released/promoted {Count} bookings.", changed);
            }
            catch (OperationCanceledException)
            {
                /* shutting down */
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "NoShowReaper error");
            }
        }
    }
}