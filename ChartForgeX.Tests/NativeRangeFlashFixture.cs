using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Tests;

/// <summary>Real paired runs and authored tuple IDs shared by causal, native and browser consumers.</summary>
internal static class NativeRangeFlashFixture {
    private const string BoundedFamily = "CFX Range Flash Fixture";
    private static readonly IReadOnlyDictionary<int, int> FixtureGlyphs = CreateFixtureGlyphs();
    private static readonly byte[] RegularFont = OpenTypeTestFonts.NameKeyed(extraGlyphs: FixtureGlyphs, weight: 400);
    private static readonly byte[] BoldFont = OpenTypeTestFonts.NameKeyed(extraGlyphs: FixtureGlyphs, weight: 700);

    // Five complete SVG observations have a real one-MiB transport budget. Keep this ink-ownership
    // fixture's actual font contours bounded without changing its frames, data, or marker policies.
    // Font registration is repeated because the existing detached-replay tests clear the registry.
    private static string Family {
        get {
            FontRegistry.Register(BoundedFamily, RegularFont, 400);
            FontRegistry.Register(BoundedFamily, BoldFont, 700);
            return BoundedFamily;
        }
    }

    private static IReadOnlyDictionary<int, int> CreateFixtureGlyphs() {
        var glyphs = Enumerable.Range(32, 95).ToDictionary(code => code, _ => OpenTypeTestFonts.Box);
        // Preserve the existing compact face's real glyphs; cover all incidental labels/metrics
        // and the native range-label separator/ellipsis without falling back to a host font.
        foreach (var character in "HOxeF") glyphs.Remove(character);
        glyphs[0x2013] = OpenTypeTestFonts.Box;
        glyphs[0x2026] = OpenTypeTestFonts.Box;
        return glyphs;
    }

    internal static Chart Model(bool area, bool destination, string change = "lower", string policy = "styled", int width = 760, bool dark = false) {
        var chart = Chart.Create().WithTitle("Native interval flash").WithSize(width, 420)
            .WithTheme(VisualTheme.Graphite().WithFontFamily(Family), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .WithLegend(true).WithDataLabels(true).WithLuminousLineStyle();
        foreach (var key in new[] { "delivery", "comparison" }) {
            var changed = destination && key == "delivery";
            var values = new[] {
                new ChartRangeBand(.5, key == "delivery" ? changed && change != "upper" ? 20 : 10 : 35,
                    key == "delivery" ? changed && change != "lower" ? 60 : 40 : destination && change == "series" ? 85 : 70),
                new ChartRangeBand(1.25, 25, 55),
                new ChartRangeBand(2.5, changed && change == "tuples" ? 35 : 30, changed && change == "tuples" ? 90 : 70, true),
                new ChartRangeBand(3.25, 40, 80)
            };
            // The unrelated series uses the other admitted Range kind; shared observation IDs remain series-qualified.
            var kindArea = key == "delivery" ? area : !area;
            var series = kindArea ? ChartSeries.CreateRangeArea(new ChartSeriesId(key), key, values)
                : ChartSeries.CreateRangeBand(new ChartSeriesId(key), key, values);
            series.WithObservationIds(new[] { new ChartObservationId("current"), new ChartObservationId("later"), new ChartObservationId("disconnected"), new ChartObservationId("tail") })
                .WithColor(key == "delivery" ? ChartColor.FromRgba(32, 96, 160, policy == "alpha" ? (byte)128 : (byte)255) : ChartColor.FromHex("#8546AD"))
                .WithFillPattern(ChartFillPattern.Crosshatch).WithPointFillPattern(0, ChartFillPattern.DiagonalForward).WithMarkerRadius(6);
            series.Markers.Enabled = policy != "markers-off";
            series.Markers.Shape = policy == "circle" ? ChartMarkerShape.Circle : ChartMarkerShape.Diamond;
            series.Markers.Stroke = ChartColor.FromHex("#F8FAFC"); series.Markers.StrokeWidth = 1.5;
            if (policy == "secondary" && key == "delivery") series.UseSecondaryValueAxis();
            chart.AddSeries(series);
        }
        chart.ConfigureXAxis(axis => { axis.Minimum = 0; axis.Maximum = 4; });
        chart.ConfigureYAxis(axis => { axis.Minimum = policy == "log" ? 1 : policy == "clipped" ? 20 : 0; axis.Maximum = policy == "clipped" ? 50 : 100;
            if (policy == "log") axis.Scale = ChartScaleKind.Logarithmic; });
        if (policy == "secondary") chart.ConfigureSecondaryYAxis(axis => { axis.Minimum = 0; axis.Maximum = 200; });
        return chart;
    }

    internal static HtmlChartSection Section(HtmlInteractiveChartRenderer renderer, bool area, bool destination,
        string change = "lower", string policy = "styled", int width = 760, bool dark = false) =>
        renderer.PrepareSection(Model(area, destination, change, policy, width, dark), configure: Configure);

    internal static void Configure(HtmlChartInteractionOptions options) {
        options.CaptureNativeValueSource = true; options.Interaction.ChartId = "native-range-flash";
        options.Interaction.Enable(ChartInteractionFeatures.Export);
    }

    internal static bool Changed(string? series, string? observation, string change) =>
        series == "delivery" && (observation == "current" || change == "tuples" && observation == "disconnected")
        || change == "series" && series == "comparison" && observation == "current";

    internal static IReadOnlyList<(VisualSceneNode Node, string? Series, string? Observation, bool Point)> Owned(PreparedVisual visual) {
        var result = new List<(VisualSceneNode, string?, string?, bool)>();
        var scopes = new Stack<(string? Series, string? Observation, bool Point)>();
        var current = (Series: (string?)null, Observation: (string?)null, Point: false);
        foreach (var node in visual.Scene.Nodes) {
            if (node is VisualSceneGroup group) {
                scopes.Push(current);
                if (group.Role == "series") current = (group.Metadata["data-cfx-series-key"], null, false);
                if (group.Role == "point") current = (group.Metadata["data-cfx-series-key"], group.Metadata["data-cfx-observation-id"], true);
            } else if (node is VisualSceneEndGroup) current = scopes.Pop();
            result.Add((node, current.Series, current.Observation, current.Point));
        }
        return result;
    }
}
