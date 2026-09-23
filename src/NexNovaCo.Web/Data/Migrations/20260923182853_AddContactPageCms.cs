using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexNovaCo.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddContactPageCms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContactFormSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Heading = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    FirstNameLabel = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    FirstNamePlaceholder = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    LastNameLabel = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    LastNamePlaceholder = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    EmailLabel = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    EmailPlaceholder = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    SubjectLabel = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    SubjectPlaceholder = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    MessageLabel = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    MessagePlaceholder = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    SubmitLabel = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    InvalidMessage = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactFormSettings", x => x.Id);
                    table.CheckConstraint("CK_ContactFormSettings_Singleton", "Id = 1");
                });

            migrationBuilder.CreateTable(
                name: "ContactPageSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    HeroTitle = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    HeroDescription = table.Column<string>(type: "TEXT", maxLength: 600, nullable: false),
                    HeroCtaLabel = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    HeroCtaHref = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    HeroImagePath = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    InfoHeading = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    MapHeading = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    MapDescription = table.Column<string>(type: "TEXT", maxLength: 400, nullable: false),
                    MapEmbedUrl = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                    MapTitle = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactPageSettings", x => x.Id);
                    table.CheckConstraint("CK_ContactPageSettings_Singleton", "Id = 1");
                });

            migrationBuilder.CreateTable(
                name: "SiteContactSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 254, nullable: false),
                    Address = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteContactSettings", x => x.Id);
                    table.CheckConstraint("CK_SiteContactSettings_Singleton", "Id = 1");
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContactFormSettings");

            migrationBuilder.DropTable(
                name: "ContactPageSettings");

            migrationBuilder.DropTable(
                name: "SiteContactSettings");
        }
    }
}
