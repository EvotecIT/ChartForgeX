using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class BoxPlotHorizontalPaintBoundsTests {
    [Theory]
    [InlineData(800, false, ChartScaleKind.Linear, false, -1)]
    [InlineData(360, false, ChartScaleKind.Linear, false, -1)]
    [InlineData(360, true, ChartScaleKind.Linear, false, -1)]
    [InlineData(360, false, ChartScaleKind.Logarithmic, false, -1)]
    [InlineData(800, false, ChartScaleKind.Linear, true, -1)]
    [InlineData(360, false, ChartScaleKind.Linear, true, -1)]
    [InlineData(360, false, ChartScaleKind.Linear, true, 6)]
    [InlineData(360, false, ChartScaleKind.Linear, true, .1)]
    [InlineData(360, true, ChartScaleKind.Logarithmic, true, -1)]
    public void AutomaticValueDomainContainsCompleteBodiesWhiskersCapsAndMedians(int width, bool reverseValue,
        ChartScaleKind scale, bool equalQuartilesAtMaximum, double strokeWidth) {
        var chart = Create(width, equalQuartilesAtMaximum, strokeWidth);
        chart.Options.XAxis.WithScale(scale).WithReversal(reverseValue);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var svg = prepared.ToSvg();
        var document = XDocument.Parse(svg);
        AssertRawStatistics(document, equalQuartilesAtMaximum);
        var paint = PaintBounds(document);
        Assert.Equal(10, paint.Length);
        Assert.Equal(2, paint.Count(item => item.Role == "boxplot-body"));
        Assert.Equal(4, paint.Count(item => item.Role == "boxplot-cap"));
        Assert.Equal(2, paint.Count(item => item.Role == "boxplot-whisker"));
        Assert.Equal(2, paint.Count(item => item.Role == "boxplot-median"));

        // Keep two native failures available even when the containment assertion below fails.
        if (!reverseValue && scale == ChartScaleKind.Linear && strokeWidth < 0
            && ((width == 800 && !equalQuartilesAtMaximum) || (width == 360 && equalQuartilesAtMaximum))) {
            Capture(width == 800 ? "wide-default" : "compact-equal-maximum", prepared, svg, paint, document);
        }

        const double serializedTolerance = .002;
        var clipped = paint.Where(item => item.Left < item.ClipLeft - serializedTolerance
            || item.Right > item.ClipRight + serializedTolerance || item.Top < item.ClipTop - serializedTolerance
            || item.Bottom > item.ClipBottom + serializedTolerance).ToArray();
        Assert.True(clipped.Length == 0, "Automatic bounds clipped authored BoxPlot paint: "
            + JsonSerializer.Serialize(clipped));
    }

    [Fact]
    public void ExplicitValueBoundsKeepAuthoredExtremaAtTheClipAndIntentionallyClipPaint() {
        var chart = Create(360, true, 6);
        chart.Options.XAxis.WithBounds(10, 100);
        var document = XDocument.Parse(chart.Prepare(VisualExportRequest.ForChart(chart).Context).ToSvg());
        AssertRawStatistics(document, true);
        var caps = document.Descendants().Where(element => Role(element) == "boxplot-cap").ToArray();
        Assert.Equal(4, caps.Length);
        var minimumClip = PlotClip(document, caps[0]);
        var maximumClip = PlotClip(document, caps[1]);
        Assert.Equal(Number(minimumClip, "x"), Number(caps[0], "x1"), 3);
        Assert.Equal(Number(maximumClip, "x") + Number(maximumClip, "width"), Number(caps[1], "x1"), 3);
        Assert.Contains(PaintBounds(document), item => item.Role == "boxplot-body" && item.Right > item.ClipRight);
        Assert.Contains(PaintBounds(document), item => item.Role == "boxplot-cap" && item.Left < item.ClipLeft);
    }

    private static Chart Create(int width, bool equalQuartilesAtMaximum, double strokeWidth) {
        var summaries = new[] { new ChartBoxPlot(1, 10, 20, 30, 50, 100), equalQuartilesAtMaximum
            ? new ChartBoxPlot(2, 100, 100, 100, 100, 100) : new ChartBoxPlot(2, 20, 25, 40, 60, 80) };
        var chart = Chart.Create().WithSize(width, 440).WithHeader(false).WithLegend(false).WithDataLabels(false)
            .WithTheme(VisualTheme.Graphite(), width == 360 ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .WithFontFamily("Carlito").WithPngFont(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf"))
            .ConfigureBoxPlot(options => options.Orientation = ChartOrientation.Horizontal).AddBoxPlot("Summary", summaries);
        if (strokeWidth >= 0) chart.Series[0].WithStrokeWidth(strokeWidth);
        return chart;
    }

    private static void AssertRawStatistics(XDocument document, bool equalQuartilesAtMaximum) {
        var expected = new[] { new[] { 10d, 20, 30, 50, 100 }, equalQuartilesAtMaximum
            ? new[] { 100d, 100, 100, 100, 100 } : new[] { 20d, 25, 40, 60, 80 } };
        var groups = document.Descendants().Where(element => Role(element) == "point").ToArray();
        Assert.Equal(2, groups.Length);
        var keys = new[] { "minimum", "q1", "median", "q3", "maximum" };
        for (var point = 0; point < groups.Length; point++) {
            Assert.Equal("1", (string?)groups[point].Attribute("data-cfx-source-count"));
            Assert.Equal(point, Number(groups[point], "data-cfx-source-point"));
            Assert.Equal(point + 1, Number(groups[point], "data-cfx-x"));
            for (var member = 0; member < 5; member++) {
                Assert.Equal(expected[point][member], Number(groups[point], "data-cfx-" + keys[member]));
            }
        }
    }

    private static PaintFootprint[] PaintBounds(XDocument document) => document.Descendants()
        .Where(element => Role(element) is "boxplot-body" or "boxplot-whisker" or "boxplot-cap" or "boxplot-median")
        .Select(element => {
            var clip = PlotClip(document, element);
            var halfStroke = (string?)element.Attribute("stroke") is null or "none" ? 0 : Number(element, "stroke-width") / 2;
            var rectangle = element.Name.LocalName == "rect";
            var left = rectangle ? Number(element, "x") : Math.Min(Number(element, "x1"), Number(element, "x2"));
            var top = rectangle ? Number(element, "y") : Math.Min(Number(element, "y1"), Number(element, "y2"));
            var right = rectangle ? left + Number(element, "width") : Math.Max(Number(element, "x1"), Number(element, "x2"));
            var bottom = rectangle ? top + Number(element, "height") : Math.Max(Number(element, "y1"), Number(element, "y2"));
            return new PaintFootprint(Role(element)!, left - halfStroke, top - halfStroke, right + halfStroke, bottom + halfStroke,
                Number(clip, "x"), Number(clip, "y"), Number(clip, "x") + Number(clip, "width"), Number(clip, "y") + Number(clip, "height"));
        }).ToArray();

    private static XElement PlotClip(XDocument document, XElement mark) {
        var clippedAncestor = mark.AncestorsAndSelf().First(element => element.Attribute("clip-path") is not null);
        var reference = clippedAncestor.Attribute("clip-path")!.Value;
        Assert.StartsWith("url(#", reference); Assert.EndsWith(")", reference);
        var clip = Assert.Single(document.Descendants(), element => (string?)element.Attribute("id") == reference[5..^1]);
        return Assert.Single(clip.Elements(), element => element.Name.LocalName == "rect");
    }

    private static void Capture(string name, PreparedVisual prepared, string svg, PaintFootprint[] paint, XDocument document) {
        var directory = Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, name + ".svg"), svg);
        File.WriteAllBytes(Path.Combine(directory, name + ".png"), prepared.ToPng());
        File.WriteAllText(Path.Combine(directory, name + "-bounds.json"), JsonSerializer.Serialize(new {
            paint, statistics = document.Descendants().Where(element => Role(element) == "point")
                .Select(element => element.Attributes().ToDictionary(attribute => attribute.Name.LocalName, attribute => attribute.Value))
        }, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static string? Role(XElement element) => (string?)element.Attribute("data-cfx-role");
    private static double Number(XElement element, string attribute) => double.Parse(element.Attribute(attribute)!.Value, CultureInfo.InvariantCulture);
    private sealed record PaintFootprint(string Role, double Left, double Top, double Right, double Bottom,
        double ClipLeft, double ClipTop, double ClipRight, double ClipBottom);
}
