using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToanTuDuy.Migrations
{
    /// <inheritdoc />
    public partial class AddPayrollManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PAYROLL_SUMMARY",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    teacher_id = table.Column<int>(type: "int", nullable: false),
                    month = table.Column<int>(type: "int", nullable: false),
                    year = table.Column<int>(type: "int", nullable: false),
                    total_valid_sessions = table.Column<int>(type: "int", nullable: false),
                    base_salary = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    total_penalty = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    total_bonus = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    final_salary = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    email_status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    email_sent_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    email_error_message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PAYROLL_SUMMARY", x => x.id);
                    table.ForeignKey(
                        name: "FK_PAYROLL_SUMMARY_TEACHER_PROFILE_teacher_id",
                        column: x => x.teacher_id,
                        principalTable: "TEACHER_PROFILE",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SALARY_ADJUSTMENT",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    teacher_id = table.Column<int>(type: "int", nullable: false),
                    type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    applied_date = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SALARY_ADJUSTMENT", x => x.id);
                    table.ForeignKey(
                        name: "FK_SALARY_ADJUSTMENT_TEACHER_PROFILE_teacher_id",
                        column: x => x.teacher_id,
                        principalTable: "TEACHER_PROFILE",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TEACHER_SALARY_CONFIG",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    teacher_id = table.Column<int>(type: "int", nullable: false),
                    base_rate_per_session = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TEACHER_SALARY_CONFIG", x => x.id);
                    table.ForeignKey(
                        name: "FK_TEACHER_SALARY_CONFIG_TEACHER_PROFILE_teacher_id",
                        column: x => x.teacher_id,
                        principalTable: "TEACHER_PROFILE",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PAYROLL_SUMMARY_teacher_id_month_year",
                table: "PAYROLL_SUMMARY",
                columns: new[] { "teacher_id", "month", "year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SALARY_ADJUSTMENT_teacher_id_applied_date",
                table: "SALARY_ADJUSTMENT",
                columns: new[] { "teacher_id", "applied_date" });

            migrationBuilder.CreateIndex(
                name: "IX_TEACHER_SALARY_CONFIG_teacher_id",
                table: "TEACHER_SALARY_CONFIG",
                column: "teacher_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PAYROLL_SUMMARY");

            migrationBuilder.DropTable(
                name: "SALARY_ADJUSTMENT");

            migrationBuilder.DropTable(
                name: "TEACHER_SALARY_CONFIG");
        }
    }
}
