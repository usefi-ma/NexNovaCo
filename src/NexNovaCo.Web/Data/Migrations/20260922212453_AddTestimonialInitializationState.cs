using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexNovaCo.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTestimonialInitializationState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TestimonialInitializationState",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestimonialInitializationState", x => x.Id);
                    table.CheckConstraint("CK_TestimonialInitializationState_Singleton", "Id = 1");
                });

            // The previous schema used AUTOINCREMENT IDs. SQLite retains its high-water mark
            // after DELETE, so an initialized-but-now-empty legacy collection can be recognized
            // without guessing from its current row count. Fresh/never-inserted tables have no history.
            migrationBuilder.Sql("""
                INSERT INTO "TestimonialInitializationState" ("Id")
                SELECT 1
                WHERE EXISTS (SELECT 1 FROM "Testimonials")
                   OR EXISTS (SELECT 1 FROM "sqlite_sequence" WHERE "name" = 'Testimonials' AND "seq" > 0);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TestimonialInitializationState");
        }
    }
}
