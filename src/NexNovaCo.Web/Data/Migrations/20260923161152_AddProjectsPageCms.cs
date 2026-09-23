using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexNovaCo.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectsPageCms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProjectsHeroSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    MobileTitle = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 600, nullable: false),
                    CtaLabel = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    CtaHref = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ImagePath = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectsHeroSettings", x => x.Id);
                    table.CheckConstraint("CK_ProjectsHeroSettings_Singleton", "Id = 1");
                });

            migrationBuilder.CreateTable(
                name: "ProjectsTestimonialsSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectsTestimonialsSettings", x => x.Id);
                    table.CheckConstraint("CK_ProjectsTestimonialsSettings_Singleton", "Id = 1");
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectsHeroSettings");

            migrationBuilder.DropTable(
                name: "ProjectsTestimonialsSettings");
        }
    }
}
