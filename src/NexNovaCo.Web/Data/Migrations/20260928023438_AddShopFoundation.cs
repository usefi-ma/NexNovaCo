using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexNovaCo.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddShopFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductInitializationState",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductInitializationState", x => x.Id);
                    table.CheckConstraint("CK_ProductInitializationState_Singleton", "Id = 1");
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false, collation: "NOCASE"),
                    Tagline = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    ShortDescription = table.Column<string>(type: "TEXT", maxLength: 240, nullable: false),
                    Price = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    OriginalPrice = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: true),
                    Badge = table.Column<int>(type: "INTEGER", nullable: false),
                    ImagePath = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                    table.CheckConstraint("CK_Products_Badge", "Badge BETWEEN 0 AND 4");
                    table.CheckConstraint("CK_Products_Order", "DisplayOrder > 0");
                    table.CheckConstraint("CK_Products_OriginalPrice", "OriginalPrice IS NULL OR OriginalPrice >= 0");
                    table.CheckConstraint("CK_Products_Price", "Price >= 0");
                });

            migrationBuilder.CreateTable(
                name: "ShopHeroSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Eyebrow = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 600, nullable: false),
                    CtaLabel = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    ImagePath = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopHeroSettings", x => x.Id);
                    table.CheckConstraint("CK_ShopHeroSettings_Singleton", "Id = 1");
                });

            migrationBuilder.CreateTable(
                name: "ShopProductsSectionSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Eyebrow = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Introduction = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopProductsSectionSettings", x => x.Id);
                    table.CheckConstraint("CK_ShopProductsSectionSettings_Singleton", "Id = 1");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Products_DisplayOrder",
                table: "Products",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Slug",
                table: "Products",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductInitializationState");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "ShopHeroSettings");

            migrationBuilder.DropTable(
                name: "ShopProductsSectionSettings");
        }
    }
}
