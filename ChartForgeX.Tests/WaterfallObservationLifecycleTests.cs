using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class WaterfallObservationLifecycleTests {
    [Fact]
    public void UpdatesPreserveAuthoredIdentityAndPresentationAcrossCheckpointChanges() {
        var series = Create().WithObservationId(1, new ChartObservationId("stage"))
            .WithObservationId(2, new ChartObservationId("late-change"))
            .WithPointColor(1, "#B34C89").WithPointFillPattern(1, ChartFillPattern.Crosshatch)
            .WithPointLabel(1, "Retained caption").WithState(ChartSeriesState.Warning);
        var chart = Chart.Create().AddSeries(series);
        var prepared = chart.Prepare(Context()); var svg = prepared.ToSvg(); var png = prepared.ToPng(new VisualRenderOptions(supersampling: 1));
        series.UpdateWaterfallItem(1, ChartWaterfallItem.Delta(2, -4));
        Assert.Equal(3, series.ObservationCount); Assert.Equal(3, series.SourcePointCount);
        Assert.Equal("stage", series.ObservationIds[1].Value); Assert.Equal("movement", series.Id.Value);
        Assert.Equal("Retained caption", series.PointLabels[1]); Assert.Equal(ChartColor.FromHex("#B34C89"), series.PointColors[1]);
        Assert.Equal(ChartFillPattern.Crosshatch, series.PointFillPatterns[1]); Assert.Equal(ChartSeriesState.Warning, series.StateRole);
        var updated = Marks(chart);
        Assert.Equal("stage", (string?)updated[1].Attribute("data-cfx-observation-id"));
        Assert.Equal("late-change", (string?)updated[2].Attribute("data-cfx-observation-id"));
        Assert.Equal("6", (string?)updated[2].Attribute("data-cfx-start"));
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
        series.UpdateWaterfallItem(1, ChartWaterfallItem.Total(2));
        Assert.Equal(2, series.SourcePointCount); Assert.Equal("stage", (string?)Marks(chart)[1].Attribute("data-cfx-observation-id"));
    }

    [Fact]
    public void FailedUpdatesAndReplacementsLeaveTheCompleteSourceAndMetadataIntact() {
        var series = Create().WithObservationId(2, new ChartObservationId("last")).WithPointColor(2, "#B34C89");
        var chart = Chart.Create().AddSeries(series); var before = chart.Prepare(Context()).ToSvg();
        Assert.Throws<ArgumentException>(() => series.UpdateWaterfallItem(2, ChartWaterfallItem.Total(1)));
        Assert.Throws<ArgumentException>(() => series.UpdateWaterfallItem(1, ChartWaterfallItem.OpeningBalance(2, 50)));
        Assert.Throws<ArgumentException>(() => series.UpdateWaterfallItem(1, null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => series.UpdateWaterfallItem(3, ChartWaterfallItem.Total(4)));
        Assert.Throws<ArgumentException>(() => series.ReplaceWaterfallItems(new[] { ChartWaterfallItem.Delta(1, 2), ChartWaterfallItem.Total(1) }));
        Assert.Throws<ArgumentException>(() => series.ReplaceWaterfallItems(new[] { ChartWaterfallItem.Total(1), ChartWaterfallItem.OpeningBalance(2, 10) }));
        Assert.Throws<ArgumentException>(() => series.ReplaceWaterfallItems(new ChartWaterfallItem[] { null! }));
        Assert.Throws<ArgumentNullException>(() => series.ReplaceWaterfallItems(null!));
        Assert.Throws<InvalidOperationException>(() => series.ReplaceWaterfallItems(InterruptedSource()));
        Assert.Equal(before, chart.Prepare(Context()).ToSvg());
        Assert.Equal("last", series.ObservationIds[2].Value); Assert.Equal(ChartColor.FromHex("#B34C89"), series.PointColors[2]);
    }

    [Fact]
    public void ReplacementClearsObservationOverridesRetainsSeriesSettingsAndSnapshotsInput() {
        var series = Create().WithObservationId(0, new ChartObservationId("old"))
            .WithPointLabel(0, "Old label").WithPointColor(0, "#B34C89").WithPointFillPattern(0, ChartFillPattern.Crosshatch)
            .ConfigurePointDataLabelStyle(0, style => style.WithColor("#FFFFFF"))
            .WithName("Localized movement").WithState(ChartSeriesState.Warning).UseSecondaryValueAxis();
        series.Color = ChartColor.FromHex("#123456");
        var input = new[] { ChartWaterfallItem.OpeningBalance(5, 12), ChartWaterfallItem.Total(6) };
        series.ReplaceWaterfallItems(input); input[0] = ChartWaterfallItem.Delta(5, 999);
        Assert.Equal(12, series.WaterfallItems[0].Value); Assert.Equal(1, series.SourcePointCount); Assert.Equal(2, series.ObservationCount);
        Assert.Equal("movement", series.Id.Value); Assert.Equal("Localized movement", series.Name);
        Assert.Equal(ChartSeriesState.Warning, series.StateRole); Assert.Equal(ChartAxisSide.Secondary, series.ValueAxis);
        Assert.Equal(ChartColor.FromHex("#123456"), series.Color);
        Assert.Empty(series.ObservationIds); Assert.Empty(series.PointLabels); Assert.Empty(series.PointColors);
        Assert.Empty(series.PointFillPatterns); Assert.Empty(series.PointDataLabelStyles);
        Assert.Throws<NotSupportedException>(() => ((IList<ChartWaterfallItem>)series.WaterfallItems)[0] = input[0]);
    }

    [Fact]
    public void EmptyTypedFactoryAndReplacementRemainReadOnlyAndRefillWithoutAnInventedTotal() {
        var series = ChartSeries.CreateWaterfall(new ChartSeriesId("empty"), "Empty", Array.Empty<ChartWaterfallItem>());
        var chart = Chart.Create().AddSeries(series);
        Assert.Empty(series.Points); Assert.True(series.Points.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => series.Points.Add(new ChartPoint(1, 20)));
        Assert.Equal(0, series.ObservationCount); Assert.Equal(0, series.SourcePointCount);
        var empty = chart.Prepare(Context());
        Assert.Contains(empty.Diagnostics, diagnostic => diagnostic.Code == "cartesian.no-data");
        Assert.Empty(Marks(chart)); Assert.NotEmpty(empty.ToPng(new VisualRenderOptions(supersampling: 1)));
        series.ReplaceWaterfallItems(new[] { ChartWaterfallItem.Delta(1, 20) });
        var mark = Assert.Single(Marks(chart));
        Assert.Equal("delta", (string?)mark.Attribute("data-cfx-waterfall-kind"));
        series.WithObservationId(0, new ChartObservationId("filled")).ReplaceWaterfallItems(Array.Empty<ChartWaterfallItem>());
        Assert.Empty(series.ObservationIds); Assert.Empty(Marks(chart)); Assert.True(series.Points.IsReadOnly);
        series.ReplaceWaterfallItems(new[] { ChartWaterfallItem.Total(2) });
        Assert.Equal(1, series.ObservationCount); Assert.Equal(0, series.SourcePointCount);
        Assert.Equal("total", (string?)Assert.Single(Marks(chart)).Attribute("data-cfx-waterfall-kind"));
    }

    [Fact]
    public void ReplacingNumericInputSwitchesOwnershipOnlyAfterSuccessfulValidation() {
        var series = new ChartSeries(new ChartSeriesId("numeric"), "Numeric", ChartSeriesKind.Waterfall, new[] { new ChartPoint(1, 10) })
            .WithObservationId(0, new ChartObservationId("numeric-source"));
        Assert.Throws<ArgumentException>(() => series.ReplaceWaterfallItems(new[] { ChartWaterfallItem.Total(1), ChartWaterfallItem.Delta(1, 2) }));
        Assert.False(series.Points.IsReadOnly); Assert.Single(series.Points); Assert.Single(series.ObservationIds);
        series.ReplaceWaterfallItems(Array.Empty<ChartWaterfallItem>());
        Assert.True(series.Points.IsReadOnly); Assert.Empty(series.Points); Assert.Empty(series.ObservationIds);
        Assert.Empty(Marks(Chart.Create().AddSeries(series)));
        var line = ChartSeries.CreateLine(new ChartSeriesId("line"), "Line", new[] { new ChartPoint(1, 2) });
        Assert.Throws<InvalidOperationException>(() => line.ReplaceWaterfallItems(Array.Empty<ChartWaterfallItem>()));
        Assert.Throws<InvalidOperationException>(() => line.UpdateWaterfallItem(0, ChartWaterfallItem.Delta(1, 2)));
    }

    [Fact]
    public void AuthoredItemIdsRemainDistinctFromCompactNumericContributorOrdinalsInExportsAndLegends() {
        var series = Create().WithObservationId(0, new ChartObservationId("first"))
            .WithObservationId(1, new ChartObservationId("checkpoint"))
            .WithObservationId(2, new ChartObservationId("last"));
        var chart = Chart.Create().WithPointLegend().AddSeries(series);
        var marks = Marks(chart);
        Assert.Equal(new[] { "first", "checkpoint", "last" }, marks.Select(mark => (string?)mark.Attribute("data-cfx-observation-id")));
        Assert.Equal(new[] { "0", "1", "2" }, marks.Select(mark => (string?)mark.Attribute("data-cfx-observation-index")));
        Assert.Equal("1", (string?)marks[2].Attribute("data-cfx-source-point"));
        Assert.Equal("0", (string?)marks[1].Attribute("data-cfx-source-points"));
        Assert.Equal("waterfall-subtotal", (string?)marks[1].Attribute("data-cfx-derived"));
        Assert.Null(marks[1].Attribute("data-cfx-source-point"));
        var legends = XDocument.Parse(chart.Prepare(Context()).ToSvg()).Descendants()
            .Where(node => (string?)node.Attribute("data-cfx-role") == "legend-entry").ToArray();
        Assert.Equal(new[] { "first", "checkpoint", "last" }, legends.Select(mark => (string?)mark.Attribute("data-cfx-observation-id")));
    }

    private static ChartSeries Create() => ChartSeries.CreateWaterfall(new ChartSeriesId("movement"), "Movement", new[] {
        ChartWaterfallItem.Delta(1, 10), ChartWaterfallItem.Subtotal(2), ChartWaterfallItem.Delta(3, 20)
    });
    private static IEnumerable<ChartWaterfallItem> InterruptedSource() {
        yield return ChartWaterfallItem.Delta(8, 10);
        throw new InvalidOperationException("Interrupted source");
    }
    private static VisualRenderContext Context() => new(new VisualLayoutOptions(new VisualSize(500, 320)), VisualTheme.Graphite());
    private static XElement[] Marks(Chart chart) => XDocument.Parse(chart.Prepare(Context()).ToSvg()).Descendants()
        .Where(node => node.Attribute("data-cfx-waterfall-kind") != null && node.Attribute("data-cfx-point") != null
            && (string?)node.Attribute("data-cfx-role") != "legend-entry").ToArray();
}
