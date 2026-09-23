using NexNovaCo.Web.Data;
using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Services;

public interface IAboutCmsService
{
    Task<AboutContent> ReadPublicAsync(CancellationToken ct = default);
    Task<AboutHeroEditModel> GetHeroForEditAsync(CancellationToken ct = default);
    Task SaveHeroAsync(AboutHeroEditModel model, CancellationToken ct = default);
    Task<AboutStoryEditModel> GetStoryForEditAsync(CancellationToken ct = default);
    Task SaveStoryAsync(AboutStoryEditModel model, CancellationToken ct = default);
    Task<AboutVisionEditModel> GetVisionForEditAsync(CancellationToken ct = default);
    Task SaveVisionAsync(AboutVisionEditModel model, CancellationToken ct = default);
    Task<AboutTimelineEditModel> GetTimelineForEditAsync(CancellationToken ct = default);
    Task SaveTimelineAsync(AboutTimelineEditModel model, CancellationToken ct = default);
    Task<AboutMissionEditModel> GetMissionForEditAsync(CancellationToken ct = default);
    Task SaveMissionAsync(AboutMissionEditModel model, CancellationToken ct = default);
    Task<AboutPartnersEditModel> GetPartnersForEditAsync(CancellationToken ct = default);
    Task SavePartnersAsync(AboutPartnersEditModel model, CancellationToken ct = default);
    Task<IReadOnlyList<AboutTimelineItem>> ListTimelineAsync(CancellationToken ct = default);
    Task<AboutTimelineItemEditModel> GetTimelineItemForEditAsync(int id, CancellationToken ct = default);
    Task<int> CreateTimelineAsync(AboutTimelineItemEditModel model, CancellationToken ct = default);
    Task UpdateTimelineAsync(int id, AboutTimelineItemEditModel model, CancellationToken ct = default);
    Task DeleteTimelineAsync(int id, CancellationToken ct = default);
    Task ReorderTimelineAsync(IReadOnlyList<int> orderedIds, CancellationToken ct = default);
    Task<IReadOnlyList<AboutMissionPointItem>> ListMissionPointAsync(CancellationToken ct = default);
    Task<AboutMissionPointEditModel> GetMissionPointItemForEditAsync(int id, CancellationToken ct = default);
    Task<int> CreateMissionPointAsync(AboutMissionPointEditModel model, CancellationToken ct = default);
    Task UpdateMissionPointAsync(int id, AboutMissionPointEditModel model, CancellationToken ct = default);
    Task DeleteMissionPointAsync(int id, CancellationToken ct = default);
    Task ReorderMissionPointAsync(IReadOnlyList<int> orderedIds, CancellationToken ct = default);
}
