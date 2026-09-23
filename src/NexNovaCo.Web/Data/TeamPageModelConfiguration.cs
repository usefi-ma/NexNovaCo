using Microsoft.EntityFrameworkCore;

namespace NexNovaCo.Web.Data;

internal static class TeamPageModelConfiguration
{
    public static void Configure(ModelBuilder builder)
    {
        var hero = builder.Entity<TeamHeroSettings>();
        hero.ToTable("TeamHeroSettings", table => table.HasCheckConstraint("CK_TeamHeroSettings_Singleton", "Id = 1"));
        hero.HasKey(x => x.Id);
        hero.Property(x => x.Id).ValueGeneratedNever();
        hero.Property(x => x.UpdatedAtUtc).IsConcurrencyToken();
        hero.Property(x => x.Title).IsRequired().HasMaxLength(80);
        hero.Property(x => x.Description).IsRequired().HasMaxLength(600);
        hero.Property(x => x.CtaLabel).IsRequired().HasMaxLength(60);
        hero.Property(x => x.CtaHref).IsRequired().HasMaxLength(200);
        hero.Property(x => x.ImagePath).IsRequired().HasMaxLength(200);
        var section = builder.Entity<TeamSectionSettings>();
        section.ToTable("TeamSectionSettings", table => table.HasCheckConstraint("CK_TeamSectionSettings_Singleton", "Id = 1"));
        section.HasKey(x => x.Id);
        section.Property(x => x.Id).ValueGeneratedNever();
        section.Property(x => x.UpdatedAtUtc).IsConcurrencyToken();
        section.Property(x => x.Eyebrow).IsRequired().HasMaxLength(80);
        section.Property(x => x.Title).IsRequired().HasMaxLength(120);
        section.Property(x => x.Introduction).IsRequired().HasMaxLength(500);
        section.Property(x => x.Highlight).IsRequired().HasMaxLength(300);
        section.Property(x => x.Description).IsRequired().HasMaxLength(1000);
        section.Property(x => x.CtaLabel).IsRequired().HasMaxLength(60);
        section.Property(x => x.CtaHref).IsRequired().HasMaxLength(200);
        section.Property(x => x.ImagePath).IsRequired().HasMaxLength(200);
    }
}
