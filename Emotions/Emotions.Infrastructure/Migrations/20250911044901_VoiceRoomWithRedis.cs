using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Emotions.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class VoiceRoomWithRedis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VoiceRoomMembers_VoiceRooms_RoomId",
                table: "VoiceRoomMembers");

            migrationBuilder.DropIndex(
                name: "IX_VoiceRooms_IsLive",
                table: "VoiceRooms");

            migrationBuilder.DropPrimaryKey(
                name: "PK_VoiceRoomMembers",
                table: "VoiceRoomMembers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_VoiceRoomConnections",
                table: "VoiceRoomConnections");

            migrationBuilder.DropColumn(
                name: "ServerMuted",
                table: "VoiceRoomMembers");

            migrationBuilder.RenameColumn(
                name: "IsLive",
                table: "VoiceRooms",
                newName: "IsDeleted");

            migrationBuilder.RenameColumn(
                name: "SelfMuted",
                table: "VoiceRoomConnections",
                newName: "IsVideoOn");

            migrationBuilder.RenameColumn(
                name: "ConnectionId",
                table: "VoiceRoomConnections",
                newName: "Username");

            migrationBuilder.AlterColumn<string>(
                name: "Prompt",
                table: "VoiceRooms",
                type: "character varying(240)",
                maxLength: 240,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<int>(
                name: "MaxParticipants",
                table: "VoiceRooms",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "VoiceRooms",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Language",
                table: "VoiceRooms",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastActiveAt",
                table: "VoiceRooms",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "VoiceRooms",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "SpeakPolicy",
                table: "VoiceRooms",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "VoiceRooms",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Theme",
                table: "VoiceRooms",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ThumbnailUrl",
                table: "VoiceRooms",
                type: "character varying(2083)",
                maxLength: 2083,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "VoiceRooms",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Topic",
                table: "VoiceRooms",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "VoiceRooms",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "VoiceRoomMembers",
                type: "timestamp with time zone",
                nullable: true,
                defaultValueSql: "NOW() AT TIME ZONE 'UTC'",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "NOW() AT TIME ZONE 'UTC'");

            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "VoiceRoomMembers",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "VoiceRoomMembers",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "VoiceRoomConnections",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DisconnectedAt",
                table: "VoiceRoomConnections",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HubConnectionId",
                table: "VoiceRoomConnections",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsMuted",
                table: "VoiceRoomConnections",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddPrimaryKey(
                name: "PK_VoiceRoomMembers",
                table: "VoiceRoomMembers",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_VoiceRoomConnections",
                table: "VoiceRoomConnections",
                column: "Id");

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

            migrationBuilder.CreateIndex(
                name: "IX_VoiceRoomMembers_RoomId_UserId",
                table: "VoiceRoomMembers",
                columns: new[] { "RoomId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VoiceRoomConnections_HubConnectionId",
                table: "VoiceRoomConnections",
                column: "HubConnectionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VoiceRoomConnections_RoomId_DisconnectedAt",
                table: "VoiceRoomConnections",
                columns: new[] { "RoomId", "DisconnectedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_VoiceRoomConnections_VoiceRooms_RoomId",
                table: "VoiceRoomConnections",
                column: "RoomId",
                principalTable: "VoiceRooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VoiceRoomMembers_VoiceRooms_RoomId",
                table: "VoiceRoomMembers",
                column: "RoomId",
                principalTable: "VoiceRooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VoiceRoomConnections_VoiceRooms_RoomId",
                table: "VoiceRoomConnections");

            migrationBuilder.DropForeignKey(
                name: "FK_VoiceRoomMembers_VoiceRooms_RoomId",
                table: "VoiceRoomMembers");

            migrationBuilder.DropIndex(
                name: "IX_VoiceRooms_LastActiveAt",
                table: "VoiceRooms");

            migrationBuilder.DropIndex(
                name: "IX_VoiceRooms_Status",
                table: "VoiceRooms");

            migrationBuilder.DropIndex(
                name: "IX_VoiceRooms_Status_LastActiveAt",
                table: "VoiceRooms");

            migrationBuilder.DropPrimaryKey(
                name: "PK_VoiceRoomMembers",
                table: "VoiceRoomMembers");

            migrationBuilder.DropIndex(
                name: "IX_VoiceRoomMembers_RoomId_UserId",
                table: "VoiceRoomMembers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_VoiceRoomConnections",
                table: "VoiceRoomConnections");

            migrationBuilder.DropIndex(
                name: "IX_VoiceRoomConnections_HubConnectionId",
                table: "VoiceRoomConnections");

            migrationBuilder.DropIndex(
                name: "IX_VoiceRoomConnections_RoomId_DisconnectedAt",
                table: "VoiceRoomConnections");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "VoiceRooms");

            migrationBuilder.DropColumn(
                name: "Language",
                table: "VoiceRooms");

            migrationBuilder.DropColumn(
                name: "LastActiveAt",
                table: "VoiceRooms");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "VoiceRooms");

            migrationBuilder.DropColumn(
                name: "SpeakPolicy",
                table: "VoiceRooms");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "VoiceRooms");

            migrationBuilder.DropColumn(
                name: "Theme",
                table: "VoiceRooms");

            migrationBuilder.DropColumn(
                name: "ThumbnailUrl",
                table: "VoiceRooms");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "VoiceRooms");

            migrationBuilder.DropColumn(
                name: "Topic",
                table: "VoiceRooms");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "VoiceRooms");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "VoiceRoomMembers");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "VoiceRoomMembers");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "VoiceRoomConnections");

            migrationBuilder.DropColumn(
                name: "DisconnectedAt",
                table: "VoiceRoomConnections");

            migrationBuilder.DropColumn(
                name: "HubConnectionId",
                table: "VoiceRoomConnections");

            migrationBuilder.DropColumn(
                name: "IsMuted",
                table: "VoiceRoomConnections");

            migrationBuilder.RenameColumn(
                name: "IsDeleted",
                table: "VoiceRooms",
                newName: "IsLive");

            migrationBuilder.RenameColumn(
                name: "Username",
                table: "VoiceRoomConnections",
                newName: "ConnectionId");

            migrationBuilder.RenameColumn(
                name: "IsVideoOn",
                table: "VoiceRoomConnections",
                newName: "SelfMuted");

            migrationBuilder.AlterColumn<string>(
                name: "Prompt",
                table: "VoiceRooms",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(240)",
                oldMaxLength: 240,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "MaxParticipants",
                table: "VoiceRooms",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "VoiceRoomMembers",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW() AT TIME ZONE 'UTC'",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldDefaultValueSql: "NOW() AT TIME ZONE 'UTC'");

            migrationBuilder.AddColumn<bool>(
                name: "ServerMuted",
                table: "VoiceRoomMembers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddPrimaryKey(
                name: "PK_VoiceRoomMembers",
                table: "VoiceRoomMembers",
                columns: new[] { "RoomId", "UserId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_VoiceRoomConnections",
                table: "VoiceRoomConnections",
                column: "ConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_VoiceRooms_IsLive",
                table: "VoiceRooms",
                column: "IsLive");

            migrationBuilder.AddForeignKey(
                name: "FK_VoiceRoomMembers_VoiceRooms_RoomId",
                table: "VoiceRoomMembers",
                column: "RoomId",
                principalTable: "VoiceRooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
