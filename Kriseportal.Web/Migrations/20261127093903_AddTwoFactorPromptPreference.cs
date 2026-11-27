using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kriseportal.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddTwoFactorPromptPreference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasDismissedTwoFactorPrompt",
                table: "AspNetUsers",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasDismissedTwoFactorPrompt",
                table: "AspNetUsers");
        }
    }
}
