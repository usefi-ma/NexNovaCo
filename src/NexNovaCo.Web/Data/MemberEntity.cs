using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Data;

public sealed class MemberEntity
{
    public int Id { get; set; }
    public int DisplayOrder { get; set; }
    public string Slug { get; set; } = "";
    public string Name { get; set; } = "";
    public string Role { get; set; } = "";
    public string Introduction { get; set; } = "";
    public string Biography { get; set; } = "";
    public string ImagePath { get; set; } = "";
    public string? Email { get; set; }
    public string? LinkedIn { get; set; }
    public string? Telegram { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public List<MemberSkill> Skills { get; set; } = [];
    public HomeFeaturedMember? HomeFeatured { get; set; }
    public TeamMemberSummary ToContent() => new(Slug, Name, Role, Introduction, ImagePath, Email, LinkedIn, Telegram);
    public MemberDetail ToDetail() => new(ToContent(), Biography,
        Skills.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Id).Select(x => x.Text).ToArray());
    public MemberEditModel ToEditModel() => MemberEditModel.FromContent(ToDetail());
    public void SetContent(MemberEditModel model)
    {
        Slug = MemberSlugs.Normalize(model.Slug); Name = model.Name.Trim(); Role = model.Role.Trim();
        Introduction = model.Introduction.Trim(); Biography = model.Biography.Trim(); ImagePath = model.ImagePath;
        Email = model.Email; LinkedIn = model.LinkedIn; Telegram = model.Telegram;
        Skills.Clear();
        Skills.AddRange(model.Skills.Select((x, i) => new MemberSkill { Text = x.Text.Trim(), DisplayOrder = i + 1 }));
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
public sealed class MemberSkill
{
    public int Id { get; set; }
    public int MemberId { get; set; }
    public int DisplayOrder { get; set; }
    public string Text { get; set; } = "";
}
public sealed class HomeFeaturedMember
{
    public int MemberId { get; set; }
    public int DisplayOrder { get; set; }
    public MemberEntity Member { get; set; } = null!;
}
public sealed class MemberInitializationState { public int Id { get; set; } = 1; }
