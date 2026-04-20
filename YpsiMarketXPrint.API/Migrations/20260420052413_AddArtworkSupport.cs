using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YpsiMarketXPrint.API.Migrations
{
    /// <inheritdoc />
    public partial class AddArtworkSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RequiresArtwork",
                table: "Products",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ArtworkUrl",
                table: "OrderItems",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequiresArtwork",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ArtworkUrl",
                table: "OrderItems");
        }
    }
}
