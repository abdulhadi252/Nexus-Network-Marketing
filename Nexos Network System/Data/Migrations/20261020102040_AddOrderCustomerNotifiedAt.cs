using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexos_Network_System.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderCustomerNotifiedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CustomerNotifiedAt",
                table: "Orders",
                type: "datetimeoffset",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomerNotifiedAt",
                table: "Orders");
        }
    }
}
