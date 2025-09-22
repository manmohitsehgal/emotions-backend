using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Emotions.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Interests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Slug = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Interests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JournalEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: true),
                    TextEncrypted = table.Column<string>(type: "text", nullable: false),
                    IsPrivate = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JournalEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SafetyEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    TriggeredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Region = table.Column<string>(type: "text", nullable: true),
                    Handled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SafetyEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SessionHosts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IsAi = table.Column<bool>(type: "boolean", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DisplayName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Credentials = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Bio = table.Column<string>(type: "text", nullable: true),
                    AiPersonaKey = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionHosts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SessionTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    SpeakPolicy = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    DefaultCapacity = table.Column<int>(type: "integer", nullable: false),
                    AllowListeners = table.Column<bool>(type: "boolean", nullable: false),
                    RecurrenceRule = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TherapyActionItem",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Details = table.Column<string>(type: "text", nullable: true),
                    DueAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TherapyActionItem", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TherapyConversations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Mode = table.Column<int>(type: "integer", nullable: false),
                    IncludeJournal = table.Column<bool>(type: "boolean", nullable: false),
                    IsPremiumSnapshot = table.Column<bool>(type: "boolean", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SafetyLevel = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TherapyConversations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VoiceRooms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Prompt = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    Topic = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Description = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    ThumbnailUrl = table.Column<string>(type: "character varying(2083)", maxLength: 2083, nullable: true),
                    Theme = table.Column<int>(type: "integer", nullable: false),
                    Language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    MaxParticipants = table.Column<int>(type: "integer", nullable: true),
                    SpeakPolicy = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastActiveAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoiceRooms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TherapyMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorType = table.Column<int>(type: "integer", nullable: false),
                    TextEncrypted = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Model = table.Column<string>(type: "text", nullable: true),
                    TokensIn = table.Column<int>(type: "integer", nullable: true),
                    TokensOut = table.Column<int>(type: "integer", nullable: true),
                    IsPrivate = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TherapyMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TherapyMessages_TherapyConversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "TherapyConversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoomMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    JoinedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW() AT TIME ZONE 'UTC'"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, defaultValueSql: "NOW() AT TIME ZONE 'UTC'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoomMembers_VoiceRooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "VoiceRooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupportSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    SpeakPolicy = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    StartAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: false, defaultValue: 15),
                    AllowListeners = table.Column<bool>(type: "boolean", nullable: false),
                    Language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: true),
                    HostId = table.Column<Guid>(type: "uuid", nullable: false),
                    VoiceRoomId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupportSessions_SessionHosts_HostId",
                        column: x => x.HostId,
                        principalTable: "SessionHosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupportSessions_SessionTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "SessionTemplates",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SupportSessions_VoiceRooms_VoiceRoomId",
                        column: x => x.VoiceRoomId,
                        principalTable: "VoiceRooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalId = table.Column<string>(type: "text", nullable: false),
                    Username = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsMuted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    VoiceRoomId = table.Column<Guid>(type: "uuid", nullable: true),
                    HasCompletedOnboarding = table.Column<bool>(type: "boolean", nullable: false),
                    OnboardingTemplate = table.Column<string>(type: "text", nullable: true),
                    AnalyticsOptIn = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    OnboardedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW() AT TIME ZONE 'UTC'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_VoiceRooms_VoiceRoomId",
                        column: x => x.VoiceRoomId,
                        principalTable: "VoiceRooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "VoiceRoomConnections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Username = table.Column<string>(type: "text", nullable: false),
                    HubConnectionId = table.Column<string>(type: "text", nullable: false),
                    ConnectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW() AT TIME ZONE 'UTC'"),
                    DisconnectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsMuted = table.Column<bool>(type: "boolean", nullable: false),
                    IsVideoOn = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoiceRoomConnections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VoiceRoomConnections_VoiceRooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "VoiceRooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SessionBookings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CheckedInAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsPremium = table.Column<bool>(type: "boolean", nullable: false),
                    PriorityScore = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionBookings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionBookings_SupportSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "SupportSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserInterests",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    InterestId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserInterests", x => new { x.UserId, x.InterestId });
                    table.ForeignKey(
                        name: "FK_UserInterests_Interests_InterestId",
                        column: x => x.InterestId,
                        principalTable: "Interests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserInterests_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Interests",
                columns: new[] { "Id", "Name", "Slug" },
                values: new object[,]
                {
                    { 1, "Stress", "stress" },
                    { 2, "Sleep", "sleep" },
                    { 3, "Relationships", "relationships" },
                    { 4, "Focus", "focus" },
                    { 5, "Self-esteem", "self-esteem" },
                    { 6, "Anxiety", "anxiety" },
                    { 7, "Depression", "depression" },
                    { 8, "Productivity", "productivity" },
                    { 9, "Habits", "habits" },
                    { 10, "Anger", "anger" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Interests_Slug",
                table: "Interests",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_UserId_CreatedAt",
                table: "JournalEntries",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RoomMembers_RoomId",
                table: "RoomMembers",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomMembers_RoomId_UserId",
                table: "RoomMembers",
                columns: new[] { "RoomId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SafetyEvents_ConversationId_TriggeredAt",
                table: "SafetyEvents",
                columns: new[] { "ConversationId", "TriggeredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SessionBookings_SessionId_UserId",
                table: "SessionBookings",
                columns: new[] { "SessionId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SessionHosts_UserId",
                table: "SessionHosts",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupportSessions_HostId",
                table: "SupportSessions",
                column: "HostId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportSessions_TemplateId",
                table: "SupportSessions",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportSessions_VoiceRoomId",
                table: "SupportSessions",
                column: "VoiceRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_TherapyMessages_ConversationId_CreatedAt",
                table: "TherapyMessages",
                columns: new[] { "ConversationId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UserInterests_InterestId",
                table: "UserInterests",
                column: "InterestId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_ExternalId",
                table: "Users",
                column: "ExternalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_VoiceRoomId",
                table: "Users",
                column: "VoiceRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_VoiceRoomConnections_HubConnectionId",
                table: "VoiceRoomConnections",
                column: "HubConnectionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VoiceRoomConnections_RoomId_DisconnectedAt",
                table: "VoiceRoomConnections",
                columns: new[] { "RoomId", "DisconnectedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_VoiceRoomConnections_RoomId_UserId",
                table: "VoiceRoomConnections",
                columns: new[] { "RoomId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_VoiceRooms_LastActiveAt",
                table: "VoiceRooms",
                column: "LastActiveAt");

            migrationBuilder.CreateIndex(
                name: "IX_VoiceRooms_Status",
                table: "VoiceRooms",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_VoiceRooms_Status_LastActiveAt",
                table: "VoiceRooms",
                columns: new[] { "Status", "LastActiveAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JournalEntries");

            migrationBuilder.DropTable(
                name: "RoomMembers");

            migrationBuilder.DropTable(
                name: "SafetyEvents");

            migrationBuilder.DropTable(
                name: "SessionBookings");

            migrationBuilder.DropTable(
                name: "TherapyActionItem");

            migrationBuilder.DropTable(
                name: "TherapyMessages");

            migrationBuilder.DropTable(
                name: "UserInterests");

            migrationBuilder.DropTable(
                name: "VoiceRoomConnections");

            migrationBuilder.DropTable(
                name: "SupportSessions");

            migrationBuilder.DropTable(
                name: "TherapyConversations");

            migrationBuilder.DropTable(
                name: "Interests");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "SessionHosts");

            migrationBuilder.DropTable(
                name: "SessionTemplates");

            migrationBuilder.DropTable(
                name: "VoiceRooms");
        }
    }
}
