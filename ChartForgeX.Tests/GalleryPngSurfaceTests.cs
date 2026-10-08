using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Gallery perimeter health distinguishes authored surfaces from unexpected edge ink.</summary>
public sealed class GalleryPngSurfaceTests {
    [Theory]
    [InlineData("fractional-background", 1)]
    [InlineData("fractional-background", 2)]
    [InlineData("shadow", 1)]
    [InlineData("shadow", 2)]
    [InlineData("grid", 1)]
    [InlineData("grid", 2)]
    public void DeclaredSurfacesRemainHealthyAndExtraEdgeMarksRemainObservable(string surface, int scale) {
        var chart = Chart.Create().WithSize(320, 220).WithTitle("Perimeter health")
            .AddBar("Observed", new[] { new ChartPoint(1, 20), new ChartPoint(2, 50) });
        string svg;
        byte[] png;
        if (surface == "fractional-background") {
            var prepared = chart.Prepare(new VisualRenderContext(
                layout: new VisualLayoutOptions(new VisualSize(320, 220.553))));
            svg = prepared.ToSvg();
            png = prepared.ToPng(new VisualRenderOptions(scale: scale));
        } else if (surface == "shadow") {
            chart.WithTheme(ChartTheme.Dark().WithShadowOpacity(.24)).WithPngOutputScale(scale);
            svg = chart.ToSvg(); png = chart.ToPng();
            Assert.Contains("frame-card-shadow", svg);
        } else {
            var grid = ChartGrid.Create().WithColumns(2).WithTheme(ChartTheme.GraphiteLight())
                .WithPngOutputScale(scale).Add(chart).Add(Chart.Create().WithSize(320, 220)
                    .AddLine("Trend", new[] { new ChartPoint(1, 10), new ChartPoint(2, 30) }));
            svg = grid.ToSvg(); png = grid.ToPng();
            Assert.True(svg.Split("data-cfx-role=\"frame-card\"").Length > 2);
        }
        var output = Path.Combine(Path.GetTempPath(), "ChartForgeX-perimeter-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(output);
        try {
            var raster = RasterImageDecoder.Decode(png);
            foreach (var name in new[] { "normal", "extra-edge-mark" }) {
                File.WriteAllText(Path.Combine(output, name + ".svg"), svg);
                var canvas = new RgbaCanvas(raster.Width, raster.Height, 1);
                canvas.DrawImage(0, 0, raster.Width, raster.Height, raster.Pixels);
                // Even the exact authored shadow RGB must not excuse an opaque added mark.
                if (name == "extra-edge-mark") canvas.FillRect(raster.Width / 2, raster.Height - 2 * scale,
                    20 * scale, 2 * scale, surface == "shadow" ? ChartColor.FromRgb(15, 23, 42) : ChartColor.Black);
                File.WriteAllBytes(Path.Combine(output, name + ".png"), PngWriter.WriteRgba(canvas.ToImage()));
            }
            GalleryWriter.Write(output);
            using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(output, "svg-png-comparison.json")));
            var charts = manifest.RootElement.GetProperty("charts").EnumerateArray()
                .ToDictionary(item => item.GetProperty("name").GetString()!, item => item.GetProperty("png").Clone());
            Assert.True(charts["normal"].GetProperty("healthy").GetBoolean(), charts["normal"].GetRawText());
            Assert.Equal(0, charts["normal"].GetProperty("edgeInkPixels").GetInt64());
            Assert.True(charts["extra-edge-mark"].GetProperty("edgeInkPixels").GetInt64() > 0);
            Assert.False(charts["extra-edge-mark"].GetProperty("healthy").GetBoolean());
        } finally {
            Directory.Delete(output, recursive: true);
        }
    }
}
