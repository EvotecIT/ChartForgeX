using System;
using System.Globalization;
using System.IO;
using System.Linq;
using ChartForgeX.Raster;
using ChartForgeX.SvgRaster;

namespace ChartForgeX.Tests;

internal static class OrderedFilterFixture {
    internal static string Source(string nodes) => "<svg xmlns='http://www.w3.org/2000/svg' width='4' height='3'><defs>"
        + "<filter id='f' filterUnits='userSpaceOnUse' primitiveUnits='userSpaceOnUse' x='0' y='0' width='4' height='3' color-interpolation-filters='sRGB'>"
        + nodes + "</filter></defs><rect id='source' width='4' height='3' fill='blue' filter='url(#f)'/></svg>";
    internal static string Scale(string input, string? result, double amount) => "<feComponentTransfer in='" + input + "'"
        + (result == null ? "" : " result='" + result + "'") + "><feFuncR type='linear' slope='" + amount.ToString("R", CultureInfo.InvariantCulture)
        + "'/><feFuncG type='linear' slope='" + amount.ToString("R", CultureInfo.InvariantCulture) + "'/><feFuncB type='linear' slope='"
        + amount.ToString("R", CultureInfo.InvariantCulture) + "'/></feComponentTransfer>";
    internal static SvgRasterizationResult Import(string name, string nodes) {
        var source = Source(nodes); SaveSource(name, source); var result = SvgRasterizer.Rasterize(source, strict: true, filters: new SvgRasterFilterOptions());
        Capture(name, source, result); return result;
    }
    internal static byte[] Pixel(SvgRasterizationResult result, int x, int y) => result.Image.Pixels.Skip((y * result.Image.Width + x) * 4).Take(4).ToArray();
    internal static void SaveSource(string name, string source) {
        var directory = Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory); File.WriteAllText(Path.Combine(directory, "ordered-filter-" + name + ".source.svg"), source);
    }
    internal static void Capture(string name, string source, SvgRasterizationResult result) {
        var directory = Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory); var stem = Path.Combine(directory, "ordered-filter-" + name);
        File.WriteAllText(stem + ".source.svg", source); File.WriteAllBytes(stem + ".native.png", PngWriter.WriteRgba(result.Image));
        File.WriteAllBytes(stem + ".native.rgba", result.Image.Pixels);
        File.WriteAllText(stem + ".diagnostics.txt", string.Join("\n", result.Diagnostics.Select(item => item.Code + ":" + item.ElementId)));
    }
}
