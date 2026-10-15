using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToanTuDuy.Migrations
{
    /// <inheritdoc />
    public partial class RefactorSchemaV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ASSIGNMENT_TEACHER_PROFILE_teacher_id",
                table: "ASSIGNMENT");

            migrationBuilder.DropForeignKey(
                name: "FK_CLASSES_COURSE_course_id",
                table: "CLASSES");

            migrationBuilder.DropForeignKey(
                name: "FK_QUESTION_COURSE_course_id",
                table: "QUESTION");

            migrationBuilder.DropForeignKey(
                name: "FK_STUDENT_BADGE_BADGE_badge_id",
                table: "STUDENT_BADGE");

            migrationBuilder.DropForeignKey(
                name: "FK_STUDENT_BADGE_STUDENT_PROFILE_student_id",
                table: "STUDENT_BADGE");

            migrationBuilder.DropForeignKey(
                name: "FK_STUDENT_PROFILE_ACCOUNT_parent_account_id",
                table: "STUDENT_PROFILE");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CLASS_STUDENT",
                table: "CLASS_STUDENT");

            migrationBuilder.DropIndex(
                name: "IX_ASSIGNMENT_teacher_id",
                table: "ASSIGNMENT");

            migrationBuilder.DropPrimaryKey(
                name: "PK_STUDENT_BADGE",
                table: "STUDENT_BADGE");

            migrationBuilder.DropColumn(
                name: "fee",
                table: "COURSE");

            migrationBuilder.DropColumn(
                name: "total_sessions",
                table: "COURSE");

            migrationBuilder.DropColumn(
                name: "teacher_id",
                table: "ASSIGNMENT");

            migrationBuilder.RenameTable(
                name: "STUDENT_BADGE",
                newName: "StudentBadge");

            migrationBuilder.RenameColumn(
                name: "parent_account_id",
                table: "STUDENT_PROFILE",
                newName: "parent_id");

            migrationBuilder.RenameIndex(
                name: "IX_STUDENT_PROFILE_parent_account_id",
                table: "STUDENT_PROFILE",
                newName: "IX_STUDENT_PROFILE_parent_id");

            migrationBuilder.RenameColumn(
                name: "course_id",
                table: "QUESTION",
                newName: "bank_id");

            migrationBuilder.RenameIndex(
                name: "IX_QUESTION_course_id",
                table: "QUESTION",
                newName: "IX_QUESTION_bank_id");

            migrationBuilder.RenameColumn(
                name: "pin",
                table: "PARENT_PROFILE",
                newName: "pin_hash");

            migrationBuilder.RenameColumn(
                name: "course_id",
                table: "CLASSES",
                newName: "course_level_id");

            migrationBuilder.RenameIndex(
                name: "IX_CLASSES_course_id",
                table: "CLASSES",
                newName: "IX_CLASSES_course_level_id");

            migrationBuilder.RenameIndex(
                name: "IX_STUDENT_BADGE_badge_id",
                table: "StudentBadge",
                newName: "IX_StudentBadge_badge_id");

            migrationBuilder.AddColumn<string>(
                name: "created_at",
                table: "TEACHER_PROFILE",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "specialize",
                table: "TEACHER_PROFILE",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "STUDENT_PROFILE",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "gender",
                table: "STUDENT_PROFILE",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "STUDENT_PROFILE",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "completed_at",
                table: "STUDENT_ASSIGNMENT",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "image_url",
                table: "QUESTION",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "PARENT_PROFILE",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "email",
                table: "PARENT_PROFILE",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "end_time",
                table: "LESSON",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_importan",
                table: "LESSON",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "start_time",
                table: "LESSON",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "LEAVE_REQUEST",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "amount",
                table: "INVOICE",
                type: "int",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "INVOICE",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "COURSE",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "COURSE",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "image_url",
                table: "COURSE",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "max_age",
                table: "COURSE",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "min_age",
                table: "COURSE",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "COURSE",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "end_date",
                table: "CLASSES",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "start_date",
                table: "CLASSES",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "id",
                table: "CLASS_STUDENT",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "CLASS_STUDENT",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "start_time",
                table: "CLASS_SCHEDULE",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(TimeSpan),
                oldType: "time");

            migrationBuilder.AlterColumn<DateTime>(
                name: "end_time",
                table: "CLASS_SCHEDULE",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(TimeSpan),
                oldType: "time");

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "BADGE",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "icon_url",
                table: "BADGE",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "teacher_notes",
                table: "ATTENDANCE",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "max_attempt",
                table: "ASSIGNMENT",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "stage_id",
                table: "ASSIGNMENT",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "ACCOUNT",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "ACCOUNT",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "id",
                table: "StudentBadge",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CLASS_STUDENT",
                table: "CLASS_STUDENT",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_StudentBadge",
                table: "StudentBadge",
                column: "id");

            migrationBuilder.CreateTable(
                name: "COURSE_LEVEL",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    course_id = table.Column<int>(type: "int", nullable: false),
                    code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    fee = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    total_sessions = table.Column<int>(type: "int", nullable: false),
                    order_index = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_COURSE_LEVEL", x => x.id);
                    table.ForeignKey(
                        name: "FK_COURSE_LEVEL_COURSE_course_id",
                        column: x => x.course_id,
                        principalTable: "COURSE",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NOTIFICATION",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    parent_id = table.Column<int>(type: "int", nullable: false),
                    title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    body = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ref_id = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    is_read = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NOTIFICATION", x => x.id);
                    table.ForeignKey(
                        name: "FK_NOTIFICATION_PARENT_PROFILE_parent_id",
                        column: x => x.parent_id,
                        principalTable: "PARENT_PROFILE",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NOTIFICATION_SETTING",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    parent_id = table.Column<int>(type: "int", nullable: false),
                    type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    enabled = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NOTIFICATION_SETTING", x => x.id);
                    table.ForeignKey(
                        name: "FK_NOTIFICATION_SETTING_PARENT_PROFILE_parent_id",
                        column: x => x.parent_id,
                        principalTable: "PARENT_PROFILE",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QUESTION_ASSIGNMENT",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    assignment_id = table.Column<int>(type: "int", nullable: false),
                    question_id = table.Column<int>(type: "int", nullable: false),
                    order_index = table.Column<int>(type: "int", nullable: false),
                    point_value = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QUESTION_ASSIGNMENT", x => x.id);
                    table.ForeignKey(
                        name: "FK_QUESTION_ASSIGNMENT_ASSIGNMENT_assignment_id",
                        column: x => x.assignment_id,
                        principalTable: "ASSIGNMENT",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QUESTION_ASSIGNMENT_QUESTION_question_id",
                        column: x => x.question_id,
                        principalTable: "QUESTION",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QUESTION_BANK",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    course_id = table.Column<int>(type: "int", nullable: false),
                    name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QUESTION_BANK", x => x.id);
                    table.ForeignKey(
                        name: "FK_QUESTION_BANK_COURSE_course_id",
                        column: x => x.course_id,
                        principalTable: "COURSE",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CLASS_STUDENT_class_id",
                table: "CLASS_STUDENT",
                column: "class_id");

            migrationBuilder.CreateIndex(
                name: "IX_StudentBadge_student_id",
                table: "StudentBadge",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_COURSE_LEVEL_course_id",
                table: "COURSE_LEVEL",
                column: "course_id");

            migrationBuilder.CreateIndex(
                name: "IX_NOTIFICATION_parent_id",
                table: "NOTIFICATION",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "IX_NOTIFICATION_SETTING_parent_id",
                table: "NOTIFICATION_SETTING",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "IX_QUESTION_ASSIGNMENT_assignment_id",
                table: "QUESTION_ASSIGNMENT",
                column: "assignment_id");

            migrationBuilder.CreateIndex(
                name: "IX_QUESTION_ASSIGNMENT_question_id",
                table: "QUESTION_ASSIGNMENT",
                column: "question_id");

            migrationBuilder.CreateIndex(
                name: "IX_QUESTION_BANK_course_id",
                table: "QUESTION_BANK",
                column: "course_id");

            migrationBuilder.AddForeignKey(
                name: "FK_CLASSES_COURSE_LEVEL_course_level_id",
                table: "CLASSES",
                column: "course_level_id",
                principalTable: "COURSE_LEVEL",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QUESTION_QUESTION_BANK_bank_id",
                table: "QUESTION",
                column: "bank_id",
                principalTable: "QUESTION_BANK",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_STUDENT_PROFILE_PARENT_PROFILE_parent_id",
                table: "STUDENT_PROFILE",
                column: "parent_id",
                principalTable: "PARENT_PROFILE",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentBadge_BADGE_badge_id",
                table: "StudentBadge",
                column: "badge_id",
                principalTable: "BADGE",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentBadge_STUDENT_PROFILE_student_id",
                table: "StudentBadge",
                column: "student_id",
                principalTable: "STUDENT_PROFILE",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CLASSES_COURSE_LEVEL_course_level_id",
                table: "CLASSES");

            migrationBuilder.DropForeignKey(
                name: "FK_QUESTION_QUESTION_BANK_bank_id",
                table: "QUESTION");

            migrationBuilder.DropForeignKey(
                name: "FK_STUDENT_PROFILE_PARENT_PROFILE_parent_id",
                table: "STUDENT_PROFILE");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentBadge_BADGE_badge_id",
                table: "StudentBadge");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentBadge_STUDENT_PROFILE_student_id",
                table: "StudentBadge");

            migrationBuilder.DropTable(
                name: "COURSE_LEVEL");

            migrationBuilder.DropTable(
                name: "NOTIFICATION");

            migrationBuilder.DropTable(
                name: "NOTIFICATION_SETTING");

            migrationBuilder.DropTable(
                name: "QUESTION_ASSIGNMENT");

            migrationBuilder.DropTable(
                name: "QUESTION_BANK");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CLASS_STUDENT",
                table: "CLASS_STUDENT");

            migrationBuilder.DropIndex(
                name: "IX_CLASS_STUDENT_class_id",
                table: "CLASS_STUDENT");

            migrationBuilder.DropPrimaryKey(
                name: "PK_StudentBadge",
                table: "StudentBadge");

            migrationBuilder.DropIndex(
                name: "IX_StudentBadge_student_id",
                table: "StudentBadge");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "TEACHER_PROFILE");

            migrationBuilder.DropColumn(
                name: "specialize",
                table: "TEACHER_PROFILE");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "STUDENT_PROFILE");

            migrationBuilder.DropColumn(
                name: "gender",
                table: "STUDENT_PROFILE");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "STUDENT_PROFILE");

            migrationBuilder.DropColumn(
                name: "completed_at",
                table: "STUDENT_ASSIGNMENT");

            migrationBuilder.DropColumn(
                name: "image_url",
                table: "QUESTION");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "PARENT_PROFILE");

            migrationBuilder.DropColumn(
                name: "email",
                table: "PARENT_PROFILE");

            migrationBuilder.DropColumn(
                name: "end_time",
                table: "LESSON");

            migrationBuilder.DropColumn(
                name: "is_importan",
                table: "LESSON");

            migrationBuilder.DropColumn(
                name: "start_time",
                table: "LESSON");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "LEAVE_REQUEST");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "INVOICE");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "COURSE");

            migrationBuilder.DropColumn(
                name: "description",
                table: "COURSE");

            migrationBuilder.DropColumn(
                name: "image_url",
                table: "COURSE");

            migrationBuilder.DropColumn(
                name: "max_age",
                table: "COURSE");

            migrationBuilder.DropColumn(
                name: "min_age",
                table: "COURSE");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "COURSE");

            migrationBuilder.DropColumn(
                name: "end_date",
                table: "CLASSES");

            migrationBuilder.DropColumn(
                name: "start_date",
                table: "CLASSES");

            migrationBuilder.DropColumn(
                name: "id",
                table: "CLASS_STUDENT");

            migrationBuilder.DropColumn(
                name: "status",
                table: "CLASS_STUDENT");

            migrationBuilder.DropColumn(
                name: "description",
                table: "BADGE");

            migrationBuilder.DropColumn(
                name: "icon_url",
                table: "BADGE");

            migrationBuilder.DropColumn(
                name: "teacher_notes",
                table: "ATTENDANCE");

            migrationBuilder.DropColumn(
                name: "max_attempt",
                table: "ASSIGNMENT");

            migrationBuilder.DropColumn(
                name: "stage_id",
                table: "ASSIGNMENT");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "ACCOUNT");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "ACCOUNT");

            migrationBuilder.DropColumn(
                name: "id",
                table: "StudentBadge");

            migrationBuilder.RenameTable(
                name: "StudentBadge",
                newName: "STUDENT_BADGE");

            migrationBuilder.RenameColumn(
                name: "parent_id",
                table: "STUDENT_PROFILE",
                newName: "parent_account_id");

            migrationBuilder.RenameIndex(
                name: "IX_STUDENT_PROFILE_parent_id",
                table: "STUDENT_PROFILE",
                newName: "IX_STUDENT_PROFILE_parent_account_id");

            migrationBuilder.RenameColumn(
                name: "bank_id",
                table: "QUESTION",
                newName: "course_id");

            migrationBuilder.RenameIndex(
                name: "IX_QUESTION_bank_id",
                table: "QUESTION",
                newName: "IX_QUESTION_course_id");

            migrationBuilder.RenameColumn(
                name: "pin_hash",
                table: "PARENT_PROFILE",
                newName: "pin");

            migrationBuilder.RenameColumn(
                name: "course_level_id",
                table: "CLASSES",
                newName: "course_id");

            migrationBuilder.RenameIndex(
                name: "IX_CLASSES_course_level_id",
                table: "CLASSES",
                newName: "IX_CLASSES_course_id");

            migrationBuilder.RenameIndex(
                name: "IX_StudentBadge_badge_id",
                table: "STUDENT_BADGE",
                newName: "IX_STUDENT_BADGE_badge_id");

            migrationBuilder.AlterColumn<decimal>(
                name: "amount",
                table: "INVOICE",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<decimal>(
                name: "fee",
                table: "COURSE",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "total_sessions",
                table: "COURSE",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "start_time",
                table: "CLASS_SCHEDULE",
                type: "time",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "end_time",
                table: "CLASS_SCHEDULE",
                type: "time",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<int>(
                name: "teacher_id",
                table: "ASSIGNMENT",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_CLASS_STUDENT",
                table: "CLASS_STUDENT",
                columns: new[] { "class_id", "student_id" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_STUDENT_BADGE",
                table: "STUDENT_BADGE",
                columns: new[] { "student_id", "badge_id" });

            migrationBuilder.CreateIndex(
                name: "IX_ASSIGNMENT_teacher_id",
                table: "ASSIGNMENT",
                column: "teacher_id");

            migrationBuilder.AddForeignKey(
                name: "FK_ASSIGNMENT_TEACHER_PROFILE_teacher_id",
                table: "ASSIGNMENT",
                column: "teacher_id",
                principalTable: "TEACHER_PROFILE",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CLASSES_COURSE_course_id",
                table: "CLASSES",
                column: "course_id",
                principalTable: "COURSE",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QUESTION_COURSE_course_id",
                table: "QUESTION",
                column: "course_id",
                principalTable: "COURSE",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_STUDENT_BADGE_BADGE_badge_id",
                table: "STUDENT_BADGE",
                column: "badge_id",
                principalTable: "BADGE",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_STUDENT_BADGE_STUDENT_PROFILE_student_id",
                table: "STUDENT_BADGE",
                column: "student_id",
                principalTable: "STUDENT_PROFILE",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_STUDENT_PROFILE_ACCOUNT_parent_account_id",
                table: "STUDENT_PROFILE",
                column: "parent_account_id",
                principalTable: "ACCOUNT",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
