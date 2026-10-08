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

    [Theory]
    [InlineData(false, 1, false)]
    [InlineData(false, 2, false)]
    [InlineData(true, 1, false)]
    [InlineData(true, 2, false)]
    [InlineData(true, 1, true)]
    [InlineData(true, 2, true)]
    public void GalleryAllowsDeclaredFlatFrameButStillDetectsEdgeMarks(bool translucent, int scale, bool framed) {
        var output=Path.Combine(Path.GetTempPath(),"ChartForgeX-graphite-gallery-test-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(output);
        try {
            var chart=Chart.Create().WithSize(320,220).WithTitle("Graphite frame").WithPngOutputScale(scale)
                .WithTheme(translucent ? ChartTheme.Light() : ChartTheme.GraphiteLight())
                .AddBar("Counts",new[]{new ChartPoint(1,20),new ChartPoint(2,50)});
            if(framed) chart.Options.Theme.WithSurfaceStyle(ChartSurfaceStyle.Framed);
            var names=new List<string>{"normal","edge-mark"};
            if(translucent) names.Add(framed?"edge-border-color":"edge-translucent-mark");
            foreach(var name in names) {
                File.WriteAllText(Path.Combine(output,name+".svg"),chart.ToSvg());
                File.WriteAllText(Path.Combine(output,name+".html"),chart.ToHtmlPage());
                var canvas=RenderCanvas(chart);
                if(name=="edge-mark") canvas.FillRect(0,50*scale,3*scale,20*scale,ChartColor.Black);
                if(name=="edge-border-color") canvas.FillRect(0,50*scale,3*scale,20*scale,chart.Options.Theme.CardBorder.WithAlpha(255));
                if(name=="edge-translucent-mark") canvas.FillRect(0,50*scale,3*scale,20*scale,ChartColor.Black.WithAlpha(96));
                File.WriteAllBytes(Path.Combine(output,name+".png"),PngWriter.WriteRgba(canvas.ToImage()));
            }
            GalleryWriter.Write(output);
            using var manifest=JsonDocument.Parse(File.ReadAllText(Path.Combine(output,"svg-png-comparison.json")));
            var charts=manifest.RootElement.GetProperty("charts").EnumerateArray().ToDictionary(e=>e.GetProperty("name").GetString()!,e=>e.Clone());
            Assert.True(charts["normal"].GetProperty("png").GetProperty("healthy").GetBoolean(), charts["normal"].GetProperty("png").GetRawText());
            Assert.Equal(0,charts["normal"].GetProperty("png").GetProperty("edgeInkPixels").GetInt64());
            var html=charts["normal"].GetProperty("html");
            Assert.Equal(!translucent,html.GetProperty("hasFlatSurface").GetBoolean());
            Assert.Equal(translucent,html.GetProperty("hasSurfaceGradient").GetBoolean());
            Assert.True(html.GetProperty("healthy").GetBoolean(),html.GetRawText());
            Assert.False(charts["edge-mark"].GetProperty("png").GetProperty("healthy").GetBoolean());
            Assert.True(charts["edge-mark"].GetProperty("png").GetProperty("edgeInkPixels").GetInt64()>0);
            if(translucent) {
                var extra=charts[framed?"edge-border-color":"edge-translucent-mark"].GetProperty("png");
                Assert.False(extra.GetProperty("healthy").GetBoolean());
                Assert.True(extra.GetProperty("edgeInkPixels").GetInt64()>0);
            }
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
