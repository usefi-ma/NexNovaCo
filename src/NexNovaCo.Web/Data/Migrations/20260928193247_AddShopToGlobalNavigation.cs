using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexNovaCo.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddShopToGlobalNavigation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Upgrade only databases whose canonical navigation was initialized before Shop
            // existed. Fresh databases are still seeded in full by GlobalSiteInitializer.
            migrationBuilder.Sql(
                """
                UPDATE NavigationItems
                SET DisplayOrder = DisplayOrder + 1
                WHERE EXISTS (SELECT 1 FROM NavigationInitializationState WHERE Id = 1)
                  AND NOT EXISTS (
                      SELECT 1 FROM NavigationItems
                      WHERE lower(trim(trim(Url), '/')) = 'shop')
                  AND DisplayOrder >= (
                      SELECT MIN(DisplayOrder) FROM NavigationItems
                      WHERE lower(trim(trim(Url), '/')) = 'contact');

                INSERT INTO NavigationItems (DisplayOrder, Label, Url, UpdatedAtUtc)
                SELECT
                    CASE
                        WHEN EXISTS (
                            SELECT 1 FROM NavigationItems
                            WHERE lower(trim(trim(Url), '/')) = 'contact')
                        THEN (
                            SELECT MIN(DisplayOrder) - 1 FROM NavigationItems
                            WHERE lower(trim(trim(Url), '/')) = 'contact')
                        ELSE COALESCE((SELECT MAX(DisplayOrder) FROM NavigationItems), 0) + 1
                    END,
                    'Shop',
                    'shop',
                    strftime('%Y-%m-%d %H:%M:%f0000', 'now')
                WHERE EXISTS (SELECT 1 FROM NavigationInitializationState WHERE Id = 1)
                  AND NOT EXISTS (
                      SELECT 1 FROM NavigationItems
                      WHERE lower(trim(trim(Url), '/')) = 'shop');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Navigation rows are Admin-owned content. A rollback must not delete a link that
            // may have been edited or intentionally created after this one-time data upgrade.
        }
    }
}
