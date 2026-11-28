using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

/// <summary>Shows category drill-down from a summary chart into embedded child charts.</summary>
public static class HtmlDrilldownExamples {
    /// <summary>Creates a saved report whose site bars open per-site domain controller charts, and one controller its daily trend.</summary>
    /// <param name="dark">Whether to use the dark Graphite palette.</param>
    /// <returns>A self-contained interactive page that drills without a host.</returns>
    public static string CreatePage(bool dark = false) {
        var mode = dark ? ThemeMode.Dark : ThemeMode.Light;
        var sites = Frame(mode, "Replication failures by site", "Select a site to see its domain controllers")
            .XLabels("London", "Warsaw", "Chicago", "Sydney")
            .AddBar("Failures", Points(18, 7, 11, 3));
        var london = Frame(mode, "London domain controllers", "Select a controller to see the last seven days")
            .XLabels("LON-DC01", "LON-DC02", "LON-DC03")
            .AddBar("Failures", Points(9, 6, 3));
        var warsaw = Frame(mode, "Warsaw domain controllers", "Failures in the last seven days")
            .XLabels("WAW-DC01", "WAW-DC02")
            .AddBar("Failures", Points(5, 2));
        var dc01 = Frame(mode, "LON-DC01 daily failures", "Last seven days")
            .XLabels("Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun")
            .AddLine("Failures", Points(0, 1, 3, 2, 2, 1, 0));
        return sites.ToInteractiveHtmlPage(options => {
            options.Interaction.Enable(ChartInteractionFeatures.Zoom);
            options.Drilldown.RootLabel = "All sites";
            options.Drilldown
                .Add("London", london, configure: child => child.Drilldown.Add("LON-DC01", dc01))
                .Add("Warsaw", warsaw);
        });
    }

    private static Chart Frame(ThemeMode mode, string title, string subtitle) => Chart.Create().Size(720, 380)
        .Theme(VisualTheme.Graphite()).ThemeMode(mode).Title(title).Subtitle(subtitle);

    private static ChartPoint[] Points(params double[] values) => values.Select((value, index) => new ChartPoint(index + 1, value)).ToArray();
}
