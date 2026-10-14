using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class BoxPlotCapacityTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DenseGroupsRejectOversizedStrokesAndKeepEverySummaryWhenTheStrokeFits(bool horizontal) {
        var chart = Chart.Create().WithHeader(false).WithAxes(false).WithLegend(false).WithDataLabels(false)
            .ConfigureBoxPlot(options => options.Orientation = horizontal ? ChartOrientation.Horizontal : ChartOrientation.Vertical);
        for (var series = 0; series < 2; series++) {
            chart.AddBoxPlot("Group " + series, Enumerable.Range(1, 30).Select(category => new ChartBoxPlot(category, 10, 12, 15, 18, 20)));
            chart.Series[series].StrokeWidth = 8;
        }
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(horizontal ? 800 : 400, horizontal ? 400 : 800), 0));
        Assert.Equal("The category slot cannot fit the mark's minimum painted footprint. Increase category-axis space, reduce grouped series, or use a simpler mark style.",
            Assert.Throws<InvalidOperationException>(() => chart.Prepare(context)).Message);

        foreach (var series in chart.Series) series.StrokeWidth = 2;
        var prepared = chart.Prepare(context); var svg = prepared.ToSvg(); var document = XDocument.Parse(svg);
        var groups = document.Descendants().Where(node => Role(node) == "point").ToArray();
        Assert.Equal(60, groups.Length); Assert.Equal(60, prepared.Regions.Count(region => region.Role == "point"));
        Assert.Equal(Enumerable.Range(0, 2).SelectMany(series => Enumerable.Range(0, 30).Select(point => (series, point))),
            groups.Select(group => ((int)Number(group, "data-cfx-series"), (int)Number(group, "data-cfx-point"))));

        var observations = groups.Select(group => {
            var point = (int)Number(group, "data-cfx-point");
            Assert.Equal(point + 1, Number(group, "data-cfx-x"));
            Assert.Equal("1", (string?)group.Attribute("data-cfx-source-count"));
            Assert.Equal(point, Number(group, "data-cfx-source-point"));
            var paint = group.Descendants().Where(node => Role(node) is "boxplot-body" or "boxplot-whisker" or "boxplot-cap" or "boxplot-median")
                .Select(node => PaintBounds(document, node)).ToArray();
            Assert.Equal(5, paint.Length);
            Assert.Single(paint, part => part.Role == "boxplot-body");
            Assert.Single(paint, part => part.Role == "boxplot-whisker");
            Assert.Single(paint, part => part.Role == "boxplot-median");
            Assert.Equal(2, paint.Count(part => part.Role == "boxplot-cap"));
            return paint;
        }).ToArray();
        Capture(horizontal ? "horizontal" : "vertical", prepared, svg, observations);

        foreach (var paint in observations.SelectMany(parts => parts)) {
            Assert.NotEmpty(paint.Clips);
            foreach (var clip in paint.Clips) Assert.True(paint.Left >= clip.Left - .003 && paint.Right <= clip.Right + .003
                && paint.Top >= clip.Top - .003 && paint.Bottom <= clip.Bottom + .003,
                "Every body, whisker, cap and median stroke must survive every actual plot clip: " + JsonSerializer.Serialize(paint));
        }
        var footprints = observations.Select(parts => (
            Minimum: parts.Min(part => horizontal ? part.Top : part.Left),
            Maximum: parts.Max(part => horizontal ? part.Bottom : part.Right))).OrderBy(bounds => bounds.Minimum).ToArray();
        for (var item = 1; item < footprints.Length; item++) Assert.True(footprints[item - 1].Maximum < footprints[item].Minimum,
            "Grouped summaries must have disjoint category paint, including all stroke widths.");
    }

    private static PaintFootprint PaintBounds(XDocument document, XElement mark) {
        var radius = (string?)mark.Attribute("stroke") is null or "none" ? 0 : Number(mark, "stroke-width") / 2;
        var rectangle = mark.Name.LocalName == "rect";
        var left = rectangle ? Number(mark, "x") : Math.Min(Number(mark, "x1"), Number(mark, "x2"));
        var top = rectangle ? Number(mark, "y") : Math.Min(Number(mark, "y1"), Number(mark, "y2"));
        var right = rectangle ? left + Number(mark, "width") : Math.Max(Number(mark, "x1"), Number(mark, "x2"));
        var bottom = rectangle ? top + Number(mark, "height") : Math.Max(Number(mark, "y1"), Number(mark, "y2"));
        var clips = mark.AncestorsAndSelf().Where(node => node.Attribute("clip-path") != null).Select(node => {
            var reference = node.Attribute("clip-path")!.Value;
            Assert.StartsWith("url(#", reference); Assert.EndsWith(")", reference);
            var definition = Assert.Single(document.Descendants(), candidate => candidate.Name.LocalName == "clipPath"
                && (string?)candidate.Attribute("id") == reference[5..^1]);
            var clip = Assert.Single(definition.Elements()); Assert.Equal("rect", clip.Name.LocalName);
            return new ClipFootprint(Number(clip, "x"), Number(clip, "y"), Number(clip, "x") + Number(clip, "width"), Number(clip, "y") + Number(clip, "height"));
        }).ToArray();
        return new PaintFootprint(Role(mark)!, left - radius, top - radius, right + radius, bottom + radius, clips);
    }

    private static void Capture(string name, PreparedVisual prepared, string svg, PaintFootprint[][] observations) {
        var directory = Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "capacity-" + name + ".svg"), svg);
        File.WriteAllBytes(Path.Combine(directory, "capacity-" + name + ".png"), prepared.ToPng());
        File.WriteAllText(Path.Combine(directory, "capacity-" + name + "-bounds.json"),
            JsonSerializer.Serialize(observations, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static string? Role(XElement element) => (string?)element.Attribute("data-cfx-role");
    private static double Number(XElement element, string attribute) => double.Parse(element.Attribute(attribute)!.Value, CultureInfo.InvariantCulture);
    private sealed record PaintFootprint(string Role, double Left, double Top, double Right, double Bottom, ClipFootprint[] Clips);
    private sealed record ClipFootprint(double Left, double Top, double Right, double Bottom);
}
