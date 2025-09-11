using Emotions.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Emotions.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    // --- DbSets ---
    public DbSet<VoiceRoom> VoiceRooms { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    public DbSet<VoiceRoomMember> VoiceRoomMembers => Set<VoiceRoomMember>();
    public DbSet<VoiceRoomConnection> VoiceRoomConnections => Set<VoiceRoomConnection>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

// ---------------- VoiceRoom ----------------
        modelBuilder.Entity<VoiceRoom>(e =>
        {
            e.HasKey(x => x.Id);

            e.Property(x => x.Title).IsRequired().HasMaxLength(120);
            e.Property(x => x.Prompt).HasMaxLength(240);
            e.Property(x => x.Topic).HasMaxLength(120);
            e.Property(x => x.Description).HasMaxLength(2048);
            e.Property(x => x.ThumbnailUrl).HasMaxLength(2083);
            e.Property(x => x.Language).IsRequired().HasMaxLength(10);
            e.Property(x => x.RowVersion).IsRowVersion();

            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.LastActiveAt);
            e.HasIndex(x => new { x.Status, x.LastActiveAt });

            e.HasMany(x => x.Members)
                .WithOne(m => m.Room)
                .HasForeignKey(m => m.RoomId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasMany(x => x.Connections)
                .WithOne(c => c.Room)
                .HasForeignKey(c => c.RoomId)
                .OnDelete(DeleteBehavior.Restrict);
        });

// --------------- VoiceRoomMember (surrogate Id PK) ---------------
        modelBuilder.Entity<VoiceRoomMember>(e =>
        {
            e.HasKey(x => x.Id);

            e.Property(x => x.Name).IsRequired().HasMaxLength(128);
            e.Property(x => x.Role).IsRequired().HasMaxLength(32);

            // Postgres defaults; on SQL Server use GETUTCDATE()
            e.Property(x => x.JoinedAt).HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'");
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'");

            e.HasIndex(x => x.RoomId);
            e.HasIndex(x => new { x.RoomId, x.UserId }).IsUnique();

            e.HasOne(x => x.Room)
                .WithMany(r => r.Members)
                .HasForeignKey(x => x.RoomId)
                .OnDelete(DeleteBehavior.Restrict);
        });

// --------------- VoiceRoomConnection ---------------
        modelBuilder.Entity<VoiceRoomConnection>(e =>
        {
            e.HasKey(x => x.Id);

            e.HasIndex(x => new { x.RoomId, x.DisconnectedAt });
            e.HasIndex(x => new { x.RoomId, x.UserId });

            // If you added HubConnectionId, keep these lines; otherwise remove them.
            e.Property(x => x.HubConnectionId).IsRequired();
            e.HasIndex(x => x.HubConnectionId).IsUnique();

            e.Property(x => x.ConnectedAt).HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'");

            e.HasOne(x => x.Room)
                .WithMany(r => r.Connections)
                .HasForeignKey(x => x.RoomId)
                .OnDelete(DeleteBehavior.Restrict);
        });

// -------------------- User --------------------
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(x => x.Id);

            e.Property(x => x.Username).IsRequired().HasMaxLength(128);
            e.HasIndex(x => x.Username).IsUnique();

            e.Property(x => x.IsMuted).HasDefaultValue(false);
            e.Property(x => x.AnalyticsOptIn).HasDefaultValue(false);

            // Optional FK to current room; null when not in a room.
            e.HasOne(x => x.VoiceRoom)
                .WithMany()
                .HasForeignKey(x => x.VoiceRoomId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}