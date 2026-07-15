using System;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Data;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void TypedDatasetsTransformWithoutMutatingSource() {
        var source = ChartDataset<MetricRow>.From(new[] {
            new MetricRow("EU", 3, 30),
            new MetricRow("US", 1, 10),
            new MetricRow("EU", 2, 20),
            new MetricRow("US", 4, 40)
        });

        var transformed = source
            .Filter(row => row.Value >= 20)
            .SortBy(row => row.Order)
            .Select(row => row.Value);

        Assert(source.Count == 4, "Dataset transforms should not mutate source rows.");
        Assert(transformed.SequenceEqual(new[] { 20d, 30d, 40d }), "Dataset filters, sorting, and projections should compose deterministically.");

        var summaries = source.Summarize(
            row => row.Region,
            (region, rows) => new MetricRow(region, rows.Count, rows.Sum(row => row.Value)));
        Assert(summaries.Count == 2 && summaries[0].Region == "EU" && summaries[0].Value == 50, "Grouped summaries should preserve first-key order and aggregate source rows.");
    }

    private static void TypedDatasetsBuildMappedSeriesAndHistograms() {
        var rows = ChartDataset<MetricRow>.From(new[] {
            new MetricRow("All", 1, 12),
            new MetricRow("All", 2, 18),
            new MetricRow("All", 3, 31),
            new MetricRow("All", 4, 42)
        });
        var chart = Chart.Create()
            .AddLine("Latency", rows, row => row.Order, row => row.Value)
            .AddHistogram("Distribution", rows.Bin(row => row.Value, 3));

        Assert(chart.Series.Count == 2, "Typed source rows should map directly into native chart series.");
        Assert(chart.Series[0].Points[2].Y == 31, "Mapped series should preserve selected values.");
        Assert(chart.Series[1].Points.Sum(point => point.Y) == rows.Count, "Histogram bins should account for every source row exactly once.");
        Assert(chart.ToPng().Length > 200, "Typed data pipelines should render through the native PNG path.");
        AssertThrows<ArgumentException>(
            () => Chart.Create().AddSeries("Invalid", ChartSeriesKind.Bubble, rows, row => row.Order, row => row.Value),
            "Typed point mapping should reject tuple-encoded specialized series that need richer contracts.");
        AssertThrows<ArgumentException>(
            () => Chart.Create().AddSeries("Invalid slope", ChartSeriesKind.Slope, rows, row => row.Order, row => row.Value),
            "Typed point mapping should reject derived slope series that require the dedicated two-value contract.");
        AssertThrows<ArgumentException>(
            () => Chart.Create().AddSeries("Invalid trend", ChartSeriesKind.TrendLine, rows, row => row.Order, row => row.Value),
            "Typed point mapping should reject derived trend lines that require least-squares calculation.");
    }

    private static void FacetsBuildDeterministicSmallMultiples() {
        var rows = ChartDataset<MetricRow>.From(new[] {
            new MetricRow("EU", 1, 20),
            new MetricRow("US", 1, 25),
            new MetricRow("EU", 2, 28),
            new MetricRow("US", 2, 32)
        });
        var grid = ChartGrid.FromFacets(
            rows,
            row => row.Region,
            (region, facet) => Chart.Create().WithTitle(region).AddLine("Value", facet, row => row.Order, row => row.Value),
            columns: 2);

        Assert(grid.Charts.Count == 2, "Faceting should create one native chart per dataset group.");
        Assert(grid.Charts[0].Title == "EU" && grid.Charts[1].Title == "US", "Facet panels should preserve deterministic first-key order.");
        Assert(grid.ToSvg().Contains("EU", StringComparison.Ordinal) && grid.ToPng().Length > 200, "Facet grids should preserve SVG and PNG output parity.");
    }

    private sealed class MetricRow {
        public MetricRow(string region, double order, double value) {
            Region = region;
            Order = order;
            Value = value;
        }

        public string Region { get; }

        public double Order { get; }

        public double Value { get; }
    }
}
