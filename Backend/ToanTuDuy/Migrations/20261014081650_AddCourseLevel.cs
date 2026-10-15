using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToanTuDuy.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseLevel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CLASSES_COURSE_course_id",
                table: "CLASSES");

            migrationBuilder.DropColumn(
                name: "fee",
                table: "COURSE");

            migrationBuilder.RenameColumn(
                name: "total_sessions",
                table: "COURSE",
                newName: "min_age");

            migrationBuilder.RenameColumn(
                name: "course_id",
                table: "CLASSES",
                newName: "course_level_id");

            migrationBuilder.RenameIndex(
                name: "IX_CLASSES_course_id",
                table: "CLASSES",
                newName: "IX_CLASSES_course_level_id");

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "COURSE",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

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
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "COURSE",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "COURSE_LEVEL",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    course_id = table.Column<int>(type: "int", nullable: false),
                    code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    order_index = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    fee = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    total_sessions = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
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

            migrationBuilder.CreateIndex(
                name: "IX_COURSE_LEVEL_course_id",
                table: "COURSE_LEVEL",
                column: "course_id");

            migrationBuilder.AddForeignKey(
                name: "FK_CLASSES_COURSE_LEVEL_course_level_id",
                table: "CLASSES",
                column: "course_level_id",
                principalTable: "COURSE_LEVEL",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CLASSES_COURSE_LEVEL_course_level_id",
                table: "CLASSES");

            migrationBuilder.DropTable(
                name: "COURSE_LEVEL");

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
                name: "updated_at",
                table: "COURSE");

            migrationBuilder.RenameColumn(
                name: "min_age",
                table: "COURSE",
                newName: "total_sessions");

            migrationBuilder.RenameColumn(
                name: "course_level_id",
                table: "CLASSES",
                newName: "course_id");

            migrationBuilder.RenameIndex(
                name: "IX_CLASSES_course_level_id",
                table: "CLASSES",
                newName: "IX_CLASSES_course_id");

            migrationBuilder.AddColumn<decimal>(
                name: "fee",
                table: "COURSE",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddForeignKey(
                name: "FK_CLASSES_COURSE_course_id",
                table: "CLASSES",
                column: "course_id",
                principalTable: "COURSE",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
