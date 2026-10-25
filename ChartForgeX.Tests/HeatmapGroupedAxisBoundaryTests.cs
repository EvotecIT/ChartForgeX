using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class HeatmapGroupedAxisBoundaryTests {
    [Fact]
    public void ExistingGroupPathsRemainCopiedAndBindingsPreserveActualCoordinatesWithoutAddingUnusedCatalogCells() {
        var parents = new[] { "First", "Work" };
        var chart = Chart.Create().AddHeatmapRow("Source", new[] { new ChartPoint(10, 0), new ChartPoint(90, 3) })
            .WithXLabels(new[] { new ChartAxisLabel(10, "Start", parents), new ChartAxisLabel(50, "Unused", "Unused", "Work"), new ChartAxisLabel(90, "End", "Last", "Work") });
        parents[0] = "Mutation";
        var plan = ChartCategoryGroupPlan.TryCreateMatrix(chart.Options.XAxis, new[] { 10d, 90d }, false)!;
        Assert.Equal(new[] { 10d, 90d }, plan.Catalog.Select(label => label.Value)); Assert.Equal("First", plan.Catalog[0].Groups[0]);
        Assert.Equal(new[] { (0d, 1d), (1d, 2d), (0d, 1d), (1d, 2d) }, plan.CreateSlotSpans().Select(span => (span.Start, span.End)));
        var xml = XDocument.Parse(chart.ToSvg()); Assert.Equal(2, HeatmapGroupedAxisTests.Cells(xml).Length);
        Assert.Equal(2, HeatmapGroupedAxisTests.ByRole(xml, "heatmap-column-label").Length); Assert.DoesNotContain("Unused", xml.Descendants().Where(element => element.Name.LocalName == "text").Select(element => element.Value));
    }

    [Fact]
    public void GroupedColumnsCanCoexistWithLaneHeadersButTwoRowGroupingContractsRejectClearly() {
        var chart = Chart.Create().WithXLabels(new[] { new ChartAxisLabel(1, "First", "Window"), new ChartAxisLabel(2, "Second", "Window") })
            .AddHeatmapCategoryRow("A", new ChartHeatmapCell?[] { new("pass"), null }, group: "Lane")
            .AddHeatmapCategoryRow("B", new ChartHeatmapCell?[] { new("pass"), new("pass") }, group: "Lane");
        Assert.NotEmpty(HeatmapGroupedAxisTests.ByRole(XDocument.Parse(chart.ToSvg()), "heatmap-row-group"));
        chart.Options.YAxis.Labels.AddRange(new[] { new ChartAxisLabel(1, "A", "Region"), new ChartAxisLabel(2, "B", "Region") });
        Assert.Contains("LaneGroup", Assert.Throws<NotSupportedException>(() => chart.ToSvg()).Message);
    }

    [Fact]
    public void CalendarHexMixedModelsMissingLeafMappingsAndNonlinearGroupedAxesKeepTheirExplicitBoundaries() {
        var calendar = Chart.Create().AddCalendarHeatmap("Calendar", new[] { new ChartCalendarHeatmapItem(new DateTime(2026, 1, 1), 2) })
            .WithXLabels(new[] { new ChartAxisLabel(1, "Day", "Month") });
        Assert.Throws<NotSupportedException>(() => calendar.ToSvg());
        var hex = Chart.Create().AddHexbinHeatmapRow("Hex", new[] { 1d, 2d }).WithXLabels(new[] { new ChartAxisLabel(1, "A", "Group"), new ChartAxisLabel(2, "B", "Group") });
        Assert.Throws<NotSupportedException>(() => hex.ToSvg());
        var mixed = Chart.Create().AddHeatmapCategoryRow("States", new ChartHeatmapCell("pass"), new ChartHeatmapCell("pass"))
            .AddHeatmapRow("Numeric", new[] { 1d, 2d }).WithXLabels(new[] { new ChartAxisLabel(1, "A", "Group"), new ChartAxisLabel(2, "B", "Group") });
        Assert.Contains("numeric rows or categorical rows", Assert.Throws<InvalidOperationException>(() => mixed.ToSvg()).Message);
        var missing = HeatmapGroupedAxisTests.Model(false, false, 1000); missing.Options.XAxis.Labels.RemoveAt(0);
        Assert.Throws<InvalidOperationException>(() => missing.ToSvg());
        var nonlinear = HeatmapGroupedAxisTests.Model(false, false, 1000); nonlinear.Options.YAxis.Scale = ChartScaleKind.Logarithmic;
        Assert.Throws<NotSupportedException>(() => nonlinear.ToPng());
    }

    [Fact]
    public void UnsortedAuthoredObservationsKeepTheirSourceOrderWhilePathsBindActualColumnCoordinates() {
        var chart = Chart.Create().AddHeatmapRow("Observed", new[] { new ChartPoint(90, 4), new ChartPoint(10, 0), new ChartPoint(70, 2) }, id: new ChartSeriesId("raw"))
            .WithXLabels(new[] { new ChartAxisLabel(10, "A", "First"), new ChartAxisLabel(70, "B", "Last"), new ChartAxisLabel(90, "C", "Last") });
        chart.Series[0].WithObservationIds(new[] { new ChartObservationId("authored-first"), new ChartObservationId("zero"), new ChartObservationId("authored-last") });
        var cells = HeatmapGroupedAxisTests.Cells(XDocument.Parse(chart.ToSvg()));
        Assert.Equal(new[] { "90", "10", "70" }, cells.Select(cell => (string?)cell.Attribute("data-cfx-x")));
        Assert.Equal(new[] { "authored-first", "zero", "authored-last" }, cells.Select(cell => (string?)cell.Attribute("data-cfx-observation-id")));
        Assert.Equal(new[] { "Last", "First", "Last" }, cells.Select(cell => (string?)cell.Attribute("data-cfx-column-category-group-0")));
        Assert.Equal(new[] { 90d, 10, 70 }, chart.Series[0].Points.Select(point => point.X));
    }

    [Fact]
    public void HiddenAxesKeepSourcePathsWithoutParentInkOrFalseOverflowDiagnostics() {
        var chart = HeatmapGroupedAxisTests.Model(false, false, 390); chart.Options.ShowAxes = false;
        var prepared = HeatmapGroupedAxisTests.Prepare(chart); var xml = XDocument.Parse(prepared.ToSvg());
        Assert.Empty(HeatmapGroupedAxisTests.ByRole(xml, "axis-x-category-group-label")); Assert.Empty(HeatmapGroupedAxisTests.ByRole(xml, "axis-y-category-group-label"));
        Assert.All(HeatmapGroupedAxisTests.Cells(xml), cell => Assert.NotNull(cell.Attribute("data-cfx-row-category-group-count")));
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code == "matrix.category-group-overflow");
    }
}
