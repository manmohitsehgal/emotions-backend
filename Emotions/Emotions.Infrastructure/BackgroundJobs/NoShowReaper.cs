using Emotions.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
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
    private readonly IServiceProvider _sp;
    private readonly ILogger<NoShowReaper> _log;
    private readonly NoShowReaperOptions _opts;

    public NoShowReaper(
        IServiceProvider sp,
        IOptions<NoShowReaperOptions> opts,
        ILogger<NoShowReaper> log)
    {
        _sp = sp;
        _log = log;
        _opts = opts.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _sp.CreateScope();
                var bookings = scope.ServiceProvider.GetRequiredService<IBookingsService>();
                await bookings.AutoReleaseNoShowsAsync(_opts.Grace, stoppingToken);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "NoShowReaper error");
            }

            try
            {
                await Task.Delay(_opts.Period, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                /* shutting down */
            }
        }
    }
}