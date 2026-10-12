using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexos_Network_System.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase3OrderRetailVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VerificationNotes",
                table: "Orders",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "VerifiedAt",
                table: "Orders",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VerifiedByEmployeeId",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_VerifiedByEmployeeId",
                table: "Orders",
                column: "VerifiedByEmployeeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Employees_VerifiedByEmployeeId",
                table: "Orders",
                column: "VerifiedByEmployeeId",
                principalTable: "Employees",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Employees_VerifiedByEmployeeId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_VerifiedByEmployeeId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "VerificationNotes",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "VerifiedAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "VerifiedByEmployeeId",
                table: "Orders");
        }
    }
}
