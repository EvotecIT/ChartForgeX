using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class RangeBarNativeTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GroupedBodiesCapsAndPointPaintRemainDistinctInNativeExports(bool horizontal) {
        var chart = Chart.Create().WithSize(horizontal ? 360 : 800, 440).WithTitle("Delivery time ranges")
            .WithSubtitle("Expected and observed intervals by service").WithXLabels("Standard", "Express")
            .WithTheme(VisualTheme.Graphite(), horizontal ? VisualThemeMode.Dark : VisualThemeMode.Light).WithDataLabels().WithFontFamily("Carlito")
            .WithPngFont(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf"))
            .WithBarStyle(horizontal ? ChartBarStyle.SegmentedCapsule : ChartBarStyle.Flat)
            .ConfigureRangeBar(options => options.Orientation = horizontal ? ChartOrientation.Horizontal : ChartOrientation.Vertical)
            .AddRangeBar("Expected", new[] { new ChartInterval(1, 2, 8), new ChartInterval(2, 3, 9) }, ChartColor.FromRgb(11, 100, 190))
            .AddRangeBar("Observed", new[] { new ChartInterval(1, 4, 10), new ChartInterval(2, 11, 5) }, ChartColor.FromRgb(220, 90, 35));
        if (horizontal) chart.Options.XAxis.WithBounds(0, 12);
        chart.Series[1].WithPointColor(1, "#A5358A").WithPointFillPattern(1, ChartFillPattern.Crosshatch);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var bodies = prepared.Scene.Nodes.OfType<VisualSceneRectangle>().Where(node => node.Role == "range-bar").ToArray();
        var caps = prepared.Scene.Nodes.OfType<VisualSceneLine>().Where(node => node.Role == "range-bar-cap").ToArray();
        Assert.Equal(4, bodies.Length); Assert.Equal(8, caps.Length);
        Assert.All(caps, cap => Assert.Equal(horizontal ? cap.Start.X : cap.Start.Y, horizontal ? cap.End.X : cap.End.Y));
        for (var category = 0; category < 2; category++) {
            double Edge(VisualSceneLine cap, bool end) => (horizontal
                ? end ? Math.Max(cap.Start.Y, cap.End.Y) : Math.Min(cap.Start.Y, cap.End.Y)
                : end ? Math.Max(cap.Start.X, cap.End.X) : Math.Min(cap.Start.X, cap.End.X)) + (end ? .5 : -.5) * cap.StrokeWidth;
            Assert.True(Edge(caps[category * 2], true) < Edge(caps[4 + category * 2], false));
        }
        Assert.Contains(prepared.Scene.Nodes, node => node.Role == "range-bar-pattern");
        Assert.Contains(prepared.Scene.Nodes.OfType<VisualSceneText>(), node => node.Role == "data-label");
        var variables = new SvgColorVariables().Add("--observed", ChartColor.FromHex("#A5358A"), SvgColorRole.Series);
        var document = XDocument.Parse(prepared.ToSvg(new VisualSvgOptions(colorVariables: variables)));
        Assert.All(document.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "range-bar-cap").Skip(6),
            element => Assert.Contains("var(--observed,", element.Attribute("stroke")!.Value));
        var raster = prepared.ToRgba(new VisualRenderOptions(supersampling: 1));
        var samples = bodies.Take(3).Select(body => {
            // Sample off the value gridlines that now run behind translucent horizontal bodies (design rule 2).
            var x = (int)Math.Round(body.Bounds.Left + body.Bounds.Width * (horizontal ? .43 : .5));
            var y = (int)Math.Round(body.Bounds.Top + body.Bounds.Height / 2);
            return Convert.ToHexString(raster.Pixels.Skip((y * raster.Width + x) * 4).Take(4).ToArray());
        }).ToArray();
        Assert.Equal(samples[0], samples[1]); Assert.NotEqual(samples[0], samples[2]);
        var directory = Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) {
            Directory.CreateDirectory(directory);
            var name = horizontal ? "grouped-horizontal-compact-dark-segmented" : "grouped-vertical-wide-light";
            File.WriteAllText(Path.Combine(directory, name + ".svg"), prepared.ToSvg());
            File.WriteAllBytes(Path.Combine(directory, name + ".png"), prepared.ToPng());
            File.WriteAllText(Path.Combine(directory, name + ".json"), JsonSerializer.Serialize(new {
                samples, regions = prepared.Regions.Where(region => region.Role == "point"), diagnostics = prepared.Diagnostics
            }, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
