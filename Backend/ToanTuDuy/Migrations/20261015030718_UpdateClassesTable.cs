using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToanTuDuy.Migrations
{
    /// <inheritdoc />
    public partial class UpdateClassesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CLASSES_TEACHER_PROFILE_teacher_id",
                table: "CLASSES");

            migrationBuilder.AlterColumn<int>(
                name: "teacher_id",
                table: "CLASSES",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "CLASSES",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "end_date",
                table: "CLASSES",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "start_date",
                table: "CLASSES",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "CLASSES",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CLASSES_TEACHER_PROFILE_teacher_id",
                table: "CLASSES",
                column: "teacher_id",
                principalTable: "TEACHER_PROFILE",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CLASSES_TEACHER_PROFILE_teacher_id",
                table: "CLASSES");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "CLASSES");

            migrationBuilder.DropColumn(
                name: "end_date",
                table: "CLASSES");

            migrationBuilder.DropColumn(
                name: "start_date",
                table: "CLASSES");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "CLASSES");

            migrationBuilder.AlterColumn<int>(
                name: "teacher_id",
                table: "CLASSES",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CLASSES_TEACHER_PROFILE_teacher_id",
                table: "CLASSES",
                column: "teacher_id",
                principalTable: "TEACHER_PROFILE",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
