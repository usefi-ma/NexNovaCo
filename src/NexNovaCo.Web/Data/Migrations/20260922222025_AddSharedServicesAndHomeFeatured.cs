using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexNovaCo.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSharedServicesAndHomeFeatured : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ServiceInitializationState",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceInitializationState", x => x.Id);
                    table.CheckConstraint("CK_ServiceInitializationState_Singleton", "Id = 1");
                });

            migrationBuilder.CreateTable(
                name: "Services",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    ContentKey = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    Tagline = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    IconPath = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Services", x => x.Id);
                    table.CheckConstraint("CK_Services_Order", "DisplayOrder > 0");
                });

            migrationBuilder.CreateTable(
                name: "HomeFeaturedServices",
                columns: table => new
                {
                    ServiceId = table.Column<int>(type: "INTEGER", nullable: false),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HomeFeaturedServices", x => x.ServiceId);
                    table.CheckConstraint("CK_HomeFeaturedServices_Order", "DisplayOrder BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "FK_HomeFeaturedServices_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HomeFeaturedServices_DisplayOrder",
                table: "HomeFeaturedServices",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_Services_ContentKey",
                table: "Services",
                column: "ContentKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Services_DisplayOrder",
                table: "Services",
                column: "DisplayOrder");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HomeFeaturedServices");

            migrationBuilder.DropTable(
                name: "ServiceInitializationState");

            migrationBuilder.DropTable(
                name: "Services");
        }
    }
}
