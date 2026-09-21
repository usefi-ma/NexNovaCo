using MudBlazor;

namespace NexNovaCo.Web.Components.Layout;

public static class DashboardTheme
{
    public static MudTheme Light { get; } = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#076b8d", PrimaryContrastText = "#ffffff",
            Background = "#f4f7fa", Surface = "#ffffff",
            AppbarBackground = "#ffffff", AppbarText = "#233449",
            DrawerBackground = "#ffffff", DrawerText = "#42536a", DrawerIcon = "#65768b",
            TextPrimary = "#1c2d41", TextSecondary = "#607086",
            Error = "#b42318", Success = "#187349",
            LinesDefault = "#e2e8ef", Divider = "#e2e8ef"
        },
        LayoutProperties = new LayoutProperties { DefaultBorderRadius = "12px", DrawerWidthLeft = "252px", AppbarHeight = "72px" },
        Typography = new Typography
        {
            Default = new DefaultTypography { FontFamily = ["Inter", "Segoe UI", "Arial", "sans-serif"], FontSize = "0.9375rem", LineHeight = "1.6" },
            H4 = new H4Typography { FontSize = "1.75rem", FontWeight = "650", LineHeight = "1.25", LetterSpacing = "-0.035em" },
            H5 = new H5Typography { FontSize = "1.25rem", FontWeight = "600", LineHeight = "1.4" },
            H6 = new H6Typography { FontSize = "1rem", FontWeight = "600", LineHeight = "1.5" },
            Button = new ButtonTypography { TextTransform = "none", FontWeight = "600", LetterSpacing = "0" }
        }
    };
}
