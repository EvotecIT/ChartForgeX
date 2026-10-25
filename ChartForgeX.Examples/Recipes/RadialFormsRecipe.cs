using System;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

/// <summary>Complete native radial forms: two disconnected smooth Polar areas and a rotated Radar with axis-relative captions.</summary>
public static class RadialFormsRecipe {
    /// <summary>Creates source-ordered Polar runs; a break starts at the fourth observation without inventing a missing value.</summary>
    public static Chart CreatePolar(int width = 800, VisualThemeMode mode = VisualThemeMode.Light) {
        var points = new[] { new ChartPoint(0, 30), new ChartPoint(.8, 80), new ChartPoint(1.7, 55),
            new ChartPoint(3.1, 60, true), new ChartPoint(3.9, 40), new ChartPoint(4.8, 90) };
        var series = new ChartSeries(new ChartSeriesId("signal"), "Observed signal", ChartSeriesKind.Polar, points)
            .ConfigurePolar(options => { options.Form = ChartLineAreaForm.Area; options.FillOpacity = .22; options.CurveTension = .15;
                options.OutlineStroke = new ChartStrokeStyle(width: 2, dash: new[] { 5d, 3d }); });
        for (var index = 0; index < points.Length; index++) series.WithObservationId(index, new ChartObservationId("reading-" + index));
        var chart = Frame(width, mode).WithTitle("Directional signal").WithSubtitle("Two authored runs · independent closed areas")
            .WithYAxisBounds(0, 100).ConfigureXAxis(axis => axis.LabelFormatter = Degrees).AddSeries(series);
        chart.Options.PolarRotationDegrees = 25; chart.Options.XAxis.LabelOrientation = ChartAxisLabelOrientation.Parallel;
        return chart;
    }

    /// <summary>Creates categorical Radar observations with a clockwise rotation and readable tangent/spoke caption policies.</summary>
    public static Chart CreateRadar(int width = 800, VisualThemeMode mode = VisualThemeMode.Light) {
        var chart = Frame(width, mode).WithTitle("Regional readiness").WithSubtitle("Rotated coordinates · axis-relative captions")
            .WithXLabels("North", "East", "South", "West").WithYAxisBounds(0, 100)
            .AddRadarLine("Readiness", new[] { new ChartPoint(1, 75), new ChartPoint(2, 60), new ChartPoint(3, 90), new ChartPoint(4, 65) });
        chart.Series[0].ConfigureRadar(options => options.CurveTension = .1);
        chart.Options.PolarRotationDegrees = 25;
        chart.Options.XAxis.LabelOrientation = ChartAxisLabelOrientation.Parallel;
        chart.Options.YAxis.LabelOrientation = ChartAxisLabelOrientation.Perpendicular;
        return chart;
    }

    private static Chart Frame(int width, VisualThemeMode mode) => Chart.Create().WithSize(width, 420).WithTheme(VisualTheme.Graphite(), mode)
        .WithLegend().WithDataLabels(false).WithValueFormat(ChartValueFormat.Number("0.##", CultureInfo.InvariantCulture));
    private static string Degrees(double radians) => (radians * 180 / Math.PI).ToString("0", CultureInfo.InvariantCulture) + "°";
}
