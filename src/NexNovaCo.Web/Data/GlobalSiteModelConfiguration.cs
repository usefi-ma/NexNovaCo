using Microsoft.EntityFrameworkCore;

namespace NexNovaCo.Web.Data;

public static class GlobalSiteModelConfiguration
{
    public static void Configure(ModelBuilder builder)
    {
        var siteidentity = builder.Entity<SiteIdentitySettings>();
        siteidentity.ToTable("SiteIdentitySettings", table => table.HasCheckConstraint("CK_SiteIdentitySettings_Singleton", "Id = 1"));
        siteidentity.HasKey(x => x.Id);
        siteidentity.Property(x => x.Id).ValueGeneratedNever();
        siteidentity.Property(x => x.SiteName).IsRequired().HasMaxLength(80);
        siteidentity.Property(x => x.ImagePath).IsRequired().HasMaxLength(200);
        var footer = builder.Entity<FooterSettings>();
        footer.ToTable("FooterSettings", table => table.HasCheckConstraint("CK_FooterSettings_Singleton", "Id = 1"));
        footer.HasKey(x => x.Id);
        footer.Property(x => x.Id).ValueGeneratedNever();
        footer.Property(x => x.Description).IsRequired().HasMaxLength(1000);
        footer.Property(x => x.Copyright).IsRequired().HasMaxLength(200);
        footer.Property(x => x.NewsletterHeading).IsRequired().HasMaxLength(120);
        footer.Property(x => x.NewsletterPlaceholder).IsRequired().HasMaxLength(160);
        footer.Property(x => x.NewsletterSubmitLabel).IsRequired().HasMaxLength(60);
        var navigation = builder.Entity<NavigationItem>();
        navigation.ToTable("NavigationItems", table => table.HasCheckConstraint("CK_NavigationItems_Order", "DisplayOrder > 0"));
        navigation.HasKey(x => x.Id);
        navigation.HasIndex(x => x.DisplayOrder);
        navigation.Property(x => x.Label).IsRequired().HasMaxLength(40);
        navigation.Property(x => x.Url).IsRequired().HasMaxLength(200);
        navigation.Property(x => x.UpdatedAtUtc).IsConcurrencyToken();
        var navigationState = builder.Entity<NavigationInitializationState>();
        navigationState.ToTable("NavigationInitializationState", table => table.HasCheckConstraint("CK_NavigationInitializationState_Singleton", "Id = 1"));
        navigationState.HasKey(x => x.Id);
        navigationState.Property(x => x.Id).ValueGeneratedNever();
        var sociallink = builder.Entity<SocialLinkItem>();
        sociallink.ToTable("SocialLinkItems", table => table.HasCheckConstraint("CK_SocialLinkItems_Order", "DisplayOrder > 0"));
        sociallink.HasKey(x => x.Id);
        sociallink.HasIndex(x => x.DisplayOrder);
        sociallink.HasIndex(x => x.Platform).IsUnique();
        sociallink.Property(x => x.Url).IsRequired().HasMaxLength(500);
        sociallink.Property(x => x.UpdatedAtUtc).IsConcurrencyToken();
        var sociallinkState = builder.Entity<SocialLinkInitializationState>();
        sociallinkState.ToTable("SocialLinkInitializationState", table => table.HasCheckConstraint("CK_SocialLinkInitializationState_Singleton", "Id = 1"));
        sociallinkState.HasKey(x => x.Id);
        sociallinkState.Property(x => x.Id).ValueGeneratedNever();
    }
}
