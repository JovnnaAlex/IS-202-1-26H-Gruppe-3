using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kriseportal.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddRequestResponseMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ResponseMessage",
                table: "Requests",
                type: "varchar(2000)",
                maxLength: 2000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResponseMessage",
                table: "Requests");
        }
    }
}
