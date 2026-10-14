/// <summary>Observed counts compared with a radar reference area and an unfilled target line.</summary>
public static class MixedRadialRecipe {
    /// <summary>Creates shared radial categories with optional closed curves or reversed angular order.</summary>
    public static Chart Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light, double? curveTension = null) {
        if (variant is not ("wide" or "compact" or "mixed" or "compact-mixed" or "mixed-curves" or "compact-mixed-curves" or "mixed-angular-reversal" or "compact-mixed-angular-reversal"))
            throw new ArgumentOutOfRangeException(nameof(variant));
        var tension = curveTension ?? (variant is "mixed-curves" or "compact-mixed-curves" ? 1d : (double?)null);
        var reference = new ChartSeries(new ChartSeriesId("reference"), "Reference", ChartSeriesKind.Radar, Points(1000, 900, 1100, 1000))
            .ConfigureRadar(options => { options.FillOpacity = .15; options.CurveTension = tension; })
            .ConfigureMarkers(markers => markers.Enabled = false);
        var observed = new ChartSeries(new ChartSeriesId("observed"), "Observed", ChartSeriesKind.RadialColumn, Points(1200, 700, 1050, 950));
        var target = new ChartSeries(new ChartSeriesId("target"), "Target", ChartSeriesKind.Radar, Points(1400, 1200, 1300, 1250))
            .ConfigureRadar(options => { options.Form = ChartLineAreaForm.Line; options.CurveTension = tension; })
            .ConfigureMarkers(markers => { markers.Shape = ChartMarkerShape.Diamond; markers.Radius = 4; });
        var chart = Chart.Create().WithXLabels("North", "East", "South", "West").WithYAxisBounds(0, 1600).WithDataLabels(false)
            .WithRadialGeometry(new ChartRadialGeometryOptions(innerRadiusRatio: .22, cornerRadius: 4))
            .AddSeries(reference).AddSeries(observed).AddSeries(target);
        if (variant is "mixed-angular-reversal" or "compact-mixed-angular-reversal") chart.ConfigureXAxis(axis => axis.WithReversal());
        return chart;
    }

    private static ChartPoint[] Points(params double[] values) => values.Select((value, index) => new ChartPoint(index + 1, value)).ToArray();
}
