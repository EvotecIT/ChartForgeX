using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class WrappedCaptionDiagnosticTests {
    [Theory]
    [InlineData(VisualThemeMode.Light)]
    [InlineData(VisualThemeMode.Dark)]
    public void CompletePyramidRailCaptionsDoNotReportTextLossWhenTheyWrap(VisualThemeMode mode) {
        var chart = Chart.Create().WithDataLabels().WithXLabels("Regional platform services in North America", "Regional platform services in South America", "Regional platform services in Western Europe")
            .AddPyramid("Allocation", new[] { new ChartPoint(1, 50), new ChartPoint(2, 30), new ChartPoint(3, 20) });
        chart.Options.DataLabelPlacement = ChartDataLabelPlacement.Right;
        var prepared = chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(390, 420), 14),
            VisualTheme.Graphite(), mode, new VisualFrame("", "", showLegend: false), Font()));
        var captions = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "pyramid-label").ToArray();
        Assert.Equal(3, captions.Length);
        Assert.Contains(captions, caption => caption.Text.Lines.Count > 1);
        Assert.Equal(new[] { "Regional platform services in North America: 50", "Regional platform services in South America: 30", "Regional platform services in Western Europe: 20" },
            captions.Select(caption => string.Join(" ", caption.Text.Lines.Select(line => line.Text))));
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code == "text.overflow");
        ScatterBubbleColorRenderingTests.Capture(prepared, "pyramid-wrapped-diagnostics-" + mode);
        Assert.NotEmpty(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
    }

    [Theory]
    [InlineData("North America services", "North America\nservices", 260, 100, false, true)]
    [InlineData("North America services", "North America\nservices", 40, 100, true, true)]
    [InlineData("North America services", "North America", 260, 100, true, true)]
    [InlineData("North America services", "services North America", 260, 100, true, true)]
    [InlineData("North America services", "North America\nservices", 260, 12, true, false)]
    [InlineData("North America services", "North America\nservices", 0, 100, true, false)]
    [InlineData("North America services", "North America\nservices", 260, 0, true, false)]
    [InlineData("North America services", "", 260, 100, true, false)]
    [InlineData("", "", 0, 0, false, false)]
    public void SharedTextReportsActualContentLossWhileAcceptingWordPreservingReflow(string source, string displayed, double width, double height, bool overflow, bool drawn) {
        var font = Font();
        var builder = new VisualSceneBuilder(new VisualSize(300, 120), font);
        VisualStateSceneTools.Text(builder, source, new ChartRect(5, 5, width, height),
            new TextStyle { Font = font, FontSize = 13.5 }, "caption", "caption", displayedText: displayed);
        var scene = builder.Build();
        Assert.Equal(overflow, scene.Diagnostics.Any(diagnostic => diagnostic.Code == "text.overflow"));
        Assert.Equal(source, Assert.Single(scene.Regions).Label);
        if (!drawn) { Assert.Empty(scene.Nodes.OfType<VisualSceneText>()); return; }
        var caption = Assert.Single(scene.Nodes.OfType<VisualSceneText>());
        if (!overflow) Assert.Equal(displayed, string.Join("\n", caption.Text.Lines.Select(line => line.Text)));
        else if (width == 40) Assert.Contains("…", string.Join("\n", caption.Text.Lines.Select(line => line.Text)));
    }

    [Theory]
    [InlineData(2, VisualThemeMode.Light)]
    [InlineData(12, VisualThemeMode.Light)]
    [InlineData(12, VisualThemeMode.Dark)]
    public void MatrixRowsRetainCompleteMultilineNamesAndDiagnoseOmittedCaptions(int count, VisualThemeMode mode) {
        const string source = "North\nAmerica\nservices";
        var chart = Chart.Create().WithLegend(false).ConfigureTickLabelStyle(style => style.WithFontSize(13.5));
        chart.Options.XAxis.Visible = false;
        for (var index = 0; index < count; index++) chart.AddHeatmapRow(source, new[] { new ChartPoint(1, index + 1) });
        var prepared = chart.Prepare(Context(mode));
        ScatterBubbleColorRenderingTests.Capture(prepared, "matrix-caption-diagnostics-" + count + "-" + mode);
        var regions = prepared.Regions.Where(region => region.Role == "heatmap-row-label").ToArray();
        Assert.Equal(count, regions.Length);
        Assert.All(regions, region => Assert.Equal(source, region.Label));
        var captions = Captions(prepared, "heatmap-row-label");
        if (count == 2) {
            Assert.Equal(count, captions.Length);
            Assert.All(captions, caption => Assert.Equal(source, CaptionText(caption)));
            Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code == "text.overflow");
        } else {
            Assert.Empty(captions);
            Assert.Equal(count, prepared.Diagnostics.Count(diagnostic => diagnostic.Code == "text.overflow"));
        }
        Assert.Contains("North", prepared.ToSvg());
        Assert.NotEmpty(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
    }

    [Theory]
    [InlineData(ChartSeriesKind.GanttLane, VisualThemeMode.Light)]
    [InlineData(ChartSeriesKind.GanttLane, VisualThemeMode.Dark)]
    [InlineData(ChartSeriesKind.StateTimeline, VisualThemeMode.Light)]
    [InlineData(ChartSeriesKind.StateTimeline, VisualThemeMode.Dark)]
    public void ScheduleRowsKeepFullSourceNamesWhenTheirVisibleCaptionsAreShortened(ChartSeriesKind kind, VisualThemeMode mode) {
        const string source = "North America regional platform services and infrastructure ownership";
        var chart = Chart.Create().WithLegend(false);
        if (kind == ChartSeriesKind.GanttLane) chart.AddGanttLane(source, new[] { new ChartGanttLaneItem(1, 3, "active") });
        else chart.AddStateTimelineLane(source, new[] { new ChartStateTimelineSegment(1, 3, "active") });
        var prepared = chart.Prepare(Context(mode));
        ScatterBubbleColorRenderingTests.Capture(prepared, "schedule-caption-diagnostics-" + kind + "-" + mode);
        var caption = Assert.Single(Captions(prepared, "schedule-row-label"));
        var displayed = CaptionText(caption);
        Assert.NotEqual(source, displayed);
        Assert.EndsWith("…", displayed);
        Assert.Equal(source, Assert.Single(prepared.Regions, region => region.Role == "schedule-row-label").Label);
        Assert.Contains(prepared.Diagnostics, diagnostic => diagnostic.Code == "text.overflow");
        Assert.NotEmpty(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
    }

    private static XElement[] Captions(PreparedVisual prepared, string role) => XDocument.Parse(prepared.ToSvg())
        .Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == role).ToArray();

    private static string CaptionText(XElement caption) => string.Join("\n", caption.Descendants()
        .Where(element => element.Name.LocalName == "text").Select(element => element.Value));

    private static VisualRenderContext Context(VisualThemeMode mode) => new(
        new VisualLayoutOptions(new VisualSize(390, 180), 14), VisualTheme.Graphite(), mode,
        new VisualFrame("", "", showLegend: false), Font());

    private static FontSpec Font() => FontSpec.FromFile(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf"));
}
