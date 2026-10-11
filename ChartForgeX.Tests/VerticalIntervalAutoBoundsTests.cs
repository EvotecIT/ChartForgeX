using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class VerticalIntervalAutoBoundsTests {
    [Theory]
    [InlineData(false, ChartBarStyle.Flat, ChartScaleKind.Logarithmic, false)]
    [InlineData(false, ChartBarStyle.Flat, ChartScaleKind.Logarithmic, true)]
    [InlineData(false, ChartBarStyle.SegmentedCapsule, ChartScaleKind.Logarithmic, false)]
    [InlineData(false, ChartBarStyle.SegmentedCapsule, ChartScaleKind.Logarithmic, true)]
    [InlineData(true, ChartBarStyle.Flat, ChartScaleKind.Logarithmic, false)]
    [InlineData(true, ChartBarStyle.Flat, ChartScaleKind.Logarithmic, true)]
    [InlineData(false, ChartBarStyle.Flat, ChartScaleKind.Linear, false)]
    [InlineData(false, ChartBarStyle.Flat, ChartScaleKind.Linear, true)]
    [InlineData(false, ChartBarStyle.SegmentedCapsule, ChartScaleKind.Linear, false)]
    [InlineData(false, ChartBarStyle.SegmentedCapsule, ChartScaleKind.Linear, true)]
    [InlineData(true, ChartBarStyle.Flat, ChartScaleKind.Linear, false)]
    [InlineData(true, ChartBarStyle.Flat, ChartScaleKind.Linear, true)]
    public void AutomaticVerticalEndsContainMinimumBodiesAndEveryPaintLayer(bool boxPlot, ChartBarStyle style, ChartScaleKind scale, bool reversed) {
        var chart = Create(boxPlot, style, scale, reversed);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context); var groups = Groups(prepared, 0);
        Capture(prepared, Name(boxPlot, style, scale, reversed) + "-primary", groups);
        Assert.Null(chart.Options.YAxis.Minimum); Assert.Null(chart.Options.YAxis.Maximum);
        AssertRawFacts(groups, boxPlot, scale, false);
        AssertCompletePaint(groups);
        Assert.All(groups, group => Assert.Equal(2, Number(Body(group, boxPlot), "height")));
    }

    [Theory]
    [InlineData(false, ChartBarStyle.Flat, false)]
    [InlineData(false, ChartBarStyle.SegmentedCapsule, true)]
    [InlineData(true, ChartBarStyle.Flat, true)]
    public void SecondaryVerticalIntervalsKeepTheirOwnAutomaticGuttersInCartesianMixtures(bool boxPlot, ChartBarStyle style, bool reversed) {
        var chart = Create(boxPlot, style, ChartScaleKind.Logarithmic, reversed, secondary: true);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context); var groups = Groups(prepared, 1);
        Capture(prepared, Name(boxPlot, style, ChartScaleKind.Logarithmic, reversed) + "-secondary-mixed", groups);
        Assert.Null(chart.Options.SecondaryYAxis.Minimum); Assert.Null(chart.Options.SecondaryYAxis.Maximum);
        Assert.Equal(1, chart.Options.YAxis.Minimum); Assert.Equal(2, chart.Options.YAxis.Maximum);
        AssertRawFacts(groups, boxPlot, ChartScaleKind.Logarithmic, true);
        AssertCompletePaint(groups);
        Assert.All(groups, group => Assert.Equal(2, Number(Body(group, boxPlot), "height")));
        var primary = Groups(prepared, 0);
        Assert.Equal(new[] { 1.25, 1.75 }, primary.Select(group => Number(group, "data-cfx-y")));
    }

    [Theory]
    [InlineData(false, ChartBarStyle.Flat, false)]
    [InlineData(false, ChartBarStyle.SegmentedCapsule, true)]
    [InlineData(true, ChartBarStyle.Flat, true)]
    public void OneExplicitVerticalEndStillClipsWhileTheAutomaticEndContainsCompletePaint(bool boxPlot, ChartBarStyle style, bool reversed) {
        foreach (var explicitMaximum in new[] { false, true }) {
            var chart = Create(boxPlot, style, ChartScaleKind.Logarithmic, reversed);
            if (explicitMaximum) chart.Options.YAxis.Maximum = 100;
            else chart.Options.YAxis.Minimum = 10;
            var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context); var groups = Groups(prepared, 0);
            Capture(prepared, Name(boxPlot, style, ChartScaleKind.Logarithmic, reversed) + "-explicit-" + (explicitMaximum ? "maximum" : "minimum"), groups);
            AssertRawFacts(groups, boxPlot, ChartScaleKind.Logarithmic, false);
            var pinned = groups[explicitMaximum ? 1 : 0]; var automatic = groups[explicitMaximum ? 0 : 1];
            AssertCompletePaint(new[] { automatic });
            var cap = pinned.Descendants().First(element => Role(element) == (boxPlot ? "boxplot-cap" : "range-bar-cap"));
            var clip = EffectiveClip(cap);
            Close(Number(cap, "y1"), explicitMaximum != reversed ? clip.Top : clip.Bottom);
            var body = Rectangle(Body(pinned, boxPlot));
            Close(Intersect(body, clip).Height, 1);
            Assert.Contains(Parts(pinned), part => !Contained(PaintBounds(part), EffectiveClip(part)));
            if (explicitMaximum) { Assert.Null(chart.Options.YAxis.Minimum); Assert.Equal(100, chart.Options.YAxis.Maximum); }
            else { Assert.Equal(10, chart.Options.YAxis.Minimum); Assert.Null(chart.Options.YAxis.Maximum); }
        }
    }

    private static Chart Create(bool boxPlot, ChartBarStyle style, ChartScaleKind scale, bool reversed, bool secondary = false) {
        var chart = Chart.Create().WithSize(480, 320).WithHeader(false).WithLegend(false).WithDataLabels(false)
            .WithBarStyle(style).WithTheme(ChartTheme.GraphiteLight()).WithFontFamily("Carlito")
            .WithPngFont(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf"));
        if (secondary) {
            chart.AddLine("Primary", new[] { new ChartPoint(1, 1.25), new ChartPoint(2, 1.75) });
            chart.Series[0].Markers.Enabled = false; chart.Options.YAxis.WithBounds(1, 2);
        }
        var values = Values(scale);
        if (boxPlot) chart.AddBoxPlot("Summary", values.Select((value, point) => new ChartBoxPlot(point + 1, value, value, value, value, value)));
        else chart.AddRangeBar("Interval", values.Select((value, point) => new ChartInterval(point + 1, value, value)));
        var interval = chart.Series[secondary ? 1 : 0]; interval.WithStrokeWidth(2);
        if (secondary) interval.UseSecondaryYAxis();
        (secondary ? chart.Options.SecondaryYAxis : chart.Options.YAxis).WithScale(scale).WithReversal(reversed);
        chart.Options.XAxis.WithBounds(0, 3);
        return chart;
    }

    private static void AssertCompletePaint(XElement[] groups) {
        Assert.NotEmpty(groups);
        foreach (var group in groups) {
            var parts = Parts(group).ToArray(); Assert.NotEmpty(parts);
            Assert.All(parts, part => Assert.True(Contained(PaintBounds(part), EffectiveClip(part)),
                "Automatic interval paint was clipped: " + part));
        }
    }

    private static void AssertRawFacts(XElement[] groups, bool boxPlot, ChartScaleKind scale, bool secondary) {
        var values = Values(scale); var count = boxPlot ? 5 : 2;
        var keys = boxPlot ? new[] { "minimum", "q1", "median", "q3", "maximum" } : new[] { "start", "end" };
        Assert.Equal(2, groups.Length);
        for (var point = 0; point < groups.Length; point++) {
            Assert.Equal(secondary ? "secondary" : "primary", (string?)groups[point].Attribute("data-cfx-axis"));
            Assert.Equal(count, Number(groups[point], "data-cfx-source-count"));
            Assert.Equal(point, Number(groups[point], "data-cfx-point"));
            for (var member = 0; member < count; member++) {
                Assert.Equal(values[point], Number(groups[point], "data-cfx-" + keys[member]));
                Assert.Equal(values[point], Number(groups[point], "data-cfx-source-" + member + "-y"));
                Assert.Equal(point + 1, Number(groups[point], "data-cfx-source-" + member + "-x"));
                Assert.Equal(point * count + member, Number(groups[point], "data-cfx-source-" + member + "-index"));
            }
        }
    }

    private static XElement[] Groups(PreparedVisual prepared, int series) => XDocument.Parse(prepared.ToSvg()).Descendants()
        .Where(element => Role(element) == "point" && Number(element, "data-cfx-series") == series).ToArray();
    private static IEnumerable<XElement> Parts(XElement group) => group.Descendants().Where(element => element.Name.LocalName is "rect" or "line");
    private static XElement Body(XElement group, bool boxPlot) => Assert.Single(group.Descendants(), element => Role(element) == (boxPlot ? "boxplot-body" : "range-bar"));
    private static double[] Values(ChartScaleKind scale) => scale == ChartScaleKind.Logarithmic ? new[] { 10d, 100 } : new[] { -100d, -10 };
    private static string Name(bool boxPlot, ChartBarStyle style, ChartScaleKind scale, bool reversed) =>
        (boxPlot ? "boxplot" : "range-bar") + "-" + style + "-" + scale + "-" + (reversed ? "reversed" : "normal");

    private static ChartRect EffectiveClip(XElement part) {
        ChartRect? result = null;
        foreach (var ancestor in part.Ancestors()) {
            var reference = (string?)ancestor.Attribute("clip-path"); if (reference == null) continue;
            var clip = Assert.Single(part.Document!.Descendants(), element => element.Name.LocalName == "clipPath" && (string?)element.Attribute("id") == reference[5..^1]);
            var bounds = Rectangle(Assert.Single(clip.Elements()));
            result = result.HasValue ? Intersect(result.Value, bounds) : bounds;
        }
        Assert.True(result.HasValue, "Check actual paint against every ancestor plot clip.");
        return result!.Value;
    }

    private static ChartRect PaintBounds(XElement part) {
        var radius = (string?)part.Attribute("stroke") is null or "none" ? 0 : Number(part, "stroke-width") / 2;
        var bounds = part.Name.LocalName == "rect" ? Rectangle(part) : new ChartRect(
            Math.Min(Number(part, "x1"), Number(part, "x2")), Math.Min(Number(part, "y1"), Number(part, "y2")),
            Math.Abs(Number(part, "x2") - Number(part, "x1")), Math.Abs(Number(part, "y2") - Number(part, "y1")));
        return new ChartRect(bounds.Left - radius, bounds.Top - radius, bounds.Width + radius * 2, bounds.Height + radius * 2);
    }

    private static ChartRect Rectangle(XElement part) => new(Number(part, "x"), Number(part, "y"), Number(part, "width"), Number(part, "height"));
    private static ChartRect Intersect(ChartRect a, ChartRect b) => new(Math.Max(a.Left, b.Left), Math.Max(a.Top, b.Top),
        Math.Max(0, Math.Min(a.Right, b.Right) - Math.Max(a.Left, b.Left)), Math.Max(0, Math.Min(a.Bottom, b.Bottom) - Math.Max(a.Top, b.Top)));
    private static bool Contained(ChartRect paint, ChartRect clip) => paint.Left >= clip.Left - .003 && paint.Top >= clip.Top - .003
        && paint.Right <= clip.Right + .003 && paint.Bottom <= clip.Bottom + .003;
    private static void Close(double actual, double expected) => Assert.InRange(Math.Abs(actual - expected), 0, .002);
    private static string? Role(XElement part) => (string?)part.Attribute("data-cfx-role");
    private static double Number(XElement part, string attribute) => double.Parse(part.Attribute(attribute)!.Value, CultureInfo.InvariantCulture);

    private static void Capture(PreparedVisual prepared, string name, XElement[] groups) {
        var directory = Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory); var path = Path.Combine(directory, "vertical-auto-" + name);
        File.WriteAllText(path + ".svg", prepared.ToSvg()); File.WriteAllBytes(path + ".png", prepared.ToPng());
        File.WriteAllText(path + ".json", JsonSerializer.Serialize(new {
            paint = groups.SelectMany(Parts).Select(part => new { role = Role(part), bounds = PaintBounds(part), clip = EffectiveClip(part) }),
            facts = groups.Select(group => group.Attributes().ToDictionary(attribute => attribute.Name.LocalName, attribute => attribute.Value))
        }, new JsonSerializerOptions { WriteIndented = true }));
    }
}
