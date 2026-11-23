using System;
using ChartForgeX;
using ChartForgeX.SvgRaster;

internal static partial class Program {
    private static void StandardBlendSourceContracts() {
        foreach (var mode in new[] { "normal", "multiply", "screen", "overlay", "darken", "lighten", "color-dodge", "color-burn", "hard-light", "soft-light", "difference", "exclusion", "hue", "saturation", "color", "luminosity" }) {
            var source = "<svg xmlns='http://www.w3.org/2000/svg' width='32' height='32'><defs>"
                + "<filter id='f' filterUnits='userSpaceOnUse' primitiveUnits='userSpaceOnUse' x='0' y='0' width='32' height='32' color-interpolation-filters='sRGB'>"
                + "<feFlood flood-color='blue' flood-opacity='.5' result='backdrop'/><feBlend in='SourceGraphic' in2='backdrop' mode='" + mode + "'/></filter></defs>"
                + "<rect x='8' y='8' width='16' height='16' fill='red' opacity='.5' filter='url(#f)'/></svg>";
            var value = SvgRasterizer.Rasterize(source, strict: true, filters: new SvgRasterFilterOptions());
            PackageAssertions.Require(value.Diagnostics.Count == 0 && value.Image.Pixels[(16 * 32 + 16) * 4 + 3] > 0,
                "A standard explicit sRGB source mode was lost.");
            PackageAssertions.Png(value.Image.ToPng());
        }
    }
}
