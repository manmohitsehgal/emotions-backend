using Emotions.Application.Interfaces;
using Emotions.Domain.Enums;
using Emotions.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Emotions.Infrastructure.BackgroundJobs;

public class SessionLifecycleWorker : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<SessionLifecycleWorker> _log;

    public SessionLifecycleWorker(IServiceProvider sp, ILogger<SessionLifecycleWorker> log)
    {
        _sp = sp;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await using var scope = _sp.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var svc = scope.ServiceProvider.GetRequiredService<ISessionsService>(); // your domain service
                var now = DateTimeOffset.UtcNow;

                // 1) Activate: Published sessions that should be live now
                var due = await db.SupportSessions
                    .Where(s => s.Status == SessionStatus.Published && s.StartAt <= now && s.EndAt > now)
                    .OrderBy(s => s.StartAt)
                    .Take(20)
                    .Select(s => s.Id)
                    .ToListAsync(ct);

                foreach (var sid in due)
                {
                    try
                    {
                        var roomId = Guid.NewGuid();
                        await svc.GoLiveAsync(sid, roomId, ct);
                    }
                    catch (Exception ex)
                    {
                        _log.LogError(ex, "GoLive failed for {SessionId}", sid);
                    }
                }

                // 2) Complete: Live sessions whose end time has passed
                var toComplete = await db.SupportSessions
                    .Where(s => s.Status == SessionStatus.Live && s.EndAt <= now)
                    .OrderBy(s => s.EndAt)
                    .Take(50)
                    .Select(s => s.Id)
                    .ToListAsync(ct);

                foreach (var sid in toComplete)
                {
                    try
                    {
                        await svc.CompleteAsync(sid, ct);
                    }
                    catch (Exception ex)
                    {
                        _log.LogError(ex, "Complete failed for {SessionId}", sid);
                    }
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "SessionLifecycle tick failed");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(15), ct);
            } // tune as needed
            catch (TaskCanceledException)
            {
            }
        }
    }
}