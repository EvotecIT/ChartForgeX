using System;
using System.Linq;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void ThemeSeriesDefaultsResolveBelowExplicitSeriesSettings() {
        var theme = VisualTheme.Graphite().Configure(builder => builder.ConfigureSeries(ChartSeriesKind.Line, line => {
            line.StrokeWidth = 3.5; line.Dash = new[] { 6d, 3d }; line.Interpolation = ChartInterpolation.Smooth;
            line.ConfigureMarkers(markers => { markers.Shape = ChartMarkerShape.Diamond; markers.Radius = 5; });
            line.ConfigureDataLabelStyle(style => { style.FontSize = 13; style.FontWeight = "700"; style.Color = ChartColor.FromHex("#112233"); });
        }));
        var options = new ChartOptions();
        options.DataLabelStyle.Color = ChartColor.FromHex("#445566");
        var plain = ChartSeries.CreateLine(new ChartSeriesId("plain"), "Plain", Points(1, 2, 3));
        var authored = ChartSeries.CreateLine(new ChartSeriesId("authored"), "Authored", Points(3, 2, 1)).WithStrokeWidth(1.25)
            .WithStrokeStyle(new ChartStrokeStyle(dash: Array.Empty<double>())).WithInterpolation(ChartInterpolation.Linear)
            .ConfigureMarkers(markers => markers.Radius = 2).ConfigureDataLabelStyle(style => style.FontSize = 9);

        var themed = plain.WithThemeDefaults(theme, options);
        Assert(VisualSeriesStroke.Width(themed, theme.SeriesStrokeWidth) == 3.5 && themed.StrokeStyle!.Dash!.SequenceEqual(new[] { 6d, 3d })
            && themed.Interpolation == ChartInterpolation.Smooth && themed.Markers.Shape == ChartMarkerShape.Diamond && themed.Markers.Radius == 5,
            "A series without its own settings should take the theme's line defaults.");
        Assert(themed.DataLabelStyle.FontSize == 13 && themed.DataLabelStyle.FontWeight == "700"
            && Equals(themed.DataLabelStyle.Color, ChartColor.FromHex("#445566")), "Chart-level label styling should sit above the kind's label defaults.");
        Assert(!plain.HasExplicitStrokeWidth && plain.StrokeStyle == null && plain.Interpolation == ChartInterpolation.Linear && !plain.Markers.IsConfigured,
            "Resolving theme defaults should leave the authored series unchanged.");

        var kept = authored.WithThemeDefaults(theme, options);
        Assert(VisualSeriesStroke.Width(kept, theme.SeriesStrokeWidth) == 1.25 && kept.StrokeStyle!.Dash!.Count == 0
            && kept.Interpolation == ChartInterpolation.Linear && kept.Markers.Radius == 2 && kept.Markers.Shape == ChartMarkerShape.Diamond
            && kept.DataLabelStyle.FontSize == 9 && kept.DataLabelStyle.FontWeight == "700",
            "Explicit series settings should win field by field while unset fields still inherit the kind defaults.");
        var bar = ChartSeries.CreateBar(new ChartSeriesId("bars"), "Bars", Points(1, 2, 3));
        Assert(ReferenceEquals(bar.WithThemeDefaults(theme, options), bar), "A kind without defaults should keep the global theme values.");

        var barTheme = theme.Configure(builder => builder.ConfigureSeries(ChartSeriesKind.Bar, defaults => defaults.CornerRadius = 9));
        var prepared = PreparedFamily(Chart.Create().WithTheme(barTheme).WithSize(480, 320)
            .AddSeries(ChartSeries.CreateLine(new ChartSeriesId("plain"), "Plain", Points(1, 4, 2))).AddSeries(ChartSeries.CreateBar(new ChartSeriesId("bars"), "Bars", Points(5, 6, 7))));
        Assert(prepared.Scene.Nodes.OfType<VisualScenePath>().Any(path => path.StrokeWidth == 3.5 && path.Dash != null && path.Dash.SequenceEqual(new[] { 6d, 3d })),
            "Prepared lines should draw the theme's per-kind width and dash.");
        Assert(prepared.Scene.Nodes.OfType<VisualSceneRectangle>().Where(node => node.Role == "bar").All(node => node.Radius == 9)
            && prepared.Scene.Nodes.OfType<VisualSceneRectangle>().Any(node => node.Role == "bar"), "Prepared bars should draw the theme's per-kind corner radius.");
        AssertThrows<ArgumentException>(() => VisualTheme.Graphite().Configure(builder => builder.ConfigureSeries(ChartSeriesKind.Bar, defaults => defaults.ConfigureMarkers(markers => markers.Radius = 3))),
            "Themes should reject defaults the kind never draws.");
    }

    private static void ThemeJsonLoadsSeriesDefaultsPerKind() {
        var document = VisualTheme.Graphite().ToJson();
        var json = document.Substring(0, document.LastIndexOf('}')) + ",\"series\":{"
            + "\"line\":{\"strokeWidth\":4,\"dash\":[5,2],\"curve\":\"smooth\",\"marker\":{\"shape\":\"square\",\"radius\":6,\"enabled\":true},"
            + "\"dataLabel\":{\"fontSize\":12,\"fontWeight\":\"600\",\"color\":\"#334455\"}},"
            + "\"horizontalBar\":{\"cornerRadius\":7}}}";
        var theme = VisualTheme.FromJson(json);
        var line = theme.SeriesDefaults[ChartSeriesKind.Line];
        Assert(line.StrokeWidth == 4 && line.Dash!.SequenceEqual(new[] { 5d, 2d }) && line.Interpolation == ChartInterpolation.Smooth
            && line.Markers.Shape == ChartMarkerShape.Square && line.Markers.Radius == 6 && line.Markers.Enabled == true
            && line.DataLabelStyle.FontSize == 12 && line.DataLabelStyle.FontWeight == "600" && Equals(line.DataLabelStyle.Color, ChartColor.FromHex("#334455")),
            "Theme JSON should load every line default.");
        Assert(theme.SeriesDefaults[ChartSeriesKind.HorizontalBar].CornerRadius == 7 && !theme.SeriesDefaults.ContainsKey(ChartSeriesKind.Bar),
            "Theme JSON should key defaults by camel-case series kind.");

        var svg = Chart.Create().WithTheme(theme).AddLine("Values", Points(1, 3, 2)).ToSvg();
        Assert(svg.Contains("stroke-width=\"4\"", StringComparison.Ordinal) && svg.Contains("stroke-dasharray=\"5 2\"", StringComparison.Ordinal),
            "A theme file should restyle a line chart that carries no styling code.");
        var reloaded = VisualTheme.FromJson(theme.ToJson());
        Assert(reloaded.ToJson() == theme.ToJson() && reloaded.SeriesDefaults.Keys.OrderBy(kind => kind).SequenceEqual(new[] { ChartSeriesKind.Line, ChartSeriesKind.HorizontalBar }),
            "Exported themes should round-trip their series defaults.");
        Assert(!VisualTheme.Graphite().ToJson().Contains("\"series\":{", StringComparison.Ordinal), "Themes without series defaults should export the existing document.");

        AssertThrows<ArgumentException>(() => VisualTheme.FromJson(document.Substring(0, document.LastIndexOf('}')) + ",\"series\":{\"lines\":{\"strokeWidth\":2}}}"),
            "Theme JSON should reject an unknown series kind.");
        AssertThrows<ArgumentException>(() => VisualTheme.FromJson(document.Substring(0, document.LastIndexOf('}')) + ",\"series\":{\"bar\":{\"curve\":\"smooth\"}}}"),
            "Theme JSON should reject a setting the kind never draws.");
    }
}
