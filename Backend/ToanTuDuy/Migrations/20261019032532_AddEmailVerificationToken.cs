using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToanTuDuy.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailVerificationToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EMAIL_VERIFICATION_TOKEN",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    account_id = table.Column<int>(type: "int", nullable: false),
                    token_hash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    email_at_issue = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    expires_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    used_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EMAIL_VERIFICATION_TOKEN", x => x.id);
                    table.ForeignKey(
                        name: "FK_EMAIL_VERIFICATION_TOKEN_ACCOUNT_account_id",
                        column: x => x.account_id,
                        principalTable: "ACCOUNT",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EMAIL_VERIFICATION_TOKEN_account_id",
                table: "EMAIL_VERIFICATION_TOKEN",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "IX_EMAIL_VERIFICATION_TOKEN_token_hash",
                table: "EMAIL_VERIFICATION_TOKEN",
                column: "token_hash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EMAIL_VERIFICATION_TOKEN");
        }
    }
}
