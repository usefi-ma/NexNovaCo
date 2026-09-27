using Microsoft.EntityFrameworkCore;

namespace NexNovaCo.Web.Data;

internal static class DetailTemplateModelConfiguration
{
    public static void Configure(ModelBuilder builder)
    {
        var project = builder.Entity<ProjectDetailTemplateSettings>();
        project.ToTable("ProjectDetailTemplateSettings", table => table.HasCheckConstraint("CK_ProjectDetailTemplateSettings_Singleton", "Id = 1"));
        project.HasKey(x => x.Id);
        project.Property(x => x.Id).ValueGeneratedNever();
        project.Property(x => x.UpdatedAtUtc).IsConcurrencyToken();
        project.Property(x => x.HeroCtaText).IsRequired().HasMaxLength(60);
        project.Property(x => x.BreadcrumbHomeLabel).IsRequired().HasMaxLength(60);
        project.Property(x => x.BreadcrumbSectionLabel).IsRequired().HasMaxLength(60);
        project.Property(x => x.ProjectDetailsHeading).IsRequired().HasMaxLength(120);
        project.Property(x => x.FeaturesHeading).IsRequired().HasMaxLength(120);
        project.Property(x => x.ClientLabel).IsRequired().HasMaxLength(60);
        project.Property(x => x.CategoryLabel).IsRequired().HasMaxLength(60);
        project.Property(x => x.DateLabel).IsRequired().HasMaxLength(60);
        project.Property(x => x.TechnologiesLabel).IsRequired().HasMaxLength(60);
        project.Property(x => x.ReturnCtaText).IsRequired().HasMaxLength(60);
        var member = builder.Entity<MemberDetailTemplateSettings>();
        member.ToTable("MemberDetailTemplateSettings", table => table.HasCheckConstraint("CK_MemberDetailTemplateSettings_Singleton", "Id = 1"));
        member.HasKey(x => x.Id);
        member.Property(x => x.Id).ValueGeneratedNever();
        member.Property(x => x.UpdatedAtUtc).IsConcurrencyToken();
        member.Property(x => x.BreadcrumbHomeLabel).IsRequired().HasMaxLength(60);
        member.Property(x => x.BreadcrumbSectionLabel).IsRequired().HasMaxLength(60);
        member.Property(x => x.SkillsHeading).IsRequired().HasMaxLength(120);
    }
}
