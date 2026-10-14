using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects one numeric color contract across matrix, hexagonal and dated caller paths.</summary>
public sealed class HeatmapColorScaleTests {
    private static readonly ChartColor Low = ChartColor.FromHex("#DAE8F8"), High = ChartColor.FromHex("#1C5CAB");

    [Theory]
    [InlineData(ChartSeriesKind.Heatmap)]
    [InlineData(ChartSeriesKind.HexbinHeatmap)]
    [InlineData(ChartSeriesKind.CalendarHeatmap)]
    public void FixedDomainAndPointOverridesKeepRawValuesIndependentOfDatasetExtent(ChartSeriesKind kind) {
        var scale = ChartColorScale.Sequential(Low, High).WithValueRange(0, 20);
        var narrow = Create(kind, 0, 10, 20).WithHeatmapColorScale(scale);
        var wide = Create(kind, 0, 10, 80).WithHeatmapColorScale(scale);
        narrow.Series[0].Color = ChartColor.Black;
        wide.Series[0].WithPointColor(2, ChartColor.Black);
        var first = Cells(narrow); var second = Cells(wide);
        Assert.Equal(scale.ColorFor(10, 0, 20).ToHex(), Fill(first[1]));
        Assert.Equal(Fill(first[1]), Fill(second[1]));
        Assert.Equal(Low.ToHex(), Fill(first[0]));
        Assert.Equal(High.ToHex(), Fill(first[2]));
        Assert.Equal(ChartColor.Black.ToHex(), Fill(second[2]));
        Assert.Equal("80", (string?)second[2].Attribute("data-cfx-value"));
        Assert.All(first.Concat(second), cell => { Assert.Null(cell.Attribute("data-cfx-level")); Assert.Null(cell.Attribute("data-cfx-status")); });
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void BatchRowsShareTheirWholeObservedDomainIncludingZero(bool hex, bool typedRows) {
        var chart = Chart.Create(); var scale = ChartColorScale.Sequential(Low, High);
        var rows = new[] { ChartHeatmapRow.CreateMasked("First", 10, null, 30), ChartHeatmapRow.Create("Second", 0, 20) };
        if (typedRows) { if (hex) chart.AddHexbinHeatmapRows(rows); else chart.AddHeatmapRows(rows); }
        else {
            var values = new[] { new[] { 10d, 30 }, new[] { 0d, 20 } };
            if (hex) chart.AddHexbinHeatmapRows(new[] { "First", "Second" }, values);
            else chart.AddHeatmapRows(new[] { "First", "Second" }, values);
        }
        chart.Options.HeatmapColorScale = scale;
        chart.Series[0].WithPointColor(1, ChartColor.Black);
        var cells = Cells(chart);
        Assert.Equal(4, cells.Length);
        Assert.Equal(scale.ColorFor(10, 0, 30).ToHex(), Fill(cells[0]));
        Assert.Equal(ChartColor.Black.ToHex(), Fill(cells[1]));
        Assert.Equal(Low.ToHex(), Fill(cells[2]));
        if (typedRows) Assert.Equal("2", (string?)cells[1].Attribute("data-cfx-column"));
    }

    [Theory]
    [InlineData(ChartSeriesKind.Heatmap, 0)]
    [InlineData(ChartSeriesKind.HexbinHeatmap, 7)]
    [InlineData(ChartSeriesKind.CalendarHeatmap, 0)]
    public void ConstantDomainsUseTheCanonicalLowColorWithoutInventingBounds(ChartSeriesKind kind, double value) {
        var chart = Create(kind, value, value).WithHeatmapColorScale(ChartColorScale.Sequential(Low, High));
        var document = XDocument.Parse(chart.ToSvg());
        Assert.All(Cells(document), cell => Assert.Equal(Low.ToHex(), Fill(cell)));
        var key = document.Descendants().Single(element => (string?)element.Attribute("data-cfx-scale-mode") == "sequential");
        Assert.Equal(value.ToString(System.Globalization.CultureInfo.InvariantCulture), (string?)key.Attribute("data-cfx-min-value"));
        Assert.Equal((string?)key.Attribute("data-cfx-min-value"), (string?)key.Attribute("data-cfx-max-value"));
    }

    [Theory]
    [InlineData(ChartSeriesKind.Heatmap)]
    [InlineData(ChartSeriesKind.HexbinHeatmap)]
    [InlineData(ChartSeriesKind.CalendarHeatmap)]
    public void NullRestoresExistingPaintLegendAndMetadataAndPreparedScenesStayDetached(ChartSeriesKind kind) {
        var chart = Create(kind, 0, 10, 20).WithSize(420, 300).WithDataLabels(false);
        var originalSvg = chart.ToSvg(); var originalPng = chart.ToPng();
        chart.WithHeatmapColorScale(ChartColorScale.Sequential(Low, High));
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var svg = prepared.ToSvg(); var png = prepared.ToPng(); var regions = prepared.Regions.Select(region => region.Label).ToArray();
        Assert.NotEqual(originalSvg, chart.ToSvg());
        chart.WithHeatmapColorScale(null);
        Assert.Equal(originalSvg, chart.ToSvg()); Assert.Equal(originalPng, chart.ToPng());
        if (kind == ChartSeriesKind.CalendarHeatmap) chart.Series[0].UpdateCalendarItem(0, new ChartCalendarHeatmapItem(chart.Series[0].CalendarItems[0].Date, 99));
        else chart.Series[0].Points[0] = new ChartPoint(chart.Series[0].Points[0].X, 99);
        chart.Series[0].WithPointColor(0, ChartColor.Black);
        chart.ConfigureLabels(labels => labels.NoData = "Changed");
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
        Assert.Equal(regions, prepared.Regions.Select(region => region.Label));
    }

    [Fact]
    public void ExplicitScaleOverridesLegacyCountAndSemanticOptionsUntilCleared() {
        var chart = Chart.Create().AddHourWeekdayHeatmap(new[] { new ChartTimedValue(new DateTime(2026, 1, 5, 10, 0, 0, DateTimeKind.Utc)) })
            .WithHeatmapScale(ChartHeatmapScale.Semantic).WithHeatmapColorScale(ChartColorScale.Sequential(Low, High).WithValueRange(0, 1));
        Assert.True(chart.Options.HeatmapRelativeScale);
        var cells = Cells(chart);
        Assert.Equal(168, cells.Length);
        Assert.Equal(Low.ToHex(), Fill(cells.First(cell => (string?)cell.Attribute("data-cfx-value") == "0")));
        Assert.Equal(High.ToHex(), Fill(cells.Single(cell => (string?)cell.Attribute("data-cfx-value") == "1")));
        Assert.All(cells, cell => { Assert.Null(cell.Attribute("data-cfx-level")); Assert.Null(cell.Attribute("data-cfx-status")); });
        chart.WithHeatmapColorScale(null);
        Assert.Throws<InvalidOperationException>(() => chart.ToSvg());
        Assert.Throws<InvalidOperationException>(() => chart.ToPng());
    }

    [Fact]
    public void NumericScaleRejectsCategoricalAndUnrelatedFamiliesWithoutChangingTheirDefaults() {
        var scale = ChartColorScale.Sequential(Low, High);
        var categorical = Chart.Create().WithStateCategories(new ChartStateCategory("ready", "Ready", Low))
            .AddHeatmapCategoryRow("States", new ChartHeatmapCell?[] { new("ready"), null });
        var before = categorical.ToSvg();
        Assert.Null(categorical.Options.HeatmapColorScale);
        categorical.WithHeatmapColorScale(scale);
        Assert.Throws<InvalidOperationException>(() => categorical.ToSvg());
        Assert.Throws<InvalidOperationException>(() => categorical.ToPng());
        categorical.WithHeatmapColorScale(null);
        Assert.Equal(before, categorical.ToSvg());
        var line = Chart.Create().AddLine("Unrelated", new[] { new ChartPoint(1, 1), new ChartPoint(2, 2) }).WithHeatmapColorScale(scale);
        Assert.Throws<InvalidOperationException>(() => line.Prepare(VisualExportRequest.ForChart(line).Context));
        var calendar = Create(ChartSeriesKind.CalendarHeatmap, 1, 2).WithHeatmapColorScale(scale);
        var calendarBefore = calendar.ToSvg();
        Assert.Throws<ArgumentOutOfRangeException>(() => calendar.Series[0].UpdateCalendarItem(0, new ChartCalendarHeatmapItem(calendar.Series[0].CalendarItems[0].Date, -1)));
        Assert.Equal(calendarBefore, calendar.ToSvg());
    }

    internal static Chart Create(ChartSeriesKind kind, params double[] values) {
        var chart = Chart.Create().WithDataLabels(false).WithPngSupersampling(1);
        if (kind == ChartSeriesKind.CalendarHeatmap)
            return chart.AddCalendarHeatmap("Observed", values.Select((value, index) => new ChartCalendarHeatmapItem(new DateTime(2026, 1, 5).AddDays(index), value)));
        return kind == ChartSeriesKind.Heatmap ? chart.AddHeatmapRow("Observed", values) : chart.AddHexbinHeatmapRow("Observed", values);
    }
    internal static XElement[] Cells(Chart chart) => Cells(XDocument.Parse(chart.ToSvg()));
    internal static XElement[] Cells(XDocument document) => document.Descendants().Where(element =>
        (string?)element.Attribute("data-cfx-role") is "heatmap-cell" or "hexbin-cell"
        || ((string?)element.Attribute("data-cfx-role") == "calendar-cell" && (string?)element.Attribute("data-cfx-empty") == "false")).ToArray();
    internal static string? Fill(XElement cell) => cell.Descendants().Single(element =>
        (string?)element.Attribute("data-cfx-role") is "heatmap-cell-shape" or "hexbin-cell-shape" or "calendar-cell-shape").Attribute("fill")?.Value;
}
