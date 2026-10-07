using ChartForgeX;
using ChartForgeX.Core;
using System.Linq;
using System.Xml.Linq;
using ChartForgeX.Rendering;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void OverlaySvgElementsExposeDataMetadata() {
        var annotations = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .WithSize(640, 360)
            .AddLine("Values", Points(42, 84, 126))
            .AddHorizontalLine(100, "target")
            .AddVerticalBand(1.5, 2.5, "window", opacity: 0.1)
            .ToSvg();
        var overlays = XDocument.Parse(annotations).Descendants().Where(element => element.Name.LocalName == "g" && element.Attribute("data-cfx-kind") != null).ToArray();
        CartesianMetadata(overlays.Single(element => (string?)element.Attribute("data-cfx-kind") == "HorizontalLine"), ("value", "100"), ("label", "target"));
        CartesianMetadata(overlays.Single(element => (string?)element.Attribute("data-cfx-kind") == "VerticalBand"), ("value", "1.5"), ("end", "2.5"), ("label", "window"));
        Assert(overlays.All(element => element.Descendants().Any(child => (string?)child.Attribute("data-cfx-role") == "annotation-label")), "Visible annotation labels should remain associated with their source annotation group.");

        var secondary = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .WithSize(640, 360)
            .WithSecondaryYAxis("Rate", value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%")
            .WithSecondaryYAxisBounds(0, 100)
            .AddLine("Rate", Points(88, 93, 91));
        secondary.Series[0].UseSecondaryYAxis();
        var secondarySvg = secondary.ToSvg();
        var secondaryPrepared = secondary.Prepare(VisualExportRequest.ForChart(secondary).Context);
        Assert(secondaryPrepared.Regions.Any(region => region.Role == "axis-secondary-y-label" && region.Label == "100% (100)"), "Secondary axis ticks should retain displayed text and raw values.");
        Assert(secondaryPrepared.Regions.Any(region => region.Role == "axis-secondary-y-title" && region.Label == "Rate"), "Secondary axis titles should retain the full configured label.");

        var legend = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .AddBar("Logged", Points(12, 18))
            .AddLine("Trend", Points(10, 20))
            .ToSvg();
        var entries = XDocument.Parse(legend).Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "legend-entry").ToArray();
        Assert(entries.Length == 2 && entries.Any(entry => (string?)entry.Attribute("data-cfx-series-key") == "Logged") && entries.Any(entry => (string?)entry.Attribute("data-cfx-series-key") == "Trend"), "Legend entries should retain their source series identities.");
        Assert(entries.All(entry => entry.Descendants().Any(label => (string?)label.Attribute("data-cfx-role") == "legend-label")), "Each legend entry should contain its own visible label.");
    }
}
