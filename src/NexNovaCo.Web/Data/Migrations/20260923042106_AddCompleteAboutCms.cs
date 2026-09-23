using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexNovaCo.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCompleteAboutCms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AboutHeroSettings",
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
                    table.PrimaryKey("PK_AboutHeroSettings", x => x.Id);
                    table.CheckConstraint("CK_AboutHeroSettings_Singleton", "Id = 1");
                });

            migrationBuilder.CreateTable(
                name: "AboutMissionPointInitializationState",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    InitializedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AboutMissionPointInitializationState", x => x.Id);
                    table.CheckConstraint("CK_AboutMissionPointInitializationState_Singleton", "Id = 1");
                });

            migrationBuilder.CreateTable(
                name: "AboutMissionPointItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AboutMissionPointItems", x => x.Id);
                    table.CheckConstraint("CK_AboutMissionPointItem_Order", "DisplayOrder > 0");
                });

            migrationBuilder.CreateTable(
                name: "AboutMissionSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    BrandTitle = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    BrandDescription = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    ParagraphOne = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    ParagraphTwo = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AboutMissionSettings", x => x.Id);
                    table.CheckConstraint("CK_AboutMissionSettings_Singleton", "Id = 1");
                });

            migrationBuilder.CreateTable(
                name: "AboutPartnersSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AboutPartnersSettings", x => x.Id);
                    table.CheckConstraint("CK_AboutPartnersSettings_Singleton", "Id = 1");
                });

            migrationBuilder.CreateTable(
                name: "AboutStorySettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Subtitle = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    IntroductionOne = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    IntroductionTwo = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    DetailOne = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    DetailTwo = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Closing = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AboutStorySettings", x => x.Id);
                    table.CheckConstraint("CK_AboutStorySettings_Singleton", "Id = 1");
                });

            migrationBuilder.CreateTable(
                name: "AboutTimelineInitializationState",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    InitializedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AboutTimelineInitializationState", x => x.Id);
                    table.CheckConstraint("CK_AboutTimelineInitializationState_Singleton", "Id = 1");
                });

            migrationBuilder.CreateTable(
                name: "AboutTimelineItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    Year = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 600, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AboutTimelineItems", x => x.Id);
                    table.CheckConstraint("CK_AboutTimelineItem_Order", "DisplayOrder > 0");
                    table.CheckConstraint("CK_AboutTimelineItem_Year", "Year BETWEEN 1000 AND 9999");
                });

            migrationBuilder.CreateTable(
                name: "AboutTimelineSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    CtaLabel = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    CtaHref = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AboutTimelineSettings", x => x.Id);
                    table.CheckConstraint("CK_AboutTimelineSettings_Singleton", "Id = 1");
                });

            migrationBuilder.CreateTable(
                name: "AboutVisionSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    ParagraphOne = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    ParagraphTwo = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    CtaLabel = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    CtaHref = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ImagePath = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ImageAlt = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AboutVisionSettings", x => x.Id);
                    table.CheckConstraint("CK_AboutVisionSettings_Singleton", "Id = 1");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AboutMissionPointItems_DisplayOrder",
                table: "AboutMissionPointItems",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_AboutTimelineItems_DisplayOrder",
                table: "AboutTimelineItems",
                column: "DisplayOrder");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AboutHeroSettings");

            migrationBuilder.DropTable(
                name: "AboutMissionPointInitializationState");

            migrationBuilder.DropTable(
                name: "AboutMissionPointItems");

            migrationBuilder.DropTable(
                name: "AboutMissionSettings");

            migrationBuilder.DropTable(
                name: "AboutPartnersSettings");

            migrationBuilder.DropTable(
                name: "AboutStorySettings");

            migrationBuilder.DropTable(
                name: "AboutTimelineInitializationState");

            migrationBuilder.DropTable(
                name: "AboutTimelineItems");

            migrationBuilder.DropTable(
                name: "AboutTimelineSettings");

            migrationBuilder.DropTable(
                name: "AboutVisionSettings");
        }
    }
}
