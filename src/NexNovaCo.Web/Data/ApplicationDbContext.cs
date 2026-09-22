using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace NexNovaCo.Web.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<HomeHeroSettings> HomeHeroSettings => Set<HomeHeroSettings>();
    public DbSet<HomeWelcomeSettings> HomeWelcomeSettings => Set<HomeWelcomeSettings>();
    public DbSet<HomeServicesSectionSettings> HomeServicesSectionSettings => Set<HomeServicesSectionSettings>();

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
    }
}
