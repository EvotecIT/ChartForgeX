using System;
using System.Globalization;
using ChartForgeX.Raster;

namespace ChartForgeX.Topology;

public sealed partial class TopologyIconArtwork {
    /// <summary>Captures an RGBA portrait as deterministic embedded artwork for native raster and SVG output.</summary>
    /// <param name="image">The complete source image. Its pixels are read only during this call.</param>
    /// <param name="fit">The centered slot fit. Slice fills the existing artwork slot without changing node geometry.</param>
    /// <returns>Independent artwork; changing the caller's pixel array does not change its image.</returns>
    /// <remarks>No network or file access is performed. The existing topology artwork owner resolves the slot crop before both exporters.</remarks>
    public static TopologyIconArtwork Portrait(RgbaImage image, TopologyPortraitFit fit = TopologyPortraitFit.Slice) {
        TopologyModelGuards.EnumDefined(fit, nameof(fit));
        var validated = new RgbaImage(image.Width, image.Height, image.Pixels);
        return Image("data:image/png;base64," + Convert.ToBase64String(PngWriter.WriteRgba(validated)),
            "0 0 " + image.Width.ToString(CultureInfo.InvariantCulture) + " " + image.Height.ToString(CultureInfo.InvariantCulture))
            .WithPreserveAspectRatio(fit == TopologyPortraitFit.Slice ? "xMidYMid slice" : "xMidYMid meet");
    }
}
