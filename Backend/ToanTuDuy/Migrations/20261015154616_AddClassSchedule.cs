using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToanTuDuy.Migrations
{
    /// <inheritdoc />
    public partial class AddClassSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "day_of_week",
                table: "CLASS_SCHEDULE",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "CLASS_SCHEDULE",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "room",
                table: "CLASS_SCHEDULE",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "CLASS_SCHEDULE",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "created_at",
                table: "CLASS_SCHEDULE");

            migrationBuilder.DropColumn(
                name: "room",
                table: "CLASS_SCHEDULE");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "CLASS_SCHEDULE");

            migrationBuilder.AlterColumn<string>(
                name: "day_of_week",
                table: "CLASS_SCHEDULE",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");
        }
    }
}
