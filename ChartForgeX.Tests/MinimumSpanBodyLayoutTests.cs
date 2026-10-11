using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MinimumSpanBodyLayoutTests {
    private static readonly (double Start, double End)[] Intervals = {
        (0, 0), (100, 100), (0, .000001), (100, 99.999999), (20, 70)
    };

    [Theory]
    [InlineData(false, false, ChartBarStyle.Flat)]
    [InlineData(false, true, ChartBarStyle.Flat)]
    [InlineData(false, false, ChartBarStyle.SegmentedCapsule)]
    [InlineData(false, true, ChartBarStyle.SegmentedCapsule)]
    [InlineData(true, false, ChartBarStyle.Flat)]
    [InlineData(true, true, ChartBarStyle.Flat)]
    public void MinimumBodiesStayCenteredOnAuthoredValuesAndKeepEqualClippedInkAfterValueReversal(
        bool boxPlot, bool horizontal, ChartBarStyle style) {
        var normal = Prepare(boxPlot, horizontal, style, false);
        var reversed = Prepare(boxPlot, horizontal, style, true);
        var role = boxPlot ? "boxplot-body" : "range-bar";
        var normalGroups = PointGroups(normal); var reversedGroups = PointGroups(reversed);
        Assert.Equal(Intervals.Length, normalGroups.Length); Assert.Equal(normalGroups.Length, reversedGroups.Length);
        var normalBodies = Bodies(normal, role); var reversedBodies = Bodies(reversed, role);
        var clip = PlotClip(normalGroups[0]); var reversedClip = PlotClip(reversedGroups[0]);
        AssertRect(clip, reversedClip);
        var reflection = horizontal ? clip.Left + clip.Right : clip.Top + clip.Bottom;
        for (var point = 0; point < Intervals.Length; point++) {
            AssertBody(normal, normalGroups[point], normalBodies[point], boxPlot, horizontal, point);
            AssertBody(reversed, reversedGroups[point], reversedBodies[point], boxPlot, horizontal, point);
            var a = normalBodies[point].Bounds; var b = reversedBodies[point].Bounds;
            Close(Start(a, horizontal) + End(b, horizontal), reflection);
            Close(End(a, horizontal) + Start(b, horizontal), reflection);
            Close(VisibleLength(a, clip, horizontal), VisibleLength(b, reversedClip, horizontal));
            Close(horizontal ? a.Top : a.Left, horizontal ? b.Top : b.Left);
            AssertRawFacts(normalGroups[point], boxPlot, point); AssertRawFacts(reversedGroups[point], boxPlot, point);
        }

        // Flat paint has no directional cap lighting, so its actual clipped native ink must also mirror.
        int[]? normalInk = null; int[]? reversedInk = null;
        if (style == ChartBarStyle.Flat) {
            normalInk = NativeInk(normal, normalBodies, clip, horizontal);
            reversedInk = NativeInk(reversed, reversedBodies, reversedClip, horizontal);
            Assert.All(normalInk, count => Assert.True(count > 0, "A boundary body must retain native ink."));
            Assert.Equal(normalInk, reversedInk);
        }
        var name = (boxPlot ? "boxplot" : "range-bar") + "-" + (horizontal ? "horizontal" : "vertical") + "-" + style;
        Capture(normal, name + "-normal", normalBodies, clip, normalInk);
        Capture(reversed, name + "-reversed", reversedBodies, reversedClip, reversedInk);
    }

    private static PreparedVisual Prepare(bool boxPlot, bool horizontal, ChartBarStyle style, bool reversed) {
        var chart = Chart.Create().WithSize(480, 320).WithHeader(false).WithLegend(false).WithDataLabels(false)
            .WithAxes(false).WithGrid(false).WithBarStyle(style).WithTheme(ChartTheme.GraphiteLight())
            .WithFontFamily("Carlito").WithPngFont(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf"));
        var orientation = horizontal ? ChartOrientation.Horizontal : ChartOrientation.Vertical;
        if (boxPlot) chart.ConfigureBoxPlot(options => options.Orientation = orientation).AddBoxPlot("Summary", Intervals.Select((interval, point) => {
            var low = Math.Min(interval.Start, interval.End); var high = Math.Max(interval.Start, interval.End);
            return new ChartBoxPlot(point + 1, low, low, (low + high) / 2, high, high);
        }));
        else chart.ConfigureRangeBar(options => options.Orientation = orientation).AddRangeBar("Interval",
            Intervals.Select((interval, point) => new ChartInterval(point + 1, interval.Start, interval.End)));
        chart.Series[0].WithStrokeWidth(.1);
        (horizontal ? chart.Options.XAxis : chart.Options.YAxis).WithBounds(0, 100).WithReversal(reversed);
        (horizontal ? chart.Options.YAxis : chart.Options.XAxis).WithBounds(0, 6);
        return chart.Prepare(VisualExportRequest.ForChart(chart).Context);
    }

    private static void AssertBody(PreparedVisual prepared, XElement group, VisualSceneRectangle body, bool boxPlot, bool horizontal, int point) {
        var caps = group.Descendants().Where(element => Role(element) == (boxPlot ? "boxplot-cap" : "range-bar-cap")).ToArray();
        Assert.Equal(2, caps.Length);
        var first = Number(caps[0], horizontal ? "x1" : "y1"); var second = Number(caps[1], horizontal ? "x1" : "y1");
        var bounds = body.Bounds; var length = End(bounds, horizontal) - Start(bounds, horizontal);
        Close((Start(bounds, horizontal) + End(bounds, horizontal)) / 2, (first + second) / 2);
        if (point < 4) {
            Close(length, 2);
            Close(VisibleLength(bounds, PlotClip(group), horizontal), 1);
        } else {
            // Ordinary spans keep their original authored endpoint geometry.
            Close(Start(bounds, horizontal), Math.Min(first, second)); Close(length, Math.Abs(second - first));
        }
        var svgBody = Assert.Single(group.Descendants(), element => Role(element) == body.Role);
        AssertRect(bounds, Rectangle(svgBody));
        var region = Assert.Single(prepared.Regions, candidate => candidate.Id == (string?)group.Attribute("id"));
        AssertRect(bounds, region.Bounds);
    }

    private static void AssertRawFacts(XElement group, bool boxPlot, int point) {
        var interval = Intervals[point]; var low = Math.Min(interval.Start, interval.End); var high = Math.Max(interval.Start, interval.End);
        var values = boxPlot ? new[] { low, low, (low + high) / 2, high, high } : new[] { interval.Start, interval.End };
        var names = boxPlot ? new[] { "minimum", "q1", "median", "q3", "maximum" } : new[] { "start", "end" };
        Assert.Equal(point, Number(group, "data-cfx-point")); Assert.Equal(point + 1, Number(group, "data-cfx-x"));
        Assert.Equal(values.Length, Number(group, "data-cfx-source-count"));
        for (var member = 0; member < values.Length; member++) {
            Assert.Equal(values[member], Number(group, "data-cfx-" + names[member]));
            Assert.Equal(values[member], Number(group, "data-cfx-source-" + member + "-y"));
            Assert.Equal(point + 1, Number(group, "data-cfx-source-" + member + "-x"));
            Assert.Equal(point * values.Length + member, Number(group, "data-cfx-source-" + member + "-index"));
        }
    }

    private static int[] NativeInk(PreparedVisual prepared, VisualSceneRectangle[] bodies, ChartRect clip, bool horizontal) {
        var raster = prepared.ToRgba(new VisualRenderOptions(supersampling: 1));
        var backgroundX = (int)(horizontal ? clip.Left + clip.Width / 2 : clip.Left + 2);
        var backgroundY = (int)(horizontal ? clip.Top + 2 : clip.Top + clip.Height / 2);
        var background = (backgroundY * raster.Width + backgroundX) * 4;
        return bodies.Select(body => {
            var bounds = body.Bounds;
            var center = horizontal ? (bounds.Top + bounds.Bottom) / 2 : (bounds.Left + bounds.Right) / 2;
            var quarter = (horizontal ? bounds.Height : bounds.Width) / 4;
            var count = 0;
            for (var category = (int)Math.Ceiling(center - quarter); category < Math.Floor(center + quarter); category++) {
                for (var value = (int)Math.Ceiling(horizontal ? clip.Left : clip.Top); value < Math.Floor(horizontal ? clip.Right : clip.Bottom); value++) {
                    var x = horizontal ? value : category; var y = horizontal ? category : value;
                    var offset = (y * raster.Width + x) * 4;
                    var delta = Enumerable.Range(0, 3).Sum(channel => Math.Abs(raster.Pixels[offset + channel] - raster.Pixels[background + channel]));
                    if (delta > 10) count++;
                }
            }
            return count;
        }).ToArray();
    }

    private static XElement[] PointGroups(PreparedVisual prepared) => XDocument.Parse(prepared.ToSvg()).Descendants().Where(element => Role(element) == "point").ToArray();
    private static VisualSceneRectangle[] Bodies(PreparedVisual prepared, string role) => prepared.Scene.Nodes.OfType<VisualSceneRectangle>().Where(node => node.Role == role).ToArray();
    private static double Start(ChartRect bounds, bool horizontal) => horizontal ? bounds.Left : bounds.Top;
    private static double End(ChartRect bounds, bool horizontal) => horizontal ? bounds.Right : bounds.Bottom;
    private static double VisibleLength(ChartRect body, ChartRect clip, bool horizontal) => Math.Max(0,
        Math.Min(End(body, horizontal), End(clip, horizontal)) - Math.Max(Start(body, horizontal), Start(clip, horizontal)));

    private static ChartRect PlotClip(XElement group) {
        var reference = group.Ancestors().First(element => element.Attribute("clip-path") != null).Attribute("clip-path")!.Value;
        var clip = Assert.Single(group.Document!.Descendants(), element => (string?)element.Attribute("id") == reference[5..^1]);
        return Rectangle(Assert.Single(clip.Elements()));
    }

    private static ChartRect Rectangle(XElement element) => new(Number(element, "x"), Number(element, "y"), Number(element, "width"), Number(element, "height"));
    private static void AssertRect(ChartRect expected, ChartRect actual) {
        Close(expected.Left, actual.Left); Close(expected.Top, actual.Top); Close(expected.Width, actual.Width); Close(expected.Height, actual.Height);
    }
    private static void Close(double actual, double expected) => Assert.InRange(Math.Abs(actual - expected), 0, .002);
    private static string? Role(XElement element) => (string?)element.Attribute("data-cfx-role");
    private static double Number(XElement element, string attribute) => double.Parse(element.Attribute(attribute)!.Value, CultureInfo.InvariantCulture);

    private static void Capture(PreparedVisual prepared, string name, VisualSceneRectangle[] bodies, ChartRect clip, int[]? ink) {
        var directory = Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory); var path = Path.Combine(directory, "minimum-span-" + name);
        File.WriteAllText(path + ".svg", prepared.ToSvg()); File.WriteAllBytes(path + ".png", prepared.ToPng());
        File.WriteAllText(path + ".json", JsonSerializer.Serialize(new { clip, bodies = bodies.Select(body => body.Bounds), ink,
            regions = prepared.Regions.Where(region => region.Role == "point") }, new JsonSerializerOptions { WriteIndented = true }));
    }
}
