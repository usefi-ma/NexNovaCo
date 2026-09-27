using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexNovaCo.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDetailTemplateSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MemberDetailTemplateSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    BreadcrumbHomeLabel = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    BreadcrumbSectionLabel = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    SkillsHeading = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberDetailTemplateSettings", x => x.Id);
                    table.CheckConstraint("CK_MemberDetailTemplateSettings_Singleton", "Id = 1");
                });

            migrationBuilder.CreateTable(
                name: "ProjectDetailTemplateSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    HeroCtaText = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    BreadcrumbHomeLabel = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    BreadcrumbSectionLabel = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    ProjectDetailsHeading = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    FeaturesHeading = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    ClientLabel = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    CategoryLabel = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    DateLabel = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    TechnologiesLabel = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    ReturnCtaText = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectDetailTemplateSettings", x => x.Id);
                    table.CheckConstraint("CK_ProjectDetailTemplateSettings_Singleton", "Id = 1");
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MemberDetailTemplateSettings");

            migrationBuilder.DropTable(
                name: "ProjectDetailTemplateSettings");
        }
    }
}
