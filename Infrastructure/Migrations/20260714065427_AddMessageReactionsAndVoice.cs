using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMessageReactionsAndVoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Duration",
                table: "Messages",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsForwarded",
                table: "Messages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MessageType",
                table: "Messages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ReplyToMessageId",
                table: "Messages",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Waveform",
                table: "Messages",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MessageReactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MessageId = table.Column<int>(type: "integer", nullable: false),
                    MessageContext = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Emoji = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageReactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MessageReactions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ReplyToMessageId",
                table: "Messages",
                column: "ReplyToMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_MessageReactions_MessageId_MessageContext_UserId",
                table: "MessageReactions",
                columns: new[] { "MessageId", "MessageContext", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MessageReactions_UserId",
                table: "MessageReactions",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Messages_ReplyToMessageId",
                table: "Messages",
                column: "ReplyToMessageId",
                principalTable: "Messages",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Messages_ReplyToMessageId",
                table: "Messages");

            migrationBuilder.DropTable(
                name: "MessageReactions");

            migrationBuilder.DropIndex(
                name: "IX_Messages_ReplyToMessageId",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "Duration",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "IsForwarded",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "MessageType",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "ReplyToMessageId",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "Waveform",
                table: "Messages");
        }
    }
}
