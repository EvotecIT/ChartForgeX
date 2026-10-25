using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

/// <summary>A complete source-owned directional Polar view with independent primary and logarithmic secondary radii.</summary>
public static partial class PolarViewportRecipe {
    /// <summary>Creates authored radians, stable identities and two independent radius axes without filtering observations.</summary>
    public static Chart Create(int width = 800, VisualThemeMode mode = VisualThemeMode.Light) {
        var primary = new ChartSeries(new ChartSeriesId("primary"), "Observed strength", ChartSeriesKind.Polar,
            new[] { new ChartPoint(-.25, 5), new ChartPoint(.1, 60), new ChartPoint(.4, 95), new ChartPoint(3, 50, true), new ChartPoint(6.1, 90) });
        primary.ConfigurePolar(options => { options.Form = ChartLineAreaForm.Area; options.FillOpacity = .2; options.CurveTension = .35; });
        for (var index = 0; index < primary.Points.Count; index++) primary.WithObservationId(index, new ChartObservationId("reading-" + index));
        var secondary = new ChartSeries(new ChartSeriesId("secondary"), "Response ratio", ChartSeriesKind.Polar,
            new[] { new ChartPoint(-.25, 1), new ChartPoint(.1, 30), new ChartPoint(.4, 300), new ChartPoint(3, 100, true), new ChartPoint(6.1, 1000) });
        secondary.UseSecondaryValueAxis();
        var chart = Chart.Create().WithSize(width, 440).WithTitle("Directional response").WithSubtitle("Sector clipping · independent radius domains")
            .WithTheme(VisualTheme.Graphite(), mode).WithLegend().WithDataLabels(false).WithYAxisBounds(0, 100).WithSecondaryYAxisBounds(1, 1000)
            .AddSeries(primary).AddSeries(secondary);
        chart.Options.SecondaryYAxis.Scale = ChartScaleKind.Logarithmic; chart.Options.ShowPlotBackground = true;
        return chart;
    }
    /// <summary>Crops a seam-crossing sector and independent annuli without changing the captured source curves.</summary>
    public static ChartPolarViewport Window() => new(angle: new ChartAxisWindow(5.5, 6.8), radius: new ChartAxisWindow(20, 80), secondaryRadius: new ChartAxisWindow(10, 100));
    /// <summary>Prepares one native decision shared by script-free SVG and native PNG.</summary>
    public static PreparedVisual Prepare(Chart chart) => chart.PreparePolar(new VisualRenderContext(
        new VisualLayoutOptions(new VisualSize(chart.Options.Size.Width, chart.Options.Size.Height), chart.Options.Padding),
        chart.Options.Theme, chart.Options.ThemeMode,
        new VisualFrame(showLegend: true, showSurface: true, showCard: true, transparentBackground: chart.Options.TransparentBackground),
        new FontSpec { Family = chart.Options.Theme.Typography.Family }), Window());

}
