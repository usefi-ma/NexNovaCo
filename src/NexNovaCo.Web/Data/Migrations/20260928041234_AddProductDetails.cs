using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexNovaCo.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProductDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FullDescription",
                table: "Products",
                type: "TEXT",
                maxLength: 10000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ProductDetailInitializationState",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductDetailInitializationState", x => x.Id);
                    table.CheckConstraint("CK_ProductDetailInitializationState_Singleton", "Id = 1");
                });

            migrationBuilder.CreateTable(
                name: "ProductFeatures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProductId = table.Column<int>(type: "INTEGER", nullable: false),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    Text = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductFeatures", x => x.Id);
                    table.CheckConstraint("CK_ProductFeatures_Order", "DisplayOrder > 0");
                    table.ForeignKey(
                        name: "FK_ProductFeatures_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductGalleryImages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProductId = table.Column<int>(type: "INTEGER", nullable: false),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    Source = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Alt = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductGalleryImages", x => x.Id);
                    table.CheckConstraint("CK_ProductGalleryImages_Order", "DisplayOrder > 0");
                    table.ForeignKey(
                        name: "FK_ProductGalleryImages_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductRelatedProducts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProductId = table.Column<int>(type: "INTEGER", nullable: false),
                    RelatedProductId = table.Column<int>(type: "INTEGER", nullable: false),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductRelatedProducts", x => x.Id);
                    table.CheckConstraint("CK_ProductRelatedProducts_NoSelf", "ProductId <> RelatedProductId");
                    table.CheckConstraint("CK_ProductRelatedProducts_Order", "DisplayOrder BETWEEN 1 AND 3");
                    table.ForeignKey(
                        name: "FK_ProductRelatedProducts_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductRelatedProducts_Products_RelatedProductId",
                        column: x => x.RelatedProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductFeatures_ProductId_DisplayOrder",
                table: "ProductFeatures",
                columns: new[] { "ProductId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductGalleryImages_ProductId_DisplayOrder",
                table: "ProductGalleryImages",
                columns: new[] { "ProductId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductRelatedProducts_ProductId_DisplayOrder",
                table: "ProductRelatedProducts",
                columns: new[] { "ProductId", "DisplayOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductRelatedProducts_ProductId_RelatedProductId",
                table: "ProductRelatedProducts",
                columns: new[] { "ProductId", "RelatedProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductRelatedProducts_RelatedProductId",
                table: "ProductRelatedProducts",
                column: "RelatedProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductDetailInitializationState");

            migrationBuilder.DropTable(
                name: "ProductFeatures");

            migrationBuilder.DropTable(
                name: "ProductGalleryImages");

            migrationBuilder.DropTable(
                name: "ProductRelatedProducts");

            migrationBuilder.DropColumn(
                name: "FullDescription",
                table: "Products");
        }
    }
}
