using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexNovaCo.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHomeStatistics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HomeStatistics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    Label = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Value = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HomeStatistics", x => x.Id);
                    table.CheckConstraint("CK_HomeStatistics_FixedSlots", "Id BETWEEN 1 AND 4 AND DisplayOrder = Id");
                    table.CheckConstraint("CK_HomeStatistics_Value", "Value BETWEEN 0 AND 9999");
                });

            migrationBuilder.CreateIndex(
                name: "IX_HomeStatistics_DisplayOrder",
                table: "HomeStatistics",
                column: "DisplayOrder",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HomeStatistics");
        }
    }
}
