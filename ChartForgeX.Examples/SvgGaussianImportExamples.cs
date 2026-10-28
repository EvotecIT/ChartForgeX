using System;
using System.IO;
using System.Globalization;
using ChartForgeX;
using ChartForgeX.Composition;
using ChartForgeX.Primitives;
using ChartForgeX.SvgRaster;

/// <summary>Explicit bounded SVG import with one existing native kernel, then ordinary report composition and self-contained snapshot export.</summary>
internal static class SvgGaussianImportExamples {
    internal static string Source(int width, bool dark) {
        var accent = dark ? "#38bdf8" : "#0369a1";
        var w = width.ToString(CultureInfo.InvariantCulture);
        return "<svg xmlns='http://www.w3.org/2000/svg' width='" + w + "' height='180' viewBox='0 0 " + w + " 180'>"
            + "<defs><filter id='soft' filterUnits='userSpaceOnUse' primitiveUnits='userSpaceOnUse' x='0' y='0' width='" + w
            + "' height='180' color-interpolation-filters='sRGB'><feGaussianBlur in='SourceGraphic' edgeMode='none' stdDeviation='3'/></filter></defs>"
            + "<g filter='url(#soft)'><rect x='30' y='35' width='110' height='54' rx='8' fill='" + accent + "'/><text x='34' y='125' font-size='24' fill='" + accent + "'>Native source</text></g></svg>";
    }
    internal static void Write(string output) {
        foreach (var dark in new[] { false, true }) foreach (var width in new[] { 320, 800 }) {
            var source = Source(width, dark);
            var imported = SvgRasterizer.Rasterize(source, strict: true, filters: new SvgRasterFilterOptions());
            var background = ChartColor.FromHex(dark ? "#111827" : "#f1f5f9");
            var foreground = ChartColor.FromHex(dark ? "#f8fafc" : "#0f172a");
            var report = ImageComposition.Create(width, 220, background).DrawImage(imported.Image, 0, 0, width, 180)
                .DrawText(24, 184, width - 48, "One explicit imported Gaussian", 16, foreground);
            var canvas = VisualCanvas.Create(width, 220).WithPngOutputScale(1).WithBackdrop(VisualCanvasBackdropStyle.Transparent)
                .WithTitle("Bounded Gaussian import").AddImage(report.ToImage(), 0, 0, width, 220);
            var stem = Path.Combine(output, "svg-gaussian-import-" + (dark ? "dark-" : "light-") + width.ToString(CultureInfo.InvariantCulture));
            File.WriteAllText(stem + ".source.svg", source);
            canvas.SaveSvg(stem + ".svg"); canvas.SavePng(stem + ".png"); canvas.SaveHtml(stem + ".html");
        }
    }
}
