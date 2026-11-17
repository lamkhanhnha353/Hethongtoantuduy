using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToanTuDuy.Migrations
{
    /// <inheritdoc />
    public partial class RestructureNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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
                name: "IX_NOTIFICATION_SETTING_parent_id",
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
                name: "IX_NOTIFICATION_parent_id",
                table: "NOTIFICATION");

            migrationBuilder.DropIndex(
                name: "IX_NOTIFICATION_teacher_id",
                table: "NOTIFICATION");

            migrationBuilder.DropCheckConstraint(
                name: "CK_NOTIFICATION_RECIPIENT",
                table: "NOTIFICATION");

            // NOTIFICATION: đổi khóa chính sang bigint và chuẩn hóa nội dung.
            migrationBuilder.Sql("UPDATE [NOTIFICATION] SET [type] = '' WHERE [type] IS NULL;");
            migrationBuilder.Sql("UPDATE [NOTIFICATION] SET [title] = '' WHERE [title] IS NULL;");

            migrationBuilder.DropPrimaryKey(
                name: "PK_NOTIFICATION",
                table: "NOTIFICATION");

            migrationBuilder.AlterColumn<long>(
                name: "id",
                table: "NOTIFICATION",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("SqlServer:Identity", "1, 1")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddPrimaryKey(
                name: "PK_NOTIFICATION",
                table: "NOTIFICATION",
                column: "id");

            migrationBuilder.AlterColumn<string>(
                name: "type",
                table: "NOTIFICATION",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "title",
                table: "NOTIFICATION",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "ref_id",
                table: "NOTIFICATION",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "body",
                table: "NOTIFICATION",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "OUTBOX",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    notification_id = table.Column<long>(type: "bigint", nullable: false),
                    audience_spec = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    attempts = table.Column<int>(type: "int", nullable: false),
                    available_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    processed_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    last_error = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OUTBOX", x => x.id);
                    table.ForeignKey(
                        name: "FK_OUTBOX_NOTIFICATION_notification_id",
                        column: x => x.notification_id,
                        principalTable: "NOTIFICATION",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "USER_INBOX",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    notification_id = table.Column<long>(type: "bigint", nullable: false),
                    account_id = table.Column<int>(type: "int", nullable: false),
                    type = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ref_id = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    is_read = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    read_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USER_INBOX", x => x.id);
                    table.ForeignKey(
                        name: "FK_USER_INBOX_ACCOUNT_account_id",
                        column: x => x.account_id,
                        principalTable: "ACCOUNT",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_USER_INBOX_NOTIFICATION_notification_id",
                        column: x => x.notification_id,
                        principalTable: "NOTIFICATION",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            // Chuyển dữ liệu thông báo cũ (mỗi dòng là một người nhận) sang hộp thư mới.
            migrationBuilder.Sql(@"
INSERT INTO [USER_INBOX] ([notification_id], [account_id], [type], [ref_id], [is_read], [created_at], [read_at])
SELECT n.[id], resolved.[account_id], ISNULL(n.[type], ''), n.[ref_id], n.[is_read], n.[created_at],
       CASE WHEN n.[is_read] = 1 THEN n.[created_at] ELSE NULL END
FROM [NOTIFICATION] n
CROSS APPLY (
    SELECT CASE
        WHEN n.[parent_id] IS NOT NULL THEN (SELECT [account_id] FROM [PARENT_PROFILE] WHERE [id] = n.[parent_id])
        WHEN n.[teacher_id] IS NOT NULL THEN (SELECT [account_id] FROM [TEACHER_PROFILE] WHERE [id] = n.[teacher_id])
        WHEN n.[admin_id] IS NOT NULL THEN (SELECT [account_id] FROM [ADMIN_PROFILE] WHERE [id] = n.[admin_id])
    END AS [account_id]
) resolved
WHERE resolved.[account_id] IS NOT NULL;");

            // NOTIFICATION_SETTING: chuyển từ hồ sơ (parent/teacher/admin) sang tài khoản.
            migrationBuilder.Sql("UPDATE [NOTIFICATION_SETTING] SET [type] = '' WHERE [type] IS NULL;");

            migrationBuilder.DropPrimaryKey(
                name: "PK_NOTIFICATION_SETTING",
                table: "NOTIFICATION_SETTING");

            migrationBuilder.AlterColumn<long>(
                name: "id",
                table: "NOTIFICATION_SETTING",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("SqlServer:Identity", "1, 1")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddPrimaryKey(
                name: "PK_NOTIFICATION_SETTING",
                table: "NOTIFICATION_SETTING",
                column: "id");

            migrationBuilder.AlterColumn<string>(
                name: "type",
                table: "NOTIFICATION_SETTING",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "account_id",
                table: "NOTIFICATION_SETTING",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "NOTIFICATION_SETTING",
                type: "datetime2",
                nullable: true);

            migrationBuilder.Sql(@"
UPDATE s
SET s.[account_id] = resolved.[account_id],
    s.[updated_at] = SYSUTCDATETIME()
FROM [NOTIFICATION_SETTING] s
CROSS APPLY (
    SELECT CASE
        WHEN s.[parent_id] IS NOT NULL THEN (SELECT [account_id] FROM [PARENT_PROFILE] WHERE [id] = s.[parent_id])
        WHEN s.[teacher_id] IS NOT NULL THEN (SELECT [account_id] FROM [TEACHER_PROFILE] WHERE [id] = s.[teacher_id])
        WHEN s.[admin_id] IS NOT NULL THEN (SELECT [account_id] FROM [ADMIN_PROFILE] WHERE [id] = s.[admin_id])
    END AS [account_id]
) resolved
WHERE resolved.[account_id] IS NOT NULL;");

            migrationBuilder.Sql("DELETE FROM [NOTIFICATION_SETTING] WHERE [account_id] IS NULL;");

            migrationBuilder.AlterColumn<int>(
                name: "account_id",
                table: "NOTIFICATION_SETTING",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "updated_at",
                table: "NOTIFICATION_SETTING",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "admin_id",
                table: "NOTIFICATION_SETTING");

            migrationBuilder.DropColumn(
                name: "parent_id",
                table: "NOTIFICATION_SETTING");

            migrationBuilder.DropColumn(
                name: "teacher_id",
                table: "NOTIFICATION_SETTING");

            migrationBuilder.AddColumn<int>(
                name: "created_by_account_id",
                table: "NOTIFICATION",
                type: "int",
                nullable: true);

            migrationBuilder.DropColumn(
                name: "admin_id",
                table: "NOTIFICATION");

            migrationBuilder.DropColumn(
                name: "is_read",
                table: "NOTIFICATION");

            migrationBuilder.DropColumn(
                name: "parent_id",
                table: "NOTIFICATION");

            migrationBuilder.DropColumn(
                name: "teacher_id",
                table: "NOTIFICATION");

            // Loại bỏ bản ghi trùng phát sinh từ dữ liệu cũ trước khi tạo unique index.
            migrationBuilder.Sql(@"
;WITH duplicates AS (
    SELECT [id], ROW_NUMBER() OVER (
        PARTITION BY [account_id], [type]
        ORDER BY CASE WHEN [enabled] = 1 THEN 0 ELSE 1 END, [id]) AS [row_number]
    FROM [NOTIFICATION_SETTING]
)
DELETE FROM duplicates WHERE [row_number] > 1;");

            migrationBuilder.Sql(@"
;WITH duplicates AS (
    SELECT [id], ROW_NUMBER() OVER (
        PARTITION BY [account_id], [type], [ref_id]
        ORDER BY [id]) AS [row_number]
    FROM [USER_INBOX]
    WHERE [ref_id] IS NOT NULL
)
DELETE FROM duplicates WHERE [row_number] > 1;");

            migrationBuilder.CreateIndex(
                name: "IX_NOTIFICATION_SETTING_account_id_type",
                table: "NOTIFICATION_SETTING",
                columns: new[] { "account_id", "type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NOTIFICATION_type_created_at",
                table: "NOTIFICATION",
                columns: new[] { "type", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_OUTBOX_notification_id",
                table: "OUTBOX",
                column: "notification_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OUTBOX_status_available_at",
                table: "OUTBOX",
                columns: new[] { "status", "available_at" });

            migrationBuilder.CreateIndex(
                name: "IX_USER_INBOX_account_id_is_read_created_at",
                table: "USER_INBOX",
                columns: new[] { "account_id", "is_read", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_USER_INBOX_account_id_type_ref_id",
                table: "USER_INBOX",
                columns: new[] { "account_id", "type", "ref_id" },
                unique: true,
                filter: "[ref_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_USER_INBOX_notification_id_account_id",
                table: "USER_INBOX",
                columns: new[] { "notification_id", "account_id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_NOTIFICATION_SETTING_ACCOUNT_account_id",
                table: "NOTIFICATION_SETTING",
                column: "account_id",
                principalTable: "ACCOUNT",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NOTIFICATION_SETTING_ACCOUNT_account_id",
                table: "NOTIFICATION_SETTING");

            migrationBuilder.DropTable(
                name: "OUTBOX");

            migrationBuilder.DropTable(
                name: "USER_INBOX");

            migrationBuilder.DropIndex(
                name: "IX_NOTIFICATION_SETTING_account_id_type",
                table: "NOTIFICATION_SETTING");

            migrationBuilder.DropIndex(
                name: "IX_NOTIFICATION_type_created_at",
                table: "NOTIFICATION");

            migrationBuilder.AlterColumn<long>(
                name: "id",
                table: "NOTIFICATION_SETTING",
                type: "int",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .Annotation("SqlServer:Identity", "1, 1")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<string>(
                name: "type",
                table: "NOTIFICATION_SETTING",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.DropColumn(
                name: "account_id",
                table: "NOTIFICATION_SETTING");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "NOTIFICATION_SETTING");

            migrationBuilder.AlterColumn<long>(
                name: "id",
                table: "NOTIFICATION",
                type: "int",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .Annotation("SqlServer:Identity", "1, 1")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<string>(
                name: "type",
                table: "NOTIFICATION",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "title",
                table: "NOTIFICATION",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "ref_id",
                table: "NOTIFICATION",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "body",
                table: "NOTIFICATION",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "created_by_account_id",
                table: "NOTIFICATION");

            migrationBuilder.AddColumn<int>(
                name: "admin_id",
                table: "NOTIFICATION",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_read",
                table: "NOTIFICATION",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "parent_id",
                table: "NOTIFICATION",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "teacher_id",
                table: "NOTIFICATION",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "admin_id",
                table: "NOTIFICATION_SETTING",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "parent_id",
                table: "NOTIFICATION_SETTING",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "teacher_id",
                table: "NOTIFICATION_SETTING",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_NOTIFICATION_SETTING_admin_id",
                table: "NOTIFICATION_SETTING",
                column: "admin_id");

            migrationBuilder.CreateIndex(
                name: "IX_NOTIFICATION_SETTING_parent_id",
                table: "NOTIFICATION_SETTING",
                column: "parent_id");

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
                name: "IX_NOTIFICATION_parent_id",
                table: "NOTIFICATION",
                column: "parent_id");

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
    }
}
