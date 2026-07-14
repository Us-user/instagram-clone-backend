using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStoryCloseFriendsAndReplies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Audience",
                table: "Stories",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SharedPostId",
                table: "Stories",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CloseFriends",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    FriendUserId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CloseFriends", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CloseFriends_AspNetUsers_FriendUserId",
                        column: x => x.FriendUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CloseFriends_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StoryReplies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StoryId = table.Column<int>(type: "integer", nullable: false),
                    FromUserId = table.Column<string>(type: "text", nullable: false),
                    MessageId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoryReplies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoryReplies_AspNetUsers_FromUserId",
                        column: x => x.FromUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StoryReplies_Messages_MessageId",
                        column: x => x.MessageId,
                        principalTable: "Messages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StoryReplies_Stories_StoryId",
                        column: x => x.StoryId,
                        principalTable: "Stories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Stories_SharedPostId",
                table: "Stories",
                column: "SharedPostId");

            migrationBuilder.CreateIndex(
                name: "IX_CloseFriends_FriendUserId",
                table: "CloseFriends",
                column: "FriendUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CloseFriends_UserId_FriendUserId",
                table: "CloseFriends",
                columns: new[] { "UserId", "FriendUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoryReplies_FromUserId",
                table: "StoryReplies",
                column: "FromUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StoryReplies_MessageId",
                table: "StoryReplies",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_StoryReplies_StoryId",
                table: "StoryReplies",
                column: "StoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Stories_Posts_SharedPostId",
                table: "Stories",
                column: "SharedPostId",
                principalTable: "Posts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Stories_Posts_SharedPostId",
                table: "Stories");

            migrationBuilder.DropTable(
                name: "CloseFriends");

            migrationBuilder.DropTable(
                name: "StoryReplies");

            migrationBuilder.DropIndex(
                name: "IX_Stories_SharedPostId",
                table: "Stories");

            migrationBuilder.DropColumn(
                name: "Audience",
                table: "Stories");

            migrationBuilder.DropColumn(
                name: "SharedPostId",
                table: "Stories");
        }
    }
}
