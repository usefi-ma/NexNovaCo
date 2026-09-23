using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexNovaCo.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSharedMembersAndHomeFeatured : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MemberInitializationState",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberInitializationState", x => x.Id);
                    table.CheckConstraint("CK_MemberInitializationState_Singleton", "Id = 1");
                });

            migrationBuilder.CreateTable(
                name: "Members",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false, collation: "NOCASE"),
                    Name = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Role = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Introduction = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Biography = table.Column<string>(type: "TEXT", maxLength: 10000, nullable: false),
                    ImagePath = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 254, nullable: true),
                    LinkedIn = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Telegram = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Members", x => x.Id);
                    table.CheckConstraint("CK_Members_Order", "DisplayOrder > 0");
                });

            migrationBuilder.CreateTable(
                name: "HomeFeaturedMembers",
                columns: table => new
                {
                    MemberId = table.Column<int>(type: "INTEGER", nullable: false),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HomeFeaturedMembers", x => x.MemberId);
                    table.CheckConstraint("CK_HomeFeaturedMembers_Order", "DisplayOrder > 0");
                    table.ForeignKey(
                        name: "FK_HomeFeaturedMembers_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MemberSkills",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MemberId = table.Column<int>(type: "INTEGER", nullable: false),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    Text = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberSkills", x => x.Id);
                    table.CheckConstraint("CK_MemberSkills_Order", "DisplayOrder > 0");
                    table.ForeignKey(
                        name: "FK_MemberSkills_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HomeFeaturedMembers_DisplayOrder",
                table: "HomeFeaturedMembers",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_Members_DisplayOrder",
                table: "Members",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_Members_Slug",
                table: "Members",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MemberSkills_MemberId_DisplayOrder",
                table: "MemberSkills",
                columns: new[] { "MemberId", "DisplayOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HomeFeaturedMembers");

            migrationBuilder.DropTable(
                name: "MemberInitializationState");

            migrationBuilder.DropTable(
                name: "MemberSkills");

            migrationBuilder.DropTable(
                name: "Members");
        }
    }
}
