using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class GraphiteGalleryTests {
    [Theory]
    [InlineData(false, 1)]
    [InlineData(true, 1)]
    [InlineData(false, 2)]
    [InlineData(true, 2)]
    public void HostFrameAllowsFittedAxisLabelsAndGuidesButRejectsUnrelatedEdgeInk(bool dark, int scale) {
        var output = Path.Combine(Path.GetTempPath(), "ChartForgeX-host-gallery-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(output);
        try {
            var chart = Chart.Create().WithSize(596, 338).WithTitle("Host frame").WithHostFrame()
                .WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight()).WithPngOutputScale(scale)
                .WithXLabels("One", "Two", "Three").AddBar("Counts", new[] { new ChartPoint(1, 50), new ChartPoint(2, 30), new ChartPoint(3, 20) });
            foreach (var name in new[] { "normal", "edge-mark" }) {
                File.WriteAllText(Path.Combine(output, name + ".svg"), chart.ToSvg());
                File.WriteAllText(Path.Combine(output, name + ".html"), chart.ToHtmlPage());
                var canvas = RenderCanvas(chart);
                if (name == "edge-mark") canvas.FillRect(0, 20 * scale, 3 * scale, 20 * scale, ChartColor.Black);
                File.WriteAllBytes(Path.Combine(output, name + ".png"), PngWriter.WriteRgba(canvas.ToImage()));
            }
            GalleryWriter.Write(output);
            using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(output, "svg-png-comparison.json")));
            var charts = manifest.RootElement.GetProperty("charts").EnumerateArray().ToDictionary(e => e.GetProperty("name").GetString()!, e => e.Clone());
            Assert.True(charts["normal"].GetProperty("svg").GetProperty("healthy").GetBoolean());
            Assert.True(charts["normal"].GetProperty("png").GetProperty("healthy").GetBoolean(), charts["normal"].GetProperty("png").GetRawText());
            Assert.Equal(0, charts["normal"].GetProperty("png").GetProperty("edgeInkPixels").GetInt64());
            Assert.False(charts["edge-mark"].GetProperty("png").GetProperty("healthy").GetBoolean());
            Assert.True(charts["edge-mark"].GetProperty("png").GetProperty("edgeInkPixels").GetInt64() > 0);
        } finally {
            Directory.Delete(output, recursive: true);
        }
    }

    [Fact]
    public void GalleryAllowsDeclaredFlatFrameButStillDetectsEdgeMarks() {
        var output=Path.Combine(Path.GetTempPath(),"ChartForgeX-graphite-gallery-test-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(output);
        try {
            var chart=Chart.Create().WithSize(320,220).WithTitle("Graphite frame").AddBar("Counts",new[]{new ChartPoint(1,20),new ChartPoint(2,50)});
            foreach(var name in new[]{"normal","edge-mark"}) {
                File.WriteAllText(Path.Combine(output,name+".svg"),chart.ToSvg());
                File.WriteAllText(Path.Combine(output,name+".html"),chart.ToHtmlPage());
                var canvas=RenderCanvas(chart);
                if(name=="edge-mark") canvas.FillRect(0,50,3,20,ChartColor.Black);
                File.WriteAllBytes(Path.Combine(output,name+".png"),PngWriter.WriteRgba(canvas.ToImage()));
            }
            GalleryWriter.Write(output);
            using var manifest=JsonDocument.Parse(File.ReadAllText(Path.Combine(output,"svg-png-comparison.json")));
            var charts=manifest.RootElement.GetProperty("charts").EnumerateArray().ToDictionary(e=>e.GetProperty("name").GetString()!,e=>e.Clone());
            Assert.True(charts["normal"].GetProperty("png").GetProperty("healthy").GetBoolean());
            Assert.Equal(0,charts["normal"].GetProperty("png").GetProperty("edgeInkPixels").GetInt64());
            Assert.True(charts["normal"].GetProperty("html").GetProperty("hasFlatSurface").GetBoolean());
            Assert.False(charts["edge-mark"].GetProperty("png").GetProperty("healthy").GetBoolean());
            Assert.True(charts["edge-mark"].GetProperty("png").GetProperty("edgeInkPixels").GetInt64()>0);
        } finally {
            Directory.Delete(output,recursive:true);
        }
    }
    private static RgbaCanvas RenderCanvas(Chart chart) {
        var image = RasterRenderer.RenderImage(chart);
        var canvas = new RgbaCanvas(image.Width, image.Height, 1);
        canvas.DrawImage(0, 0, image.Width, image.Height, image.Pixels);
        return canvas;
    }
}
