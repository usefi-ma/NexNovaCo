namespace NexNovaCo.Web.Models;

public sealed record ContactInfoContent(string Heading, string Phone, string Email, string Address);
public sealed record ContactFieldContent(string Label, string Placeholder);
public sealed record ContactFormContent(string Heading, ContactFieldContent FirstName,
    ContactFieldContent LastName, ContactFieldContent Email, ContactFieldContent Subject,
    ContactFieldContent Message, string SubmitLabel, string InvalidMessage);
public sealed record ContactMapContent(string Heading, string Description, Uri EmbedUrl, string Title);
public sealed record ContactContent(InnerPageHeroContent Hero, ContactInfoContent Info,
    ContactFormContent Form, ContactMapContent Map);
