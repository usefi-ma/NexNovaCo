using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexNovaCo.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHomeHeroSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HomeHeroSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    OpeningLine = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    EmphasisLine = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    ClosingLine = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    CtaLabel = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    CtaHref = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HomeHeroSettings", x => x.Id);
                    table.CheckConstraint("CK_HomeHeroSettings_Singleton", "Id = 1");
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HomeHeroSettings");
        }
    }
}
