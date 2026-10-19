using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Automatic authored point captions retain full names before the existing shortening fallback.</summary>
public sealed class AutomaticPointCaptionWrappingTests {
    private const string Caption = "Regional processing checkpoint North America";

    [Theory]
    [InlineData(ChartSeriesKind.Scatter)]
    [InlineData(ChartSeriesKind.Dumbbell)]
    public void CompactAutomaticCaptionKeepsCompleteAuthoredNameAndSourceFacts(ChartSeriesKind kind) {
        var chart = Create(kind, 220);
        var prepared = Prepare(chart);
        var caption = Assert.Single(Captions(prepared));
        Assert.Equal(2, caption.Text.Lines.Count);
        Assert.Equal(Caption, string.Join(" ", caption.Text.Lines.Select(line => line.Text)));
        Assert.Equal(12, caption.Text.Size);
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code == "cartesian.data-label-overflow");
        var plot = Assert.Single(prepared.Regions, region => region.Role == "cartesian-plot").Bounds;
        Assert.True(caption.X >= plot.Left && caption.X + caption.Text.Metrics.Width <= plot.Right);
        Assert.True(caption.Baseline - caption.Text.Metrics.LineHeight >= plot.Top);
        Assert.True(caption.Baseline - caption.Text.Metrics.LineHeight + caption.Text.Metrics.Height <= plot.Bottom);
        var source = Assert.Single(XDocument.Parse(prepared.ToSvg()).Descendants(),
            node => (string?)node.Attribute("data-cfx-role") == "point");
        Assert.Equal(Caption, (string?)source.Attribute("data-cfx-label"));
        Assert.Equal("region-north", (string?)source.Attribute("data-cfx-observation-id"));
        Assert.Equal(Caption, chart.Series[0].PointLabels[0]);
        var svg = prepared.ToSvg(); var png = prepared.ToPng();
        chart.Series[0].WithPointLabel(0, "Changed");
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
    }

    [Fact]
    public void FullSingleLineCaptionRetainsFirstChoiceWhenItFits() {
        var prepared = Prepare(Create(ChartSeriesKind.Scatter, 390));
        Assert.Equal(Caption, Assert.Single(Assert.Single(Captions(prepared)).Text.Lines).Text);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExplicitPlacementAndFormattedNumericValuesKeepSingleLinePolicy(bool numeric) {
        var chart = Create(ChartSeriesKind.Scatter, 220);
        if (numeric) {
            chart.Series[0].UseFormattedPointLabel(0);
            chart.Options.ValueFormatter = _ => Caption;
        } else chart.Series[0].DataLabelPlacement = ChartDataLabelPlacement.Above;
        var prepared = Prepare(chart);
        var caption = Assert.Single(Assert.Single(Captions(prepared)).Text.Lines).Text;
        Assert.EndsWith("…", caption);
        Assert.Contains(prepared.Diagnostics, diagnostic => diagnostic.Code == "cartesian.data-label-overflow");
    }

    [Fact]
    public void WrappedFallbackRemeasuresBothLinesAndStillRejectsMarkCollisions() {
        var style = new TextStyle { Font = Font(), FontSize = 12, LineHeight = 1.2 };
        var service = new LabelPlacementService();
        var request = new LabelPlacementRequest(Caption, new ChartPoint(0, 0), style, new[] { new LabelCandidate(0, 0) }) {
            AllowWrapping = true, MeasuredSize = service.Measure(Caption, style), Fallback = LabelFallbackRule.Drop
        };
        var scene = new ChartRect(0, 0, 175, 80);
        var full = Assert.Single(service.Place(new[] { request }, scene));
        Assert.False(full.IsDropped); Assert.False(full.IsEllipsized);
        Assert.Contains('\n', full.Text);
        var measured = service.Measure(full.Text, style);
        Assert.Equal(measured.Width, full.Bounds.Width, 8);
        Assert.Equal(measured.Height, full.Bounds.Height, 8);
        var blocked = Assert.Single(service.Place(new[] { request }, scene,
            new[] { new LabelObstacle("other-mark", new ChartRect(0, measured.LineHeight, 175, measured.LineHeight)) }));
        Assert.True(blocked.IsDropped);
        Assert.Equal(Caption, blocked.Request.Text);
    }

    [Fact]
    public void OrdinaryPublicPlacementDoesNotIntroduceAutomaticWrapping() {
        var request = new LabelPlacementRequest(Caption, new ChartPoint(0, 0),
            new TextStyle { Font = Font(), FontSize = 12 }, new[] { new LabelCandidate(0, 0) });
        var placed = Assert.Single(new LabelPlacementService().Place(new[] { request }, new ChartRect(0, 0, 175, 80)));
        Assert.True(placed.IsEllipsized); Assert.DoesNotContain('\n', placed.Text);
    }

    private static Chart Create(ChartSeriesKind kind, int width) {
        var chart = Chart.Create().WithSize(width, 240).WithTheme(VisualTheme.Graphite())
            .WithHeader(false).WithLegend(false).WithAxes(false).WithGrid(false).WithDataLabels()
            .WithXAxisBounds(0, 10).WithYAxisBounds(0, 10);
        var series = kind == ChartSeriesKind.Scatter
            ? ChartSeries.CreateScatter(new ChartSeriesId("observed"), "Observed", new[] { new ChartPoint(5, 5) })
            : ChartSeries.CreateDumbbell(new ChartSeriesId("observed"), "Observed", new[] { new ChartDumbbell(5, 4, 5) });
        series.WithObservationId(0, new ChartObservationId("region-north")).WithPointLabel(0, Caption);
        return chart.AddSeries(series);
    }

    private static FontSpec Font() => new() { Family = "Carlito", FilePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf") };
    private static PreparedVisual Prepare(Chart chart) => chart.Prepare(new VisualRenderContext(
        new VisualLayoutOptions(new VisualSize(chart.Options.Size.Width, chart.Options.Size.Height), chart.Options.Padding),
        chart.Options.Theme, VisualThemeMode.Light, new VisualFrame(showLegend: false), Font()));
    private static VisualSceneText[] Captions(PreparedVisual prepared) => prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "data-label").ToArray();
}
