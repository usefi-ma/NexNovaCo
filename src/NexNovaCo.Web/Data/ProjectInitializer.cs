using Microsoft.EntityFrameworkCore;
using NexNovaCo.Web.Models;
using NexNovaCo.Web.Services;

namespace NexNovaCo.Web.Data;

public static class ProjectInitializer
{
    public static async Task InitializeAsync(ApplicationDbContext database, ProjectCatalog defaults, CancellationToken cancellationToken = default)
    {
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        if (await database.ProjectInitializationStates.AnyAsync(cancellationToken)) return;
        if (!await database.Projects.AnyAsync(cancellationToken))
        {
            var order = 0;
            foreach (var content in await defaults.GetDetailsAsync(cancellationToken))
            {
                var entity = new ProjectEntity { DisplayOrder = ++order };
                entity.SetContent(ProjectEditModel.FromContent(content));
                database.Projects.Add(entity);
                var featuredOrder = ProjectCatalog.HomeFeaturedSlugs.ToList().IndexOf(content.Summary.Slug);
                if (featuredOrder >= 0)
                    database.HomeFeaturedProjects.Add(new HomeFeaturedProject { Project = entity, DisplayOrder = featuredOrder + 1 });
            }
        }
        database.ProjectInitializationStates.Add(new ProjectInitializationState());
        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
