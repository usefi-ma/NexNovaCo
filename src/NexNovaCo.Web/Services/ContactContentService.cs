using NexNovaCo.Web.Models;

namespace NexNovaCo.Web.Services;

// Approved contact.html demo content, not verified business contact information.
public sealed class ContactContentService : IContactContentService
{
    private static readonly ContactContent Content = new(
        new("Contact Us", "Contact Us", "Need help or have questions? Our team is ready to support you. Let's connect and create something great together.",
            "Send Us a Message", "contact#Contact"),
        new("Contact Info:", "+1 (587) 555-1234", "info@nexnovaco.com",
            "NexNovaCo Inc. 123 Innovation Drive, Suite 456 Calgary, AB T2P 1A1"),
        new("Get in Touch:", new("First Name", "First Name"), new("Last Name", "Last Name"),
            new("Email", "Email"), new("Subject", "Subject"), new("Message", "Write your message"),
            "Submit", "Please fill out all required fields and enter a valid email address."),
        new("We're Here!", "Come visit us in downtown Calgary",
            new Uri("https://www.google.com/maps/embed?pb=!1m18!1m12!1m3!1d2508.4752813776636!2d-114.06566632294482!3d51.04431134442705!2m3!1f0!2f0!3f0!3m2!1i1024!2i768!4f13.1!3m3!1m2!1s0x53716ffd8b6c3227%3A0xf1592407377b9781!2sCalgary%20Tower!5e0!3m2!1sen!2sca!4v1741962012805!5m2!1sen!2sca"),
            "Map showing Calgary Tower in downtown Calgary"));

    public Task<ContactContent> GetAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Content);
    }
}
