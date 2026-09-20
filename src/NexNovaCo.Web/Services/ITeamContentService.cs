using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Services;

public interface ITeamContentService
{
    Task<TeamContent> GetAsync(CancellationToken cancellationToken = default);
}
