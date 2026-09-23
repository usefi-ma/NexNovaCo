using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace NexNovaCo.Web.Models;

public sealed class ContactHeroEditModel
{
    [Required, StringLength(80)]
    public string Title { get; set; } = "";
    [Required, StringLength(600)]
    public string Description { get; set; } = "";
    [Required, StringLength(60)]
    public string CtaLabel { get; set; } = "";
    [Required, StringLength(200), ContactPageCtaRoute]
    public string CtaHref { get; set; } = "";
    [Required, StringLength(200), HomeImagePath(MediaKind.ContactHero)]
    public string ImagePath { get; set; } = "";
    public static ContactHeroEditModel Approved()
    {
        var c = ContactPageDefaults.Content;
        return new() { Title = c.Hero.Title, Description = c.Hero.Description!, CtaLabel = c.Hero.CtaLabel, CtaHref = c.Hero.CtaHref, ImagePath = MediaPolicy.ContactHeroDefault };
    }
}

public sealed class ContactInfoEditModel
{
    [Required, StringLength(120)]
    public string Heading { get; set; } = "";
    [Required, StringLength(80)]
    public string Phone { get; set; } = "";
    [Required, StringLength(254), EmailAddress]
    public string Email { get; set; } = "";
    [Required, StringLength(500)]
    public string Address { get; set; } = "";
    public static ContactInfoEditModel Approved()
    {
        var c = ContactPageDefaults.Content;
        return new() { Heading = c.Info.Heading, Phone = c.Info.Phone, Email = c.Info.Email, Address = c.Info.Address };
    }
}

public sealed class ContactFormEditModel
{
    [Required, StringLength(120)]
    public string Heading { get; set; } = "";
    [Required, StringLength(80)]
    public string FirstNameLabel { get; set; } = "";
    [Required, StringLength(160)]
    public string FirstNamePlaceholder { get; set; } = "";
    [Required, StringLength(80)]
    public string LastNameLabel { get; set; } = "";
    [Required, StringLength(160)]
    public string LastNamePlaceholder { get; set; } = "";
    [Required, StringLength(80)]
    public string EmailLabel { get; set; } = "";
    [Required, StringLength(160)]
    public string EmailPlaceholder { get; set; } = "";
    [Required, StringLength(80)]
    public string SubjectLabel { get; set; } = "";
    [Required, StringLength(160)]
    public string SubjectPlaceholder { get; set; } = "";
    [Required, StringLength(80)]
    public string MessageLabel { get; set; } = "";
    [Required, StringLength(160)]
    public string MessagePlaceholder { get; set; } = "";
    [Required, StringLength(60)]
    public string SubmitLabel { get; set; } = "";
    [Required, StringLength(300)]
    public string InvalidMessage { get; set; } = "";
    public static ContactFormEditModel Approved()
    {
        var c = ContactPageDefaults.Content;
        return new() { Heading = c.Form.Heading, FirstNameLabel = c.Form.FirstName.Label, FirstNamePlaceholder = c.Form.FirstName.Placeholder, LastNameLabel = c.Form.LastName.Label, LastNamePlaceholder = c.Form.LastName.Placeholder, EmailLabel = c.Form.Email.Label, EmailPlaceholder = c.Form.Email.Placeholder, SubjectLabel = c.Form.Subject.Label, SubjectPlaceholder = c.Form.Subject.Placeholder, MessageLabel = c.Form.Message.Label, MessagePlaceholder = c.Form.Message.Placeholder, SubmitLabel = c.Form.SubmitLabel, InvalidMessage = c.Form.InvalidMessage };
    }
}

public sealed class ContactMapEditModel
{
    [Required, StringLength(120)]
    public string Heading { get; set; } = "";
    [Required, StringLength(400)]
    public string Description { get; set; } = "";
    [Required, StringLength(2048), ContactMapUrl]
    public string EmbedUrl { get; set; } = "";
    [Required, StringLength(200)]
    public string Title { get; set; } = "";
    public static ContactMapEditModel Approved()
    {
        var c = ContactPageDefaults.Content;
        return new() { Heading = c.Map.Heading, Description = c.Map.Description, EmbedUrl = c.Map.EmbedUrl.AbsoluteUri, Title = c.Map.Title };
    }
}

public sealed class ContactPageCtaRouteAttribute : ValidationAttribute
{
    public ContactPageCtaRouteAttribute() => ErrorMessage = "Use a public site route, or /contact#Contact.";
    public override bool IsValid(object? value) => value is string path && Regex.IsMatch(path,
        @"\A(?:/|/?(?:about|services|contact(?:#Contact)?|projects(?:/[a-z0-9]+(?:-[a-z0-9]+)*)?|team(?:/[a-z0-9]+(?:-[a-z0-9]+)*)?))\z", RegexOptions.CultureInvariant);
}

public sealed class ContactMapUrlAttribute : ValidationAttribute
{
    public ContactMapUrlAttribute() => ErrorMessage = "Use a Google Maps HTTPS embed URL (https://www.google.com/maps/embed?...).";
    public override bool IsValid(object? value) => value is string url && url.Length <= 2048 &&
        !url.Any(c => char.IsWhiteSpace(c) || char.IsControl(c) || c is '<' or '>' or '"' or '\\') &&
        Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps &&
        uri.Host.Equals("www.google.com", StringComparison.OrdinalIgnoreCase) && uri.IsDefaultPort &&
        uri.UserInfo.Length == 0 && uri.AbsolutePath == "/maps/embed" &&
        uri.Fragment.Length == 0 && uri.Query.Length > 1;
}
