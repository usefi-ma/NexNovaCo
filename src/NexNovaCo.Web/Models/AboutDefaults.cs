namespace NexNovaCo.Web.Models;

// Exact approved editorial content. Seed/fallback only; never overwrites saved content.
public static class AboutDefaults
{
    public static AboutContent Content { get; } = new(
        new("About Us", "About Us",
            "At NexNovaCo, we are dedicated to innovation and excellence, creating tailored solutions that drive success. Discover our journey and commitment to empowering businesses. Together, we turn ideas into impactful realities.",
            "Explore Our Story", "about#About"),
        new("What We Do", "Innovating the Future with Smart Technology.",
            ["At NexNovaCo, we specialize in developing cutting-edge digital solutions that empower businesses to thrive. From custom software development to AI-driven innovations, we create tailored applications that enhance efficiency, boost engagement, and drive growth.",
             "Our team of experts blends technology, creativity, and strategy to deliver high-performance web and mobile applications, seamless UI/UX design, and intelligent AI solutions. We take pride in crafting scalable, future-ready software that helps businesses stay ahead in an ever-evolving digital landscape."],
            ["What makes NexNovaCo different is our commitment to collaboration and innovation. We work closely with clients to understand their unique needs, challenges, and goals—ensuring that every solution we build is both impactful and results-driven. With a focus on quality, efficiency, and excellence, we help businesses of all sizes turn their ideas into reality.",
             "Whether you're a startup looking to launch a groundbreaking product or an established company seeking to integrate AI into your workflow, NexNovaCo is your trusted partner in digital transformation. Let's build the future together."],
            "Join us on this journey of growth and innovation. Experience the difference with NexNovaCo - where creativity meets results."),
        new("Our Vision",
            ["We envision a world where technology transforms businesses, unlocking new possibilities and driving meaningful growth. Our goal is to be a trusted partner, delivering innovative, intelligent, and scalable solutions that empower companies to stay ahead in a rapidly evolving digital landscape.",
             "We strive to create a future where creativity, AI, and technology work together seamlessly, enabling businesses to streamline operations, enhance customer experiences, and achieve long-term success."],
            "Explore Our Services", "services", "image/about/vision.png", "NexNovaCo vision for technology and innovation"),
        new(new("Our Timeline", "Our Timeline showcases the journey of NexNovaCo, from our humble beginnings to becoming a leader in delivering innovative tech solutions. Explore how our dedication to growth, innovation, and customer satisfaction has shaped the milestones that define who we are today."),
            [new(2023, "Founded with a passion for innovation, NexNovaCo started its journey by focusing on custom software development, providing businesses with the tools they need to grow and succeed."),
             new(2024, "Expanded to offer web and mobile app development services, helping clients reach a broader audience with cutting-edge technology and high-performance applications."),
             new(2025, "Incorporated AI-powered solutions to revolutionize business processes, bringing automation, smarter recommendations, and data-driven insights to clients' applications.")],
            "View Our Projects", "projects"),
        new(new("NexNovaCo", "NexNovaCo delivers innovative AI-driven, web, and mobile solutions, empowering businesses with cutting-edge technology for growth and success."),
            "Our Mission",
            ["Our mission is to enable businesses to thrive by providing innovative, customized solutions that foster growth, streamline operations, and drive long-term success. We are dedicated to delivering high-quality services that align with each client's unique goals and challenges.",
             "By leveraging creativity, collaboration, and advanced technology, we strive to create impactful solutions that not only meet client needs but also exceed their expectations. Our commitment is to build strong, lasting relationships with our clients, delivering results that inspire confidence and sustainable success across industries."],
            ["Creating tailored strategies that align with the unique goals and challenges of each client.",
             "Utilizing advanced tools and innovative technology to enhance business efficiency.",
             "Fostering strong partnerships through transparent communication and mutual trust.",
             "Dedicated to delivering long-term value and measurable results for our clients."]),
        PartnerPresentationDefaults.Heading, []);
}
