using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.SvgRaster;

namespace ChartForgeX.Rendering;

internal static partial class VisualSpecialtyCompiler {
    private static void PictorialImage(VisualSceneBuilder builder, ChartPictorialArtwork artwork, ChartRect slot, double amount, ChartOptions options) {
        var bounds = VisualSceneBuilder.FitImageBounds(artwork.Width, artwork.Height, slot, SvgRasterPreserveAspectRatio.Parse("xMidYMid meet"));
        if (amount < 1) builder.Image(artwork.Resource, bounds, "pictorial-empty", opacity: options.PictorialEmptyOpacity);
        if (amount <= 0) return;
        using (builder.PushClip(PictorialPartialBounds(bounds, amount, options.PictorialFillDirection)))
            builder.Image(artwork.Resource, bounds, "pictorial-fill");
    }

    private static ChartRect PictorialPartialBounds(ChartRect bounds, double amount, ChartPictorialFillDirection direction) => direction switch {
        ChartPictorialFillDirection.RightToLeft => new ChartRect(bounds.X + bounds.Width * (1 - amount), bounds.Y, bounds.Width * amount, bounds.Height),
        ChartPictorialFillDirection.BottomToTop => new ChartRect(bounds.X, bounds.Y + bounds.Height * (1 - amount), bounds.Width, bounds.Height * amount),
        ChartPictorialFillDirection.TopToBottom => new ChartRect(bounds.X, bounds.Y, bounds.Width, bounds.Height * amount),
        _ => new ChartRect(bounds.X, bounds.Y, bounds.Width * amount, bounds.Height)
    };
}
