using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToanTuDuy.Migrations
{
    /// <inheritdoc />
    public partial class AdminProfileTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "pending_email",
                table: "ACCOUNT",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ADMIN_PROFILE",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    account_id = table.Column<int>(type: "int", nullable: false),
                    full_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    avatar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ADMIN_PROFILE", x => x.id);
                    table.ForeignKey(
                        name: "FK_ADMIN_PROFILE_ACCOUNT_account_id",
                        column: x => x.account_id,
                        principalTable: "ACCOUNT",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ADMIN_PROFILE_account_id",
                table: "ADMIN_PROFILE",
                column: "account_id",
                unique: true);

            migrationBuilder.Sql(
                """
                INSERT INTO [dbo].[ADMIN_PROFILE] ([account_id], [full_name], [created_at])
                SELECT [id], COALESCE(NULLIF([UserName], N''), NULLIF([phone], N''), N'Admin'), [created_at]
                FROM [dbo].[ACCOUNT] AS account
                WHERE account.[role] = N'Admin'
                    AND NOT EXISTS (
                        SELECT 1
                        FROM [dbo].[ADMIN_PROFILE] AS profile
                        WHERE profile.[account_id] = account.[id]);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ADMIN_PROFILE");

            migrationBuilder.DropColumn(
                name: "pending_email",
                table: "ACCOUNT");
        }
    }
}
