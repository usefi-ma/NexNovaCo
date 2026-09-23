using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexNovaCo.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHomeImagePaths : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImagePath",
                table: "HomeWelcomeSettings",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "image/home/welcome.jpg");

            migrationBuilder.AddColumn<string>(
                name: "ImagePath",
                table: "HomeHeroSettings",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "image/home/header.jpg");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImagePath",
                table: "HomeWelcomeSettings");

            migrationBuilder.DropColumn(
                name: "ImagePath",
                table: "HomeHeroSettings");
        }
    }
}
