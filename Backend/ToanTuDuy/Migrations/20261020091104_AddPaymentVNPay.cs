using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToanTuDuy.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentVNPay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CLASS_STUDENT_class_id",
                table: "CLASS_STUDENT");

            migrationBuilder.AlterColumn<string>(
                name: "transaction_id",
                table: "INVOICE",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "INVOICE",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "checkout_url",
                table: "INVOICE",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "expired_at",
                table: "INVOICE",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "paid_at",
                table: "INVOICE",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "qr_code",
                table: "INVOICE",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "txn_ref",
                table: "INVOICE",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "CLASS_STUDENT",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "invoice_id",
                table: "CLASS_STUDENT",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_INVOICE_transaction_id",
                table: "INVOICE",
                column: "transaction_id",
                unique: true,
                filter: "[transaction_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_INVOICE_txn_ref",
                table: "INVOICE",
                column: "txn_ref",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CLASS_STUDENT_class_id_student_id",
                table: "CLASS_STUDENT",
                columns: new[] { "class_id", "student_id" },
                unique: true,
                filter: "[status] IN ('ACTIVE', 'PENDING')");

            migrationBuilder.CreateIndex(
                name: "IX_CLASS_STUDENT_invoice_id",
                table: "CLASS_STUDENT",
                column: "invoice_id",
                unique: true,
                filter: "[invoice_id] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_CLASS_STUDENT_INVOICE_invoice_id",
                table: "CLASS_STUDENT",
                column: "invoice_id",
                principalTable: "INVOICE",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CLASS_STUDENT_INVOICE_invoice_id",
                table: "CLASS_STUDENT");

            migrationBuilder.DropIndex(
                name: "IX_INVOICE_transaction_id",
                table: "INVOICE");

            migrationBuilder.DropIndex(
                name: "IX_INVOICE_txn_ref",
                table: "INVOICE");

            migrationBuilder.DropIndex(
                name: "IX_CLASS_STUDENT_class_id_student_id",
                table: "CLASS_STUDENT");

            migrationBuilder.DropIndex(
                name: "IX_CLASS_STUDENT_invoice_id",
                table: "CLASS_STUDENT");

            migrationBuilder.DropColumn(
                name: "checkout_url",
                table: "INVOICE");

            migrationBuilder.DropColumn(
                name: "expired_at",
                table: "INVOICE");

            migrationBuilder.DropColumn(
                name: "paid_at",
                table: "INVOICE");

            migrationBuilder.DropColumn(
                name: "qr_code",
                table: "INVOICE");

            migrationBuilder.DropColumn(
                name: "txn_ref",
                table: "INVOICE");

            migrationBuilder.DropColumn(
                name: "invoice_id",
                table: "CLASS_STUDENT");

            migrationBuilder.AlterColumn<string>(
                name: "transaction_id",
                table: "INVOICE",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "INVOICE",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "CLASS_STUDENT",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_CLASS_STUDENT_class_id",
                table: "CLASS_STUDENT",
                column: "class_id");
        }
    }
}
