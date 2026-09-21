using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexNovaCo.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHomeWelcomeSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HomeWelcomeSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Introduction = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    ParagraphOne = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    ParagraphTwo = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    CtaLabel = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    CtaHref = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HomeWelcomeSettings", x => x.Id);
                    table.CheckConstraint("CK_HomeWelcomeSettings_Singleton", "Id = 1");
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HomeWelcomeSettings");
        }
    }
}
