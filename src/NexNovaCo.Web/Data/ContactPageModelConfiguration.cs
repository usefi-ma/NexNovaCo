using Microsoft.EntityFrameworkCore;

namespace NexNovaCo.Web.Data;

internal static class ContactPageModelConfiguration
{
    public static void Configure(ModelBuilder builder)
    {
        var ContactPageSettings = builder.Entity<ContactPageSettings>();
        ContactPageSettings.ToTable("ContactPageSettings", table => table.HasCheckConstraint("CK_ContactPageSettings_Singleton", "Id = 1"));
        ContactPageSettings.HasKey(x => x.Id);
        ContactPageSettings.Property(x => x.Id).ValueGeneratedNever();
        ContactPageSettings.Property(x => x.UpdatedAtUtc).IsConcurrencyToken();
        ContactPageSettings.Property(x => x.HeroTitle).IsRequired().HasMaxLength(80);
        ContactPageSettings.Property(x => x.HeroDescription).IsRequired().HasMaxLength(600);
        ContactPageSettings.Property(x => x.HeroCtaLabel).IsRequired().HasMaxLength(60);
        ContactPageSettings.Property(x => x.HeroCtaHref).IsRequired().HasMaxLength(200);
        ContactPageSettings.Property(x => x.HeroImagePath).IsRequired().HasMaxLength(200);
        ContactPageSettings.Property(x => x.InfoHeading).IsRequired().HasMaxLength(120);
        ContactPageSettings.Property(x => x.MapHeading).IsRequired().HasMaxLength(120);
        ContactPageSettings.Property(x => x.MapDescription).IsRequired().HasMaxLength(400);
        ContactPageSettings.Property(x => x.MapEmbedUrl).IsRequired().HasMaxLength(2048);
        ContactPageSettings.Property(x => x.MapTitle).IsRequired().HasMaxLength(200);
        var ContactFormSettings = builder.Entity<ContactFormSettings>();
        ContactFormSettings.ToTable("ContactFormSettings", table => table.HasCheckConstraint("CK_ContactFormSettings_Singleton", "Id = 1"));
        ContactFormSettings.HasKey(x => x.Id);
        ContactFormSettings.Property(x => x.Id).ValueGeneratedNever();
        ContactFormSettings.Property(x => x.UpdatedAtUtc).IsConcurrencyToken();
        ContactFormSettings.Property(x => x.Heading).IsRequired().HasMaxLength(120);
        ContactFormSettings.Property(x => x.FirstNameLabel).IsRequired().HasMaxLength(80);
        ContactFormSettings.Property(x => x.FirstNamePlaceholder).IsRequired().HasMaxLength(160);
        ContactFormSettings.Property(x => x.LastNameLabel).IsRequired().HasMaxLength(80);
        ContactFormSettings.Property(x => x.LastNamePlaceholder).IsRequired().HasMaxLength(160);
        ContactFormSettings.Property(x => x.EmailLabel).IsRequired().HasMaxLength(80);
        ContactFormSettings.Property(x => x.EmailPlaceholder).IsRequired().HasMaxLength(160);
        ContactFormSettings.Property(x => x.SubjectLabel).IsRequired().HasMaxLength(80);
        ContactFormSettings.Property(x => x.SubjectPlaceholder).IsRequired().HasMaxLength(160);
        ContactFormSettings.Property(x => x.MessageLabel).IsRequired().HasMaxLength(80);
        ContactFormSettings.Property(x => x.MessagePlaceholder).IsRequired().HasMaxLength(160);
        ContactFormSettings.Property(x => x.SubmitLabel).IsRequired().HasMaxLength(60);
        ContactFormSettings.Property(x => x.InvalidMessage).IsRequired().HasMaxLength(300);
        var SiteContactSettings = builder.Entity<SiteContactSettings>();
        SiteContactSettings.ToTable("SiteContactSettings", table => table.HasCheckConstraint("CK_SiteContactSettings_Singleton", "Id = 1"));
        SiteContactSettings.HasKey(x => x.Id);
        SiteContactSettings.Property(x => x.Id).ValueGeneratedNever();
        SiteContactSettings.Property(x => x.UpdatedAtUtc).IsConcurrencyToken();
        SiteContactSettings.Property(x => x.Phone).IsRequired().HasMaxLength(80);
        SiteContactSettings.Property(x => x.Email).IsRequired().HasMaxLength(254);
        SiteContactSettings.Property(x => x.Address).IsRequired().HasMaxLength(500);
    }
}
