using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Uses the downloadable recipe to qualify supported compositions through public preparation and both native exporters.</summary>
public sealed class SupportedCombinationLifecycleTests {
    [Theory]
    [InlineData("vertical-secondary", VisualThemeMode.Light, 800)]
    [InlineData("vertical-secondary", VisualThemeMode.Dark, 390)]
    [InlineData("horizontal", VisualThemeMode.Light, 800)]
    [InlineData("horizontal", VisualThemeMode.Dark, 390)]
    [InlineData("mixed-radial", VisualThemeMode.Light, 800)]
    [InlineData("mixed-radial", VisualThemeMode.Dark, 390)]
    public void RetainedHandlesKeepMissingZeroAndIndependentAxesThroughConfigurationAndExport(string layout, VisualThemeMode mode, int width) {
        var chart = Assert.IsType<Chart>(ExampleRecipes.Named("supported-combinations").Create(layout, mode)).WithSize(width, 460);
        var observed = chart.Series.Single(series => series.Id.Value == "observed");
        var reference = chart.Series.Single(series => series.Id.Value == "reference");
        var before = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var beforeSvg = before.ToSvg(); var beforePng = before.ToPng();
        Assert.Equal(new[] { 1d, 3, 4 }, observed.Points.Select(point => point.X));
        Assert.Equal(0, observed.Points[^1].Y);
        Assert.Equal(3, observed.SourcePointCount);
        Assert.Equal(4, reference.SourcePointCount);
        Assert.Equal(layout == "vertical-secondary" ? ChartAxisSide.Secondary : ChartAxisSide.Primary, reference.YAxis);
        var points = Targets(before);
        Assert.DoesNotContain(points, point => Fact(point, "series-key") == "observed" && Fact(point, "observation-id") == "north-remote");
        Assert.Contains(points, point => Fact(point, "series-key") == "observed" && Fact(point, "observation-id") == "south-remote");
        if (layout == "vertical-secondary") {
            var axis = Assert.Single(before.Scene.Nodes.OfType<VisualSceneLine>(), node => node.Role == "axis-y");
            var bottom = Math.Max(axis.Start.Y, axis.End.Y);
            var height = Math.Abs(axis.Start.Y - axis.End.Y);
            var bubble = Assert.Single(before.Regions, region => region.Id == "series-1-point-0");
            Assert.Equal(bottom - height * .72, bubble.Bounds.Top + bubble.Bounds.Height / 2, 7);
            var bar = Assert.Single(before.Regions, region => region.Id == "series-0-point-0");
            Assert.Equal(bottom - height * (28d / 70), bar.Bounds.Top, 7);
            Assert.Contains(before.Regions, region => region.Role == "axis-secondary-y-label" && region.Label!.StartsWith("100%", StringComparison.Ordinal));
        }
        if (layout != "mixed-radial") {
            var source = Assert.Single(points, point => Fact(point, "series-key") == "observed" && Fact(point, "observation-id") == "north-central");
            Assert.Contains("North / Central", (string?)source.Attribute("aria-label"));
            Assert.Contains(before.Scene.Nodes, node => node.Role == (layout == "horizontal" ? "axis-y-category-group" : "axis-x-category-group"));
        }
        // Changing real source/configuration after preparation must not alter either exported artifact.
        observed.Points[0] = new ChartPoint(1, 34);
        if (layout == "vertical-secondary") {
            reference.ReplaceBubbles(new[] { new ChartBubble(3, .83, 11), new ChartBubble(1, .94, 16), new ChartBubble(4, .88, 13) });
            Assert.Empty(reference.ObservationIds);
            reference.WithObservationIds(new[] { "south-central", "north-central", "south-remote" }.Select(id => new ChartObservationId(id)));
        } else reference.Points[0] = new ChartPoint(1, 48);
        chart.MoveSeries(reference, 0).WithTheme(VisualTheme.Graphite(), mode == VisualThemeMode.Light ? VisualThemeMode.Dark : VisualThemeMode.Light);
        Assert.Equal(beforeSvg, before.ToSvg()); Assert.Equal(beforePng, before.ToPng());
        var after = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var changed = Assert.Single(Targets(after), point => Fact(point, "series-key") == "reference" && Fact(point, "observation-id") == "north-central");
        Assert.Equal("0", Fact(changed, "series"));
        Assert.Equal(layout == "vertical-secondary" ? .94 : 48, double.Parse((Fact(changed, "y") ?? Fact(changed, "value"))!, System.Globalization.CultureInfo.InvariantCulture));
        var image = RasterImageDecoder.Decode(after.ToPng());
        Assert.Equal(width, image.Width); Assert.Equal(460, image.Height);
        Capture(before, layout + "-before-" + width);
        Capture(after, layout + "-after-" + width);
    }

    private static XElement[] Targets(PreparedVisual prepared) => XDocument.Parse(prepared.ToSvg()).Descendants()
        .Where(node => node.Attribute("data-cfx-observation-id") != null && node.Attribute("data-cfx-label-for") == null).ToArray();
    private static string? Fact(XElement node, string name) => (string?)node.Attribute("data-cfx-" + name);
    private static void Capture(PreparedVisual prepared, string name) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        if (!Path.IsPathRooted(directory)) throw new InvalidOperationException("Captures require an absolute task path.");
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "supported-combination-" + name + ".svg"), prepared.ToSvg());
        File.WriteAllBytes(Path.Combine(directory, "supported-combination-" + name + ".native.png"), prepared.ToPng());
    }
}
