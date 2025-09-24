using Emotions.Application.Storage;
using Emotions.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Emotions.Infrastructure.BackgroundJobs;

public sealed class PendingBlobReaper : BackgroundService
{
    private readonly AppDbContext _db;
    private readonly IBlobStorage _storage;
    private readonly ILogger<PendingBlobReaper> _log;


    public PendingBlobReaper(AppDbContext db, IBlobStorage storage, ILogger<PendingBlobReaper> log)
    {
        _db = db;
        _storage = storage;
        _log = log;
    }


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var cutoff = DateTime.UtcNow.AddHours(-24);
                var stale = await _db.PresignedUploadLogs
                    .Where(x => x.CreatedAt < cutoff)
                    .ToListAsync(stoppingToken);
                foreach (var s in stale)
                {
                    await _storage.DeleteAsync(s.BlobKey);
                    _db.Remove(s);
                }

                if (stale.Count > 0) await _db.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "PendingBlobReaper failed");
            }


            await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
        }
    }
}