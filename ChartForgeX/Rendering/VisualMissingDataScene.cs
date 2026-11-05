using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

/// <summary>The key swatch for an absent observation, painted in the look of the missing marks it explains.</summary>
internal static class VisualMissingDataScene {
    internal static void Swatch(VisualSceneBuilder builder, ChartRect box, ChartMissingLook look, string prefix, ChartColor captionInk) =>
        ChartMissingData.Paint(builder, box, 1, look.WithInk(captionInk), prefix + "-missing-swatch", prefix + "-missing");
}