using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToanTuDuy.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseDetailFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "faqs",
                table: "COURSE_LEVEL",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "syllabus",
                table: "COURSE_LEVEL",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "benefits",
                table: "COURSE",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "facility_images",
                table: "COURSE",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "rating",
                table: "COURSE",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "reviews",
                table: "COURSE",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "video_url",
                table: "COURSE",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "faqs",
                table: "COURSE_LEVEL");

            migrationBuilder.DropColumn(
                name: "syllabus",
                table: "COURSE_LEVEL");

            migrationBuilder.DropColumn(
                name: "benefits",
                table: "COURSE");

            migrationBuilder.DropColumn(
                name: "facility_images",
                table: "COURSE");

            migrationBuilder.DropColumn(
                name: "rating",
                table: "COURSE");

            migrationBuilder.DropColumn(
                name: "reviews",
                table: "COURSE");

            migrationBuilder.DropColumn(
                name: "video_url",
                table: "COURSE");
        }
    }
}
