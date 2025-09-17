using Emotions.Domain.Entities;
using Emotions.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Emotions.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    // --- DbSets ---
    public DbSet<Room> VoiceRooms { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    public DbSet<RoomMember> RoomMembers => Set<RoomMember>();
    public DbSet<RoomConnection> VoiceRoomConnections => Set<RoomConnection>();
    public DbSet<Interest> Interests => Set<Interest>();
    public DbSet<UserInterest> UserInterests => Set<UserInterest>();
    public DbSet<SupportSession> SupportSessions => Set<SupportSession>();
    public DbSet<SessionTemplate> SessionTemplates => Set<SessionTemplate>();
    public DbSet<SessionHost> SessionHosts => Set<SessionHost>();
    public DbSet<SessionBooking> SessionBookings => Set<SessionBooking>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ---------------- VoiceRoom ----------------
        modelBuilder.Entity<Room>(e =>
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

        // --------------- RoomMember (surrogate Id PK) ---------------
        modelBuilder.Entity<RoomMember>(e =>
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
        modelBuilder.Entity<RoomConnection>(e =>
        {
            e.HasKey(x => x.Id);

            e.HasIndex(x => new { x.RoomId, x.DisconnectedAt });
            e.HasIndex(x => new { x.RoomId, x.UserId });

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

            // Helpful caps for free-text fields
            e.Property(x => x.Name).HasMaxLength(128);
            e.Property(x => x.Email).HasMaxLength(256);

            e.Property(x => x.IsMuted).HasDefaultValue(false);
            e.Property(x => x.AnalyticsOptIn).HasDefaultValue(false);

            // DB-level default for created timestamp (Postgres)
            e.Property(x => x.CreatedAt).HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'");

            // Stable OIDC subject; must be unique
            e.Property(x => x.ExternalId).IsRequired();
            e.HasIndex(x => x.ExternalId).IsUnique();

            // Optional FK to current room; null when not in a room.
            e.HasOne(x => x.VoiceRoom)
                .WithMany()
                .HasForeignKey(x => x.VoiceRoomId)
                .OnDelete(DeleteBehavior.SetNull);

            // Explicit join (UserInterest)
            e.HasMany(u => u.UserInterests)
                .WithOne(ui => ui.User)
                .HasForeignKey(ui => ui.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // -------------------- UserInterest (explicit join) --------------------
        modelBuilder.Entity<UserInterest>(b =>
        {
            // Composite primary key prevents duplicates
            b.HasKey(ui => new { ui.UserId, ui.InterestId });

            // FKs defined by navs; explicit indexes help query patterns
            b.HasOne(ui => ui.User)
                .WithMany(u => u.UserInterests)
                .HasForeignKey(ui => ui.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(ui => ui.Interest)
                .WithMany(i => i.UserInterests)
                .HasForeignKey(ui => ui.InterestId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(ui => ui.InterestId);
        });

        // -------------------- Interest --------------------
        modelBuilder.Entity<Interest>(b =>
        {
            b.HasKey(i => i.Id);
            b.Property(i => i.Name).HasMaxLength(128).IsRequired();
            b.Property(i => i.Slug).HasMaxLength(64).IsRequired();

            // Slug must be unique (stable key exchanged with the app)
            b.HasIndex(i => i.Slug).IsUnique();

            b.HasMany(i => i.UserInterests)
                .WithOne(ui => ui.Interest)
                .HasForeignKey(ui => ui.InterestId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // -------------------- Seed a small catalog (optional) --------------------
        modelBuilder.Entity<Interest>().HasData(new[]
        {
            new Interest { Id = 1, Name = "Stress", Slug = "stress" },
            new Interest { Id = 2, Name = "Sleep", Slug = "sleep" },
            new Interest { Id = 3, Name = "Relationships", Slug = "relationships" },
            new Interest { Id = 4, Name = "Focus", Slug = "focus" },
            new Interest { Id = 5, Name = "Self-esteem", Slug = "self-esteem" },
            new Interest { Id = 6, Name = "Anxiety", Slug = "anxiety" },
            new Interest { Id = 7, Name = "Depression", Slug = "depression" },
            new Interest { Id = 8, Name = "Productivity", Slug = "productivity" },
            new Interest { Id = 9, Name = "Habits", Slug = "habits" },
            new Interest { Id = 10, Name = "Anger", Slug = "anger" },
        });

        modelBuilder.Entity<SupportSession>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).IsRequired().HasMaxLength(140);
            e.Property(x => x.Capacity).HasDefaultValue(15);
            e.Property(x => x.Status).HasDefaultValue(SessionStatus.Draft);
            e.Property(x => x.SpeakPolicy).HasDefaultValue(SpeakPolicy.RoundRobin);

            e.HasOne(x => x.Host).WithMany().HasForeignKey(x => x.HostId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Room).WithMany().HasForeignKey(x => x.VoiceRoomId)
                .OnDelete(DeleteBehavior.SetNull);
            e.Property(x => x.Language).IsRequired().HasMaxLength(10); // if added
        });

        modelBuilder.Entity<SessionTemplate>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).IsRequired().HasMaxLength(140);
            e.Property(x => x.SpeakPolicy).HasDefaultValue(SpeakPolicy.RoundRobin);
        });

        modelBuilder.Entity<SessionHost>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.DisplayName).HasMaxLength(120);
            e.Property(x => x.Credentials).HasMaxLength(120);
        });

        modelBuilder.Entity<SessionBooking>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.SessionId, x.UserId }).IsUnique();
            e.Property(x => x.Status).HasDefaultValue(BookingStatus.Pending);

            e.HasOne(x => x.Session).WithMany()
                .HasForeignKey(x => x.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}