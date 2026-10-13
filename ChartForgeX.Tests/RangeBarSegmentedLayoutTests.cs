using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class RangeBarSegmentedLayoutTests {
    [Theory]
    [InlineData(800, 440, VisualThemeMode.Light, ChartBarStyle.SegmentedCapsule)]
    [InlineData(800, 440, VisualThemeMode.Dark, ChartBarStyle.SegmentedCapsule)]
    [InlineData(360, 360, VisualThemeMode.Light, ChartBarStyle.SegmentedCapsule)]
    [InlineData(360, 360, VisualThemeMode.Dark, ChartBarStyle.SegmentedCapsule)]
    [InlineData(800, 440, VisualThemeMode.Light, ChartBarStyle.Flat)]
    [InlineData(360, 360, VisualThemeMode.Dark, ChartBarStyle.Flat)]
    public void FourCategoryGalleryKeepsVisibleBodiesAndCapsWithinDistinctPaintedSlots(int width, int height, VisualThemeMode mode, ChartBarStyle style) {
        var chart = Gallery(width, style); var prepared = chart.Prepare(Context(width, height, mode));
        var bodies = prepared.Scene.Nodes.OfType<VisualSceneRectangle>().Where(node => node.Role == "range-bar").ToArray();
        var caps = prepared.Scene.Nodes.OfType<VisualSceneLine>().Where(node => node.Role == "range-bar-cap").ToArray();
        Assert.Equal(8, bodies.Length); Assert.Equal(16, caps.Length);
        Assert.All(bodies, body => Assert.True(body.Bounds.Height >= 3, "The actual compact gallery needs a visible interval body."));
        Assert.All(caps, cap => Assert.True(cap.End.Y - cap.Start.Y > .05, "Caps retain a nonzero category span."));
        var document = XDocument.Parse(prepared.ToSvg());
        var groups = document.Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "point").ToArray();
        var footprints = groups.Select(PaintBounds).ToArray();
        var ordered = footprints.OrderBy(bounds => bounds.Top).ToArray();
        for (var item = 1; item < ordered.Length; item++) Assert.True(ordered[item - 1].Bottom < ordered[item].Top);
        Assert.Equal("32", (string?)groups[5].Attribute("data-cfx-start"));
        Assert.Equal("12", (string?)groups[5].Attribute("data-cfx-end"));
        Assert.Equal("1", (string?)groups[5].Attribute("data-cfx-source-point"));
        if (style == ChartBarStyle.SegmentedCapsule) {
            for (var item = 0; item < bodies.Length; item++) {
                var measured = ChartSegmentedBarGeometry.RangePaintBounds(chart.Options.ResolvePreparedBarVisualStyle(), bodies[item].Bounds, true);
                Assert.InRange(Math.Abs(measured.Top - footprints[item].Top), 0, .002); Assert.InRange(Math.Abs(measured.Bottom - footprints[item].Bottom), 0, .002);
                Assert.InRange(Math.Abs(measured.Left - footprints[item].Left), 0, .002); Assert.InRange(Math.Abs(measured.Right - footprints[item].Right), 0, .002);
            }
            Assert.All(prepared.Scene.Nodes.OfType<VisualSceneLine>().Where(node => node.Role == "range-bar-cap-highlight"),
                line => Assert.True(line.End.Y > line.Start.Y));
        }
        var raster = prepared.ToRgba(new VisualRenderOptions(supersampling: 1));
        var pixelDeltas = bodies.Select(body => {
            var x = (int)Math.Round(body.Bounds.Left + body.Bounds.Width / 2); var y = (int)Math.Round(body.Bounds.Top + body.Bounds.Height / 2);
            var inside = (y * raster.Width + x) * 4; var outside = (((int)Math.Floor(body.Bounds.Top) - 2) * raster.Width + x) * 4;
            return Enumerable.Range(0, 3).Sum(channel => Math.Abs(raster.Pixels[inside + channel] - raster.Pixels[outside + channel]));
        }).ToArray();
        Assert.All(pixelDeltas, delta => Assert.True(delta > 10, "Native body ink must differ from the neighboring background."));
        var directory = Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) {
            Directory.CreateDirectory(directory);
            var name = (width == 360 ? "compact-horizontal" : "horizontal") + "-" + mode.ToString().ToLowerInvariant() + "-" + style.ToString().ToLowerInvariant();
            File.WriteAllText(Path.Combine(directory, name + ".svg"), prepared.ToSvg());
            File.WriteAllBytes(Path.Combine(directory, name + ".png"), prepared.ToPng());
            File.WriteAllText(Path.Combine(directory, name + ".json"), JsonSerializer.Serialize(new {
                bodyHeights = bodies.Select(body => body.Bounds.Height), capSpans = caps.Select(cap => cap.End.Y - cap.Start.Y), pixelDeltas,
                footprints, regions = prepared.Regions.Where(region => region.Role == "point")
            }, new JsonSerializerOptions { WriteIndented = true }));
        }
    }

    [Theory]
    [InlineData(.1, false)]
    [InlineData(.1, true)]
    [InlineData(2, false)]
    [InlineData(2, true)]
    [InlineData(28, false)]
    [InlineData(28, true)]
    public void RangeCapsKeepPositiveCapAndHighlightSpansWithoutChangingWideGeometry(double width, bool horizontal) {
        var geometry = ChartSegmentedBarGeometry.RangeCap(ChartBarVisualStyle.DashboardCapsule(), 0, 0, width, horizontal);
        double Span(ChartSegmentedLine line) => horizontal ? line.Y2 - line.Y1 : line.X2 - line.X1;
        Assert.True(Span(geometry.Cap) > 0); Assert.True(Span(geometry.Highlight) > 0);
        Assert.True(Span(geometry.Highlight) < Span(geometry.Cap));
        if (width == 28) { Assert.Equal(39, Span(geometry.Cap), 8); Assert.Equal(35.92, Span(geometry.Highlight), 8); }
    }

    [Fact]
    public void DisabledLightingDoesNotConsumeCategorySpace() {
        var chart = Gallery(360, ChartBarStyle.SegmentedCapsule);
        var context = Context(360, 360, VisualThemeMode.Dark);
        double Thickness() => chart.Prepare(context).Scene.Nodes.OfType<VisualSceneRectangle>().First(node => node.Role == "range-bar").Bounds.Height;
        var lit = Thickness();
        chart.Options.BarVisualStyle.CapShadowOpacity = 0; chart.Options.BarVisualStyle.CapHighlightOpacity = 0;
        Assert.True(Thickness() > lit);
        Assert.DoesNotContain(chart.Prepare(context).Scene.Nodes, node => node.Role is "range-bar-cap-shadow-soft" or "range-bar-cap-shadow" or "range-bar-cap-highlight");
    }

    [Fact]
    public void MeasuredFootprintRemainsNonnegativeAndMonotonicAcrossCapClampBoundaries() {
        // Boundaries cover minimum cap width, inset, cap thickness, highlight stroke and maximum cap width.
        var widths = new[] { .1, 1, 2 / 1.7, 2d, 5.38 / 1.7, 2 / .45, 1 / (.24 * .45), 7 / .45, 22d, 28d }
            .SelectMany(width => new[] { Math.Max(.1, width - .000001), width, width + .000001 }).Distinct().OrderBy(width => width).ToArray();
        foreach (var shadows in new[] { true, false }) {
            var style = ChartBarVisualStyle.DashboardCapsule(); if (!shadows) style.CapShadowOpacity = 0;
            var previous = 0d;
            foreach (var width in widths) {
                var footprint = ChartSegmentedBarGeometry.RangeCategoryFootprint(style, width);
                Assert.True(footprint >= width && footprint >= previous);
                var cap = ChartSegmentedBarGeometry.RangeCap(style, 0, 0, width);
                Assert.True(cap.Highlight.X2 > cap.Highlight.X1 && cap.Cap.X2 > cap.Cap.X1);
                previous = footprint;
            }
        }
    }

    [Fact]
    public void DenseSegmentedGroupsRejectInsufficientEffectSpaceWhileNarrowFlatMarksRemainAvailable() {
        var chart = Gallery(360, ChartBarStyle.SegmentedCapsule);
        for (var index = 2; index < 8; index++) chart.AddRangeBar("Group " + index, new[] {
            new ChartInterval(1, 20, 40), new ChartInterval(2, 10, 30), new ChartInterval(3, 15, 35), new ChartInterval(4, 25, 50)
        });
        var context = Context(360, 360, VisualThemeMode.Dark);
        Assert.Equal("The category slot cannot fit the mark's minimum painted footprint. Increase category-axis space, reduce grouped series, or use a simpler mark style.",
            Assert.Throws<InvalidOperationException>(() => chart.Prepare(context)).Message);
        chart.WithBarStyle(ChartBarStyle.Flat);
        foreach (var series in chart.Series) series.StrokeWidth = 1;
        var prepared = chart.Prepare(context);
        Assert.Equal(32, prepared.Regions.Count(region => region.Role == "point"));
        var footprints = XDocument.Parse(prepared.ToSvg()).Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "point")
            .Select(PaintBounds).OrderBy(bounds => bounds.Top).ToArray();
        for (var item = 1; item < footprints.Length; item++) Assert.True(footprints[item - 1].Bottom < footprints[item].Top);
    }

    private static Chart Gallery(int width, ChartBarStyle style) => V2GalleryModels.Create(ChartSeriesKind.RangeBar, width == 360 ? "compact-horizontal" : "horizontal")
        .WithBarStyle(style);

    private static VisualRenderContext Context(int width, int height, VisualThemeMode mode) {
        const string family = "CFX Range Gallery Carlito";
        var directory = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito");
        FontRegistry.Register(family, Path.Combine(directory, "Carlito-Regular.ttf"), 400);
        FontRegistry.Register(family, Path.Combine(directory, "Carlito-Bold.ttf"), 700);
        return new VisualRenderContext(new VisualLayoutOptions(new VisualSize(width, height)), VisualTheme.Graphite(), mode,
            new VisualFrame(V2GalleryModels.Title(ChartSeriesKind.RangeBar), "Grouped horizontal intervals preserve both authored endpoints"), FontSpec.FromFamily(family));
    }

    private static ChartRect PaintBounds(XElement group) {
        var body = group.Descendants().Single(node => (string?)node.Attribute("data-cfx-role") == "range-bar");
        var left = Number(body, "x"); var right = left + Number(body, "width"); var top = Number(body, "y"); var bottom = top + Number(body, "height");
        foreach (var line in group.Descendants().Where(node => ((string?)node.Attribute("data-cfx-role"))?.StartsWith("range-bar-cap", StringComparison.Ordinal) == true)) {
            var radius = Number(line, "stroke-width") / 2;
            left = Math.Min(left, Math.Min(Number(line, "x1"), Number(line, "x2")) - radius);
            right = Math.Max(right, Math.Max(Number(line, "x1"), Number(line, "x2")) + radius);
            top = Math.Min(top, Math.Min(Number(line, "y1"), Number(line, "y2")) - radius);
            bottom = Math.Max(bottom, Math.Max(Number(line, "y1"), Number(line, "y2")) + radius);
        }
        return new ChartRect(left, top, right - left, bottom - top);
    }

    private static double Number(XElement node, string attribute) => double.Parse(node.Attribute(attribute)!.Value, CultureInfo.InvariantCulture);
}
