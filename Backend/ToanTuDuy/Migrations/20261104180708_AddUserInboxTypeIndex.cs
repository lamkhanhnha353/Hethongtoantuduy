using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToanTuDuy.Migrations
{
    /// <inheritdoc />
    public partial class AddUserInboxTypeIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_USER_INBOX_account_id_type_created_at",
                table: "USER_INBOX",
                columns: new[] { "account_id", "type", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_USER_INBOX_account_id_type_created_at",
                table: "USER_INBOX");
        }
    }
}
