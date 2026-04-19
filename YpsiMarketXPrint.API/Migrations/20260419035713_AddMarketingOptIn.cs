using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YpsiMarketXPrint.API.Migrations
{
    /// <inheritdoc />
    public partial class AddMarketingOptIn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "MarketingOptIn",
                table: "Users",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MarketingOptIn",
                table: "Users");
        }
    }
}
