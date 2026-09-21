using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace NexNovaCo.Web.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<HomeHeroSettings> HomeHeroSettings => Set<HomeHeroSettings>();

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
    }
}
