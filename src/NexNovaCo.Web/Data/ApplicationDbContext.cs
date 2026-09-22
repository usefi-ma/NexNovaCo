using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace NexNovaCo.Web.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<HomeHeroSettings> HomeHeroSettings => Set<HomeHeroSettings>();
    public DbSet<HomeWelcomeSettings> HomeWelcomeSettings => Set<HomeWelcomeSettings>();
    public DbSet<HomeServicesSectionSettings> HomeServicesSectionSettings => Set<HomeServicesSectionSettings>();
    public DbSet<HomeProjectsSectionSettings> HomeProjectsSectionSettings => Set<HomeProjectsSectionSettings>();
    public DbSet<HomeTeamSectionSettings> HomeTeamSectionSettings => Set<HomeTeamSectionSettings>();
    public DbSet<HomeStatistic> HomeStatistics => Set<HomeStatistic>();
    public DbSet<HomePartnersSectionSettings> HomePartnersSectionSettings => Set<HomePartnersSectionSettings>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        var hero = builder.Entity<HomeHeroSettings>();
        hero.ToTable("HomeHeroSettings", table => table.HasCheckConstraint("CK_HomeHeroSettings_Singleton", "Id = 1"));
        hero.HasKey(x => x.Id);
        hero.Property(x => x.Id).ValueGeneratedNever();
        hero.Property(x => x.OpeningLine).IsRequired().HasMaxLength(60);
        hero.Property(x => x.EmphasisLine).IsRequired().HasMaxLength(60);
        hero.Property(x => x.ClosingLine).IsRequired().HasMaxLength(60);
        hero.Property(x => x.Description).IsRequired().HasMaxLength(500);
        hero.Property(x => x.CtaLabel).IsRequired().HasMaxLength(60);
        hero.Property(x => x.CtaHref).IsRequired().HasMaxLength(200);
        var welcome = builder.Entity<HomeWelcomeSettings>();
        welcome.ToTable("HomeWelcomeSettings", table => table.HasCheckConstraint("CK_HomeWelcomeSettings_Singleton", "Id = 1"));
        welcome.HasKey(x => x.Id);
        welcome.Property(x => x.Id).ValueGeneratedNever();
        welcome.Property(x => x.Title).IsRequired().HasMaxLength(120);
        welcome.Property(x => x.Introduction).IsRequired().HasMaxLength(500);
        welcome.Property(x => x.ParagraphOne).IsRequired().HasMaxLength(1000);
        welcome.Property(x => x.ParagraphTwo).IsRequired().HasMaxLength(1000);
        welcome.Property(x => x.CtaLabel).IsRequired().HasMaxLength(60);
        welcome.Property(x => x.CtaHref).IsRequired().HasMaxLength(200);
        var services = builder.Entity<HomeServicesSectionSettings>();
        services.ToTable("HomeServicesSectionSettings", table => table.HasCheckConstraint("CK_HomeServicesSectionSettings_Singleton", "Id = 1"));
        services.HasKey(x => x.Id);
        services.Property(x => x.Id).ValueGeneratedNever();
        services.Property(x => x.Title).IsRequired().HasMaxLength(120);
        services.Property(x => x.Description).IsRequired().HasMaxLength(1000);
        var projects = builder.Entity<HomeProjectsSectionSettings>();
        projects.ToTable("HomeProjectsSectionSettings", table => table.HasCheckConstraint("CK_HomeProjectsSectionSettings_Singleton", "Id = 1"));
        projects.HasKey(x => x.Id);
        projects.Property(x => x.Id).ValueGeneratedNever();
        projects.Property(x => x.Title).IsRequired().HasMaxLength(120);
        projects.Property(x => x.Description).IsRequired().HasMaxLength(1000);
        var team = builder.Entity<HomeTeamSectionSettings>();
        team.ToTable("HomeTeamSectionSettings", table => table.HasCheckConstraint("CK_HomeTeamSectionSettings_Singleton", "Id = 1"));
        team.HasKey(x => x.Id);
        team.Property(x => x.Id).ValueGeneratedNever();
        team.Property(x => x.Title).IsRequired().HasMaxLength(120);
        team.Property(x => x.Introduction).IsRequired().HasMaxLength(500);
        team.Property(x => x.Highlight).IsRequired().HasMaxLength(300);
        team.Property(x => x.Description).IsRequired().HasMaxLength(1000);
        team.Property(x => x.CtaLabel).IsRequired().HasMaxLength(60);
        team.Property(x => x.CtaHref).IsRequired().HasMaxLength(200);
        var statistics = builder.Entity<HomeStatistic>();
        statistics.ToTable("HomeStatistics", table =>
        {
            table.HasCheckConstraint("CK_HomeStatistics_FixedSlots", "Id BETWEEN 1 AND 4 AND DisplayOrder = Id");
            table.HasCheckConstraint("CK_HomeStatistics_Value", "Value BETWEEN 0 AND 9999");
        });
        statistics.HasKey(x => x.Id);
        statistics.Property(x => x.Id).ValueGeneratedNever();
        statistics.HasIndex(x => x.DisplayOrder).IsUnique();
        statistics.Property(x => x.Label).IsRequired().HasMaxLength(32);
        var partners = builder.Entity<HomePartnersSectionSettings>();
        partners.ToTable("HomePartnersSectionSettings", table => table.HasCheckConstraint("CK_HomePartnersSectionSettings_Singleton", "Id = 1"));
        partners.HasKey(x => x.Id);
        partners.Property(x => x.Id).ValueGeneratedNever();
        partners.Property(x => x.Title).IsRequired().HasMaxLength(120);
        partners.Property(x => x.Description).IsRequired().HasMaxLength(1000);
    }
}
