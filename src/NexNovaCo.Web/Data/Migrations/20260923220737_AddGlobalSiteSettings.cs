using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexNovaCo.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGlobalSiteSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FooterSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Copyright = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    NewsletterHeading = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    NewsletterPlaceholder = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    NewsletterSubmitLabel = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    NewsletterVisible = table.Column<bool>(type: "INTEGER", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FooterSettings", x => x.Id);
                    table.CheckConstraint("CK_FooterSettings_Singleton", "Id = 1");
                });

            migrationBuilder.CreateTable(
                name: "NavigationInitializationState",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    InitializedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NavigationInitializationState", x => x.Id);
                    table.CheckConstraint("CK_NavigationInitializationState_Singleton", "Id = 1");
                });

            migrationBuilder.CreateTable(
                name: "NavigationItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    Label = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Url = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NavigationItems", x => x.Id);
                    table.CheckConstraint("CK_NavigationItems_Order", "DisplayOrder > 0");
                });

            migrationBuilder.CreateTable(
                name: "SiteIdentitySettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    SiteName = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    ImagePath = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteIdentitySettings", x => x.Id);
                    table.CheckConstraint("CK_SiteIdentitySettings_Singleton", "Id = 1");
                });

            migrationBuilder.CreateTable(
                name: "SocialLinkInitializationState",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    InitializedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SocialLinkInitializationState", x => x.Id);
                    table.CheckConstraint("CK_SocialLinkInitializationState_Singleton", "Id = 1");
                });

            migrationBuilder.CreateTable(
                name: "SocialLinkItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    Platform = table.Column<int>(type: "INTEGER", nullable: false),
                    Url = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SocialLinkItems", x => x.Id);
                    table.CheckConstraint("CK_SocialLinkItems_Order", "DisplayOrder > 0");
                });

            migrationBuilder.CreateIndex(
                name: "IX_NavigationItems_DisplayOrder",
                table: "NavigationItems",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_SocialLinkItems_DisplayOrder",
                table: "SocialLinkItems",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_SocialLinkItems_Platform",
                table: "SocialLinkItems",
                column: "Platform",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FooterSettings");

            migrationBuilder.DropTable(
                name: "NavigationInitializationState");

            migrationBuilder.DropTable(
                name: "NavigationItems");

            migrationBuilder.DropTable(
                name: "SiteIdentitySettings");

            migrationBuilder.DropTable(
                name: "SocialLinkInitializationState");

            migrationBuilder.DropTable(
                name: "SocialLinkItems");
        }
    }
}
