using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

internal static class CartesianCategoryExamples {
    internal static void Write(string output, ChartPngOutputScale pngOutputScale) {
        var peer = BitConverter.Int64BitsToDouble(BitConverter.DoubleToInt64Bits(1.5) + 3);
        var observations = new[] { new ChartPoint(1.5, 2), new ChartPoint(3.5, 3) };
        var mixed = Chart.Create().Title("Bars and interval observations")
            .Subtitle("Each mark family retains its category margin")
            .XAxis(x => x.Title("Category")).YAxis(y => y.Title("Value")).Theme(VisualTheme.Graphite())
            .Bars(b => b.Style(ChartBarStyle.Flat)).DataLabels(false)
            .AddBar("Observed", observations)
            .AddRangeBar("Reference range", new[] { new ChartInterval(peer, 1, 4) });
        var stacked = Chart.Create().Title("Horizontal category stacks")
            .Subtitle("Equivalent numeric categories share one slot")
            .XAxis(x => x.Title("Value")).YAxis(y => y.Title("Category")).Theme(VisualTheme.Graphite())
            .Bars(b => b.Style(ChartBarStyle.Flat)).DataLabels(false).Bars(b => b.Stacked())
            .AddHorizontalBar("First", observations)
            .AddHorizontalBar("Second", new[] { new ChartPoint(peer, 4), new ChartPoint(3.5, 5) });
        var grid = ChartGrid.Create().Title("Automatic category margins")
            .Subtitle("Complete edge bars with mixed marks and equivalent source coordinates")
            .Columns(2).Gap(20).PanelSize(600, 415)
            .Add(mixed).Add(stacked);
        ExampleArtifactWriter.SaveGrid(grid, output, "cartesian-automatic-category-margins", pngOutputScale);
        WriteGrouped(output, pngOutputScale);
    }

    /// <summary>Shows numeric category membership in two physical orientations and both paired palettes.</summary>
    internal static void WriteGrouped(string output, ChartPngOutputScale pngOutputScale) {
        foreach (var width in new[] { 390, 960 }) foreach (var mode in new[] { ThemeMode.Light, ThemeMode.Dark }) {
            var suffix = width + (mode == ThemeMode.Dark ? "-dark" : "-light");
            ExampleArtifactWriter.SaveChart(CreateGroupedMonths(width, mode), output, "cartesian-grouped-months-" + suffix, pngOutputScale);
            ExampleArtifactWriter.SaveChart(CreateGroupedSites(width, mode), output, "cartesian-grouped-sites-" + suffix, pngOutputScale);
        }
    }

    /// <summary>Keeps a missing month in the catalog without inventing an observation or joining its line gap.</summary>
    internal static Chart CreateGroupedMonths(int width, ThemeMode mode) {
        var ids = new[] { "jan", "mar", "apr", "may", "jun" }.Select(value => new ChartObservationId(value)).ToArray();
        var actual = ChartSeries.CreateBar(new ChartSeriesId("monthly-actual"), "Actual", new[] {
            new ChartPoint(1.5, 48), new ChartPoint(4, 62), new ChartPoint(5.25, 53), new ChartPoint(6.75, 70), new ChartPoint(8.5, 68)
        }).ObservationIds(ids);
        var plan = ChartSeries.CreateLine(new ChartSeriesId("monthly-plan"), "Plan", new[] {
            new ChartPoint(1.5, 52), new ChartPoint(4, 58, breakBefore: true), new ChartPoint(5.25, 60),
            new ChartPoint(6.75, 65), new ChartPoint(8.5, 72)
        }).ObservationIds(ids).Markers(markers => { markers.Enabled = true; markers.Radius = 3; });
        return Chart.Create().Size(width, 520).Theme(VisualTheme.Graphite()).ThemeMode(mode)
            .Title("Monthly orders").Subtitle("February has no observation")
            .XAxis(x => x.Title("Month")).YAxis(y => y.Title("Orders")).Bars(b => b.Style(ChartBarStyle.Flat)).DataLabels(false)
            .TickLabelStyle(style => style.FontSize = 12)
            .XLabels(new[] {
                new ChartAxisLabel(1.5, "Jan", "2026", "Q1"), new ChartAxisLabel(2.5, "Feb", "2026", "Q1"),
                new ChartAxisLabel(4, "Mar", "2026", "Q1"), new ChartAxisLabel(5.25, "Apr", "2026", "Q2"),
                new ChartAxisLabel(6.75, "May", "2026", "Q2"), new ChartAxisLabel(8.5, "Jun", "2026", "Q2")
            }).AddSeries(actual).AddSeries(plan);
    }

    /// <summary>Disambiguates repeated site names through their parent region while retaining authored numeric positions.</summary>
    internal static Chart CreateGroupedSites(int width, ThemeMode mode) {
        var series = new ChartSeries(new ChartSeriesId("site-orders"), "Orders", ChartSeriesKind.HorizontalBar, new[] {
            new ChartPoint(1.5, 48), new ChartPoint(3.25, 62), new ChartPoint(7.5, 53), new ChartPoint(11, 70)
        }).ObservationIds(new[] { "north-central", "north-harbor", "europe-central", "europe-harbor" }
            .Select(value => new ChartObservationId(value)));
        return Chart.Create().Size(width, 520).Theme(VisualTheme.Graphite()).ThemeMode(mode)
            .Title("Orders by site").Subtitle("Repeated names retain their region")
            .XAxis(x => x.Title("Orders")).YAxis(y => y.Title("Site")).Legend(false).DataLabels(false).Bars(b => b.Style(ChartBarStyle.Flat))
            .TickLabelStyle(style => style.FontSize = 12)
            .YAxis(axis => axis.Labels.AddRange(new[] {
                new ChartAxisLabel(1.5, "Central", "North America"), new ChartAxisLabel(3.25, "Harbor", "North America"),
                new ChartAxisLabel(7.5, "Central", "Western Europe"), new ChartAxisLabel(11, "Harbor", "Western Europe")
            })).AddSeries(series);
    }
}
