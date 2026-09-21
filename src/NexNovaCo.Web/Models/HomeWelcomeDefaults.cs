namespace NexNovaCo.Web.Models;

// Exact approved Welcome copy. Used only for missing-row initialization and read fallback.
public static class HomeWelcomeDefaults
{
    public static WelcomeContent Content { get; } = new(
        "Welcome to NexNovaCo",
        "Whether you're a startup bringing a bold new idea to life or an enterprise looking to enhance your digital presence, our team is committed to delivering tailored solutions that align with your unique needs.",
        ["From intuitive user experiences to powerful backend systems, we build software that is not only functional but also optimized for performance, security, and growth.",
         "Our mission is to empower businesses with smart, scalable, and future-ready digital solutions. From AI-driven automation to tailored app development, we help our clients stay ahead in an ever-evolving digital world. Let's build the future together!"],
        "Learn more", "about");
}
