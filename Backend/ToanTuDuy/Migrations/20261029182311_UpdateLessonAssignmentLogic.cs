using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToanTuDuy.Migrations
{
    /// <inheritdoc />
    public partial class UpdateLessonAssignmentLogic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ASSIGNMENT_CLASSES_class_id",
                table: "ASSIGNMENT");

            migrationBuilder.RenameColumn(
                name: "class_id",
                table: "ASSIGNMENT",
                newName: "lesson_id");

            migrationBuilder.RenameIndex(
                name: "IX_ASSIGNMENT_class_id",
                table: "ASSIGNMENT",
                newName: "IX_ASSIGNMENT_lesson_id");

            migrationBuilder.AddColumn<string>(
                name: "title",
                table: "LESSON",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ASSIGNMENT_LESSON_lesson_id",
                table: "ASSIGNMENT",
                column: "lesson_id",
                principalTable: "LESSON",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ASSIGNMENT_LESSON_lesson_id",
                table: "ASSIGNMENT");

            migrationBuilder.DropColumn(
                name: "title",
                table: "LESSON");

            migrationBuilder.RenameColumn(
                name: "lesson_id",
                table: "ASSIGNMENT",
                newName: "class_id");

            migrationBuilder.RenameIndex(
                name: "IX_ASSIGNMENT_lesson_id",
                table: "ASSIGNMENT",
                newName: "IX_ASSIGNMENT_class_id");

            migrationBuilder.AddForeignKey(
                name: "FK_ASSIGNMENT_CLASSES_class_id",
                table: "ASSIGNMENT",
                column: "class_id",
                principalTable: "CLASSES",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
