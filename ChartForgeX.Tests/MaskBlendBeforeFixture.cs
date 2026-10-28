using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using ChartForgeX.Composition;
using ChartForgeX.Raster;
using ChartForgeX.SvgRaster;

namespace ChartForgeX.Tests;

internal static class MaskBlendBeforeFixture {
    internal static string Mask(string color, string mode, string opacity, string? colorSpace) =>
        "<svg xmlns='http://www.w3.org/2000/svg' width='32' height='32'><defs>"
        + "<mask id='mask' mask-type='" + mode + "' maskUnits='userSpaceOnUse' maskContentUnits='userSpaceOnUse' x='0' y='0' width='32' height='32'"
        + (colorSpace == null ? "" : " color-interpolation='" + colorSpace + "'") + ">"
        + "<rect width='32' height='32' fill='" + color + "' fill-opacity='" + opacity + "'/></mask></defs>"
        + "<rect id='target' x='8' y='8' width='16' height='16' fill='white' mask='url(#mask)'/></svg>";
    internal static byte[] Pixel(RgbaImage image, int x, int y) => image.Pixels.Skip((y * image.Width + x) * 4).Take(4).ToArray();
    internal static void Capture(string stem, string source, SvgRasterizationResult result) {
        var directory = Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory); var path = Path.Combine(directory, stem);
        File.WriteAllText(path + ".source.svg", source); File.WriteAllBytes(path + ".native.png", ImageComposition.FromImage(result.Image).ToPng());
        File.WriteAllBytes(path + ".rgba", result.Image.Pixels);
        File.WriteAllText(path + ".observed.json", JsonSerializer.Serialize(new {
            width = result.Image.Width, height = result.Image.Height, interior = Pixel(result.Image, 16, 16), outside = Pixel(result.Image, 2, 2),
            diagnostics = result.Diagnostics.Select(item => new { item.Code, item.Message, item.ElementName, item.ElementId })
        }));
    }
}
