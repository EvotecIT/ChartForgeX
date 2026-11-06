using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Themes;

public sealed partial class VisualTheme {
    /// <summary>Creates the Graphite theme with a palette that colorblind readers can tell apart.</summary>
    /// <remarks>Typography, geometry, surfaces and both modes are Graphite's; only the series palette changes.</remarks>
    public static VisualTheme Accessible() => Graphite().WithPalette(ChartPalettes.Colorblind.ToArray());

    /// <summary>Creates the Graphite theme with transparent backgrounds for drawing over imagery and wallpapers.</summary>
    /// <remarks>Typography, geometry and palettes are Graphite's. Plot and page backgrounds are transparent in both modes;
    /// cards keep a translucent surface so text stays readable over a busy image.</remarks>
    public static VisualTheme TransparentOverlay() => Graphite().Configure(builder => {
        foreach (var colors in new[] { builder.Light, builder.Dark }) {
            colors.Background = ChartColors.Transparent;
            colors.Surface = ChartColors.Transparent;
            colors.ElevatedSurface = colors.ElevatedSurface.WithAlpha(172);
        }
        builder.CardShadowOpacity = 0;
    });
}
