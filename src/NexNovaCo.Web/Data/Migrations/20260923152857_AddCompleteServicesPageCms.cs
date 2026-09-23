using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexNovaCo.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCompleteServicesPageCms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ServiceBenefitInitializationState",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    InitializedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceBenefitInitializationState", x => x.Id);
                    table.CheckConstraint("CK_ServiceBenefitInitializationState_Singleton", "Id = 1");
                });

            migrationBuilder.CreateTable(
                name: "ServiceBenefits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 600, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceBenefits", x => x.Id);
                    table.CheckConstraint("CK_ServiceBenefits_Order", "DisplayOrder > 0");
                });

            migrationBuilder.CreateTable(
                name: "ServiceFaqInitializationState",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    InitializedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceFaqInitializationState", x => x.Id);
                    table.CheckConstraint("CK_ServiceFaqInitializationState_Singleton", "Id = 1");
                });

            migrationBuilder.CreateTable(
                name: "ServiceFaqItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Question = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Answer = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceFaqItems", x => x.Id);
                    table.CheckConstraint("CK_ServiceFaqItems_Order", "DisplayOrder > 0");
                });

            migrationBuilder.CreateTable(
                name: "ServicePricingInitializationState",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    InitializedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicePricingInitializationState", x => x.Id);
                    table.CheckConstraint("CK_ServicePricingInitializationState_Singleton", "Id = 1");
                });

            migrationBuilder.CreateTable(
                name: "ServicePricingPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    Subtitle = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DisplayPrice = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    IdealFor = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CtaLabel = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    Treatment = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicePricingPlans", x => x.Id);
                    table.CheckConstraint("CK_ServicePricingPlans_Order", "DisplayOrder > 0");
                    table.CheckConstraint("CK_ServicePricingPlans_Treatment", "Treatment BETWEEN 0 AND 2");
                });

            migrationBuilder.CreateTable(
                name: "ServiceProcessInitializationState",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    InitializedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceProcessInitializationState", x => x.Id);
                    table.CheckConstraint("CK_ServiceProcessInitializationState_Singleton", "Id = 1");
                });

            migrationBuilder.CreateTable(
                name: "ServiceProcessSteps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    Icon = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceProcessSteps", x => x.Id);
                    table.CheckConstraint("CK_ServiceProcessSteps_Icon", "Icon BETWEEN 0 AND 4");
                    table.CheckConstraint("CK_ServiceProcessSteps_Order", "DisplayOrder > 0");
                });

            migrationBuilder.CreateTable(
                name: "ServicesBenefitsSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    ImagePath = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicesBenefitsSettings", x => x.Id);
                    table.CheckConstraint("CK_ServicesBenefitsSettings_Singleton", "Id = 1");
                });

            migrationBuilder.CreateTable(
                name: "ServicesFaqSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicesFaqSettings", x => x.Id);
                    table.CheckConstraint("CK_ServicesFaqSettings_Singleton", "Id = 1");
                });

            migrationBuilder.CreateTable(
                name: "ServicesHeroSettings",
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
                    table.PrimaryKey("PK_ServicesHeroSettings", x => x.Id);
                    table.CheckConstraint("CK_ServicesHeroSettings_Singleton", "Id = 1");
                });

            migrationBuilder.CreateTable(
                name: "ServicesPricingSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicesPricingSettings", x => x.Id);
                    table.CheckConstraint("CK_ServicesPricingSettings_Singleton", "Id = 1");
                });

            migrationBuilder.CreateTable(
                name: "ServicesProcessSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicesProcessSettings", x => x.Id);
                    table.CheckConstraint("CK_ServicesProcessSettings_Singleton", "Id = 1");
                });

            migrationBuilder.CreateTable(
                name: "ServicePricingPlanFeatures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlanId = table.Column<int>(type: "INTEGER", nullable: false),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    Text = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicePricingPlanFeatures", x => x.Id);
                    table.CheckConstraint("CK_ServicePricingPlanFeatures_Order", "DisplayOrder > 0");
                    table.ForeignKey(
                        name: "FK_ServicePricingPlanFeatures_ServicePricingPlans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "ServicePricingPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceBenefits_DisplayOrder",
                table: "ServiceBenefits",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceFaqItems_DisplayOrder",
                table: "ServiceFaqItems",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_ServicePricingPlanFeatures_PlanId_DisplayOrder",
                table: "ServicePricingPlanFeatures",
                columns: new[] { "PlanId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ServicePricingPlans_DisplayOrder",
                table: "ServicePricingPlans",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_ServicePricingPlans_Treatment",
                table: "ServicePricingPlans",
                column: "Treatment",
                unique: true,
                filter: "Treatment = 2");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceProcessSteps_DisplayOrder",
                table: "ServiceProcessSteps",
                column: "DisplayOrder");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServiceBenefitInitializationState");

            migrationBuilder.DropTable(
                name: "ServiceBenefits");

            migrationBuilder.DropTable(
                name: "ServiceFaqInitializationState");

            migrationBuilder.DropTable(
                name: "ServiceFaqItems");

            migrationBuilder.DropTable(
                name: "ServicePricingInitializationState");

            migrationBuilder.DropTable(
                name: "ServicePricingPlanFeatures");

            migrationBuilder.DropTable(
                name: "ServiceProcessInitializationState");

            migrationBuilder.DropTable(
                name: "ServiceProcessSteps");

            migrationBuilder.DropTable(
                name: "ServicesBenefitsSettings");

            migrationBuilder.DropTable(
                name: "ServicesFaqSettings");

            migrationBuilder.DropTable(
                name: "ServicesHeroSettings");

            migrationBuilder.DropTable(
                name: "ServicesPricingSettings");

            migrationBuilder.DropTable(
                name: "ServicesProcessSettings");

            migrationBuilder.DropTable(
                name: "ServicePricingPlans");
        }
    }
}
