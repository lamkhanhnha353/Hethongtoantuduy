using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToanTuDuy.Migrations
{
    /// <inheritdoc />
    public partial class AddChatRealtimeFinal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CONVERSATION",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    parent_id = table.Column<int>(type: "int", nullable: false),
                    teacher_id = table.Column<int>(type: "int", nullable: true),
                    chat_type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    last_message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    last_message_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CONVERSATION", x => x.id);
                    table.ForeignKey(
                        name: "FK_CONVERSATION_PARENT_PROFILE_parent_id",
                        column: x => x.parent_id,
                        principalTable: "PARENT_PROFILE",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CONVERSATION_TEACHER_PROFILE_teacher_id",
                        column: x => x.teacher_id,
                        principalTable: "TEACHER_PROFILE",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MESSAGE",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    conversation_id = table.Column<int>(type: "int", nullable: false),
                    sender_type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    sender_id = table.Column<int>(type: "int", nullable: true),
                    message_type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    content = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    attachment_url = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    is_read = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MESSAGE", x => x.id);
                    table.ForeignKey(
                        name: "FK_MESSAGE_CONVERSATION_conversation_id",
                        column: x => x.conversation_id,
                        principalTable: "CONVERSATION",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CONVERSATION_parent_id",
                table: "CONVERSATION",
                column: "parent_id",
                unique: true,
                filter: "[chat_type] = 'AI'");

            migrationBuilder.CreateIndex(
                name: "IX_CONVERSATION_parent_id_last_message_at",
                table: "CONVERSATION",
                columns: new[] { "parent_id", "last_message_at" });

            migrationBuilder.CreateIndex(
                name: "IX_CONVERSATION_parent_id_teacher_id",
                table: "CONVERSATION",
                columns: new[] { "parent_id", "teacher_id" },
                unique: true,
                filter: "[chat_type] = 'HUMAN' AND [teacher_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CONVERSATION_teacher_id_last_message_at",
                table: "CONVERSATION",
                columns: new[] { "teacher_id", "last_message_at" });

            migrationBuilder.CreateIndex(
                name: "IX_MESSAGE_conversation_id_created_at",
                table: "MESSAGE",
                columns: new[] { "conversation_id", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MESSAGE");

            migrationBuilder.DropTable(
                name: "CONVERSATION");
        }
    }
}
