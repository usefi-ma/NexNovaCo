using Microsoft.EntityFrameworkCore;
using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Data;

public static class ServicesPageInitializer
{
    public static async Task InitializeAsync(ApplicationDbContext db, CancellationToken ct = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        if (!await db.ServicesHeroSettings.AnyAsync(ct))
        {
            var row = new ServicesHeroSettings(); row.SetContent(ServicesHeroEditModel.Approved());
            db.ServicesHeroSettings.Add(row);
        }
        if (!await db.ServicesBenefitsSettings.AnyAsync(ct))
        {
            var row = new ServicesBenefitsSettings(); row.SetContent(ServicesBenefitsEditModel.Approved());
            db.ServicesBenefitsSettings.Add(row);
        }
        if (!await db.ServicesProcessSettings.AnyAsync(ct))
        {
            var row = new ServicesProcessSettings(); row.SetContent(ServicesProcessEditModel.Approved());
            db.ServicesProcessSettings.Add(row);
        }
        if (!await db.ServicesPricingSettings.AnyAsync(ct))
        {
            var row = new ServicesPricingSettings(); row.SetContent(ServicesPricingEditModel.Approved());
            db.ServicesPricingSettings.Add(row);
        }
        if (!await db.ServicesFaqSettings.AnyAsync(ct))
        {
            var row = new ServicesFaqSettings(); row.SetContent(ServicesFaqEditModel.Approved());
            db.ServicesFaqSettings.Add(row);
        }
        if (!await db.ServiceBenefitInitializationStates.AnyAsync(ct))
        {
            // Adopt any existing rows; once marked, an intentionally empty collection stays empty.
            if (!await db.ServiceBenefits.AnyAsync(ct))
            {
                var order = 0;
                foreach (var x in ServicesPageDefaults.Content.Benefits)
                {
                    var row = new ServiceBenefitItem { DisplayOrder = ++order };
                    row.SetContent(new() { Title = x.Title, Description = x.Description });
                    db.ServiceBenefits.Add(row);
                }
            }
            db.ServiceBenefitInitializationStates.Add(new());
        }
        if (!await db.ServiceProcessInitializationStates.AnyAsync(ct))
        {
            // Adopt any existing rows; once marked, an intentionally empty collection stays empty.
            if (!await db.ServiceProcessSteps.AnyAsync(ct))
            {
                var order = 0;
                foreach (var x in ServicesPageDefaults.Content.Steps)
                {
                    var row = new ServiceProcessStepItem { DisplayOrder = ++order };
                    row.SetContent(new() { Title = x.Title, Icon = x.Icon });
                    db.ServiceProcessSteps.Add(row);
                }
            }
            db.ServiceProcessInitializationStates.Add(new());
        }
        if (!await db.ServicePricingInitializationStates.AnyAsync(ct))
        {
            // Adopt any existing rows; once marked, an intentionally empty collection stays empty.
            if (!await db.ServicePricingPlans.AnyAsync(ct))
            {
                var order = 0;
                foreach (var x in ServicesPageDefaults.Content.Plans)
                {
                    var row = new ServicePricingPlanItem { DisplayOrder = ++order };
                    row.SetContent(new() { Name = x.Name, Subtitle = x.Subtitle, DisplayPrice = x.DisplayPrice, IdealFor = x.IdealFor, CtaLabel = x.CtaLabel, Treatment = x.Treatment, Features = x.Features.ToList() });
                    db.ServicePricingPlans.Add(row);
                }
            }
            db.ServicePricingInitializationStates.Add(new());
        }
        if (!await db.ServiceFaqInitializationStates.AnyAsync(ct))
        {
            // Adopt any existing rows; once marked, an intentionally empty collection stays empty.
            if (!await db.ServiceFaqItems.AnyAsync(ct))
            {
                var order = 0;
                foreach (var x in ServicesPageDefaults.Content.Questions)
                {
                    var row = new ServiceFaqItem { DisplayOrder = ++order };
                    row.SetContent(new() { Question = x.Question, Answer = x.Answer });
                    db.ServiceFaqItems.Add(row);
                }
            }
            db.ServiceFaqInitializationStates.Add(new());
        }
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }
}
