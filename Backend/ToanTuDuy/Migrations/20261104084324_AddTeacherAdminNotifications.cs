using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToanTuDuy.Migrations
{
    /// <inheritdoc />
    public partial class AddTeacherAdminNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NOTIFICATION_PARENT_PROFILE_parent_id",
                table: "NOTIFICATION");

            migrationBuilder.DropForeignKey(
                name: "FK_NOTIFICATION_SETTING_PARENT_PROFILE_parent_id",
                table: "NOTIFICATION_SETTING");

            migrationBuilder.AlterColumn<int>(
                name: "parent_id",
                table: "NOTIFICATION_SETTING",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "admin_id",
                table: "NOTIFICATION_SETTING",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "teacher_id",
                table: "NOTIFICATION_SETTING",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "parent_id",
                table: "NOTIFICATION",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "admin_id",
                table: "NOTIFICATION",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "teacher_id",
                table: "NOTIFICATION",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_NOTIFICATION_SETTING_admin_id",
                table: "NOTIFICATION_SETTING",
                column: "admin_id");

            migrationBuilder.CreateIndex(
                name: "IX_NOTIFICATION_SETTING_teacher_id",
                table: "NOTIFICATION_SETTING",
                column: "teacher_id");

            migrationBuilder.AddCheckConstraint(
                name: "CK_NOTIFICATION_SETTING_RECIPIENT",
                table: "NOTIFICATION_SETTING",
                sql: "(CASE WHEN [parent_id] IS NOT NULL THEN 1 ELSE 0 END + CASE WHEN [teacher_id] IS NOT NULL THEN 1 ELSE 0 END + CASE WHEN [admin_id] IS NOT NULL THEN 1 ELSE 0 END) = 1");

            migrationBuilder.CreateIndex(
                name: "IX_NOTIFICATION_admin_id",
                table: "NOTIFICATION",
                column: "admin_id");

            migrationBuilder.CreateIndex(
                name: "IX_NOTIFICATION_teacher_id",
                table: "NOTIFICATION",
                column: "teacher_id");

            migrationBuilder.AddCheckConstraint(
                name: "CK_NOTIFICATION_RECIPIENT",
                table: "NOTIFICATION",
                sql: "(CASE WHEN [parent_id] IS NOT NULL THEN 1 ELSE 0 END + CASE WHEN [teacher_id] IS NOT NULL THEN 1 ELSE 0 END + CASE WHEN [admin_id] IS NOT NULL THEN 1 ELSE 0 END) = 1");

            migrationBuilder.AddForeignKey(
                name: "FK_NOTIFICATION_ADMIN_PROFILE_admin_id",
                table: "NOTIFICATION",
                column: "admin_id",
                principalTable: "ADMIN_PROFILE",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_NOTIFICATION_PARENT_PROFILE_parent_id",
                table: "NOTIFICATION",
                column: "parent_id",
                principalTable: "PARENT_PROFILE",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_NOTIFICATION_TEACHER_PROFILE_teacher_id",
                table: "NOTIFICATION",
                column: "teacher_id",
                principalTable: "TEACHER_PROFILE",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_NOTIFICATION_SETTING_ADMIN_PROFILE_admin_id",
                table: "NOTIFICATION_SETTING",
                column: "admin_id",
                principalTable: "ADMIN_PROFILE",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_NOTIFICATION_SETTING_PARENT_PROFILE_parent_id",
                table: "NOTIFICATION_SETTING",
                column: "parent_id",
                principalTable: "PARENT_PROFILE",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_NOTIFICATION_SETTING_TEACHER_PROFILE_teacher_id",
                table: "NOTIFICATION_SETTING",
                column: "teacher_id",
                principalTable: "TEACHER_PROFILE",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NOTIFICATION_ADMIN_PROFILE_admin_id",
                table: "NOTIFICATION");

            migrationBuilder.DropForeignKey(
                name: "FK_NOTIFICATION_PARENT_PROFILE_parent_id",
                table: "NOTIFICATION");

            migrationBuilder.DropForeignKey(
                name: "FK_NOTIFICATION_TEACHER_PROFILE_teacher_id",
                table: "NOTIFICATION");

            migrationBuilder.DropForeignKey(
                name: "FK_NOTIFICATION_SETTING_ADMIN_PROFILE_admin_id",
                table: "NOTIFICATION_SETTING");

            migrationBuilder.DropForeignKey(
                name: "FK_NOTIFICATION_SETTING_PARENT_PROFILE_parent_id",
                table: "NOTIFICATION_SETTING");

            migrationBuilder.DropForeignKey(
                name: "FK_NOTIFICATION_SETTING_TEACHER_PROFILE_teacher_id",
                table: "NOTIFICATION_SETTING");

            migrationBuilder.DropIndex(
                name: "IX_NOTIFICATION_SETTING_admin_id",
                table: "NOTIFICATION_SETTING");

            migrationBuilder.DropIndex(
                name: "IX_NOTIFICATION_SETTING_teacher_id",
                table: "NOTIFICATION_SETTING");

            migrationBuilder.DropCheckConstraint(
                name: "CK_NOTIFICATION_SETTING_RECIPIENT",
                table: "NOTIFICATION_SETTING");

            migrationBuilder.DropIndex(
                name: "IX_NOTIFICATION_admin_id",
                table: "NOTIFICATION");

            migrationBuilder.DropIndex(
                name: "IX_NOTIFICATION_teacher_id",
                table: "NOTIFICATION");

            migrationBuilder.DropCheckConstraint(
                name: "CK_NOTIFICATION_RECIPIENT",
                table: "NOTIFICATION");

            migrationBuilder.DropColumn(
                name: "admin_id",
                table: "NOTIFICATION_SETTING");

            migrationBuilder.DropColumn(
                name: "teacher_id",
                table: "NOTIFICATION_SETTING");

            migrationBuilder.DropColumn(
                name: "admin_id",
                table: "NOTIFICATION");

            migrationBuilder.DropColumn(
                name: "teacher_id",
                table: "NOTIFICATION");

            migrationBuilder.AlterColumn<int>(
                name: "parent_id",
                table: "NOTIFICATION_SETTING",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "parent_id",
                table: "NOTIFICATION",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_NOTIFICATION_PARENT_PROFILE_parent_id",
                table: "NOTIFICATION",
                column: "parent_id",
                principalTable: "PARENT_PROFILE",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_NOTIFICATION_SETTING_PARENT_PROFILE_parent_id",
                table: "NOTIFICATION_SETTING",
                column: "parent_id",
                principalTable: "PARENT_PROFILE",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
