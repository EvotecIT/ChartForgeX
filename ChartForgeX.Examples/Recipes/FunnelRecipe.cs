/// <summary>Builds an ordered request funnel as proportional stages or a cone.</summary>
public static class FunnelRecipe {
    /// <summary>Creates the selected funnel example, preserving zero-valued completion stages.</summary>
    public static Chart Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("wide" or "compact" or "sparse" or "options" or "compact-options" or "cone-vertical" or "stage-bars-horizontal"))
            throw new ArgumentOutOfRangeException(nameof(variant));
        var chart = Chart.Create().WithDataLabels();
        if (variant is "options" or "compact-options" or "cone-vertical" or "stage-bars-horizontal") {
            var requests = new ChartSeries(new ChartSeriesId("requests"), "Requests", ChartSeriesKind.Funnel, new[] {
                new ChartPoint(1, 100), new ChartPoint(2, 75), new ChartPoint(3, 25), new ChartPoint(4, 0)
            });
            chart.WithXLabels("Received", "Reviewed", "Qualified", "Completed").AddSeries(requests)
                .ConfigureFunnel(options => {
                    options.Form = variant == "stage-bars-horizontal" ? ChartFunnelForm.StageBars : ChartFunnelForm.Cone;
                    options.Orientation = variant == "cone-vertical" ? ChartOrientation.Vertical : ChartOrientation.Horizontal;
                });
        }
        else {
            var requests = new ChartSeries(new ChartSeriesId("requests"), "Requests", ChartSeriesKind.Funnel, new[] {
                new ChartPoint(1, 120), new ChartPoint(2, 95), new ChartPoint(3, 74), new ChartPoint(4, 41)
            });
            chart.WithXLabels("Received", "Qualified", "Reviewed", "Completed").AddSeries(requests);
        }
        return chart.WithValueFormat(ChartValueFormat.Number("0.##", CultureInfo.InvariantCulture));
    }
}
