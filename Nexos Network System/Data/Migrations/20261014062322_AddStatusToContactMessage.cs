using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexos_Network_System.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStatusToContactMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "ContactMessages",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "ContactMessages");
        }
    }
}
