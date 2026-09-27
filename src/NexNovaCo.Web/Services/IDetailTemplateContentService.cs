using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Services;

public interface IDetailTemplateContentService
{
    Task<ProjectDetailTemplateEditModel> ReadProjectAsync(CancellationToken ct = default);
    Task<ProjectDetailTemplateEditModel> GetProjectForEditAsync(CancellationToken ct = default);
    Task SaveProjectAsync(ProjectDetailTemplateEditModel model, CancellationToken ct = default);
    Task<MemberDetailTemplateEditModel> ReadMemberAsync(CancellationToken ct = default);
    Task<MemberDetailTemplateEditModel> GetMemberForEditAsync(CancellationToken ct = default);
    Task SaveMemberAsync(MemberDetailTemplateEditModel model, CancellationToken ct = default);
}
