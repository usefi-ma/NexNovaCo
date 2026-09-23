using Microsoft.EntityFrameworkCore;

namespace NexNovaCo.Web.Data;

internal static class ProjectsPageModelConfiguration
{
    public static void Configure(ModelBuilder builder)
    {
        var hero = builder.Entity<ProjectsHeroSettings>();
        hero.ToTable("ProjectsHeroSettings", table => table.HasCheckConstraint("CK_ProjectsHeroSettings_Singleton", "Id = 1"));
        hero.HasKey(x => x.Id);
        hero.Property(x => x.Id).ValueGeneratedNever();
        hero.Property(x => x.UpdatedAtUtc).IsConcurrencyToken();
        hero.Property(x => x.Title).IsRequired().HasMaxLength(80);
        hero.Property(x => x.MobileTitle).IsRequired().HasMaxLength(60);
        hero.Property(x => x.Description).IsRequired().HasMaxLength(600);
        hero.Property(x => x.CtaLabel).IsRequired().HasMaxLength(60);
        hero.Property(x => x.CtaHref).IsRequired().HasMaxLength(200);
        hero.Property(x => x.ImagePath).IsRequired().HasMaxLength(200);
        var testimonials = builder.Entity<ProjectsTestimonialsSettings>();
        testimonials.ToTable("ProjectsTestimonialsSettings", table => table.HasCheckConstraint("CK_ProjectsTestimonialsSettings_Singleton", "Id = 1"));
        testimonials.HasKey(x => x.Id);
        testimonials.Property(x => x.Id).ValueGeneratedNever();
        testimonials.Property(x => x.UpdatedAtUtc).IsConcurrencyToken();
        testimonials.Property(x => x.Title).IsRequired().HasMaxLength(120);
        testimonials.Property(x => x.Description).IsRequired().HasMaxLength(1000);
    }
}
