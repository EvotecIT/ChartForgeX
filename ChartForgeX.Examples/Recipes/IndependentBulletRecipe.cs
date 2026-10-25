/// <summary>Different measurement units retain independent domains, captions and targets in horizontal or vertical bullet tracks.</summary>
public static class IndependentBulletRecipe {
    public static Chart Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("wide" or "compact" or "vertical" or "compact-vertical")) throw new ArgumentOutOfRangeException(nameof(variant));
        var compact = variant.StartsWith("compact", StringComparison.Ordinal); var vertical = variant.EndsWith("vertical", StringComparison.Ordinal);
        var load = ChartSeries.CreateBullet(new ChartSeriesId("load"), "Capacity", new ChartBulletValue(.72, .8, 0, 1, new[] { .5, .8 }))
            .WithObservationId(0, new ChartObservationId("current"))
            .ConfigureBulletAxis(axis => { axis.WithValueFormat(ChartValueFormat.Number("0%")); axis.Labels.Add(new ChartAxisLabel(0, "0%")); axis.Labels.Add(new ChartAxisLabel(.5, "50%")); axis.Labels.Add(new ChartAxisLabel(1, "100%")); });
        var queue = ChartSeries.CreateBullet(new ChartSeriesId("queue"), "Queue", new ChartBulletValue(1250, 900, 100, 1100, new[] { 500d, 900 }))
            .WithObservationId(0, new ChartObservationId("current"))
            .ConfigureBulletAxis(axis => { axis.Reversed = true; axis.TickCount = 3; axis.WithValueFormat(ChartValueFormat.Number("0' jobs'")); });
        return Chart.Create().WithSize(compact ? 390 : 760, vertical ? 480 : 340).WithTitle("Independent operating measurements")
            .WithTheme(VisualTheme.Graphite(), mode).WithLegend(false).AddSeries(load).AddSeries(queue)
            .ConfigureBullet(options => { options.IndependentAxes = true; options.Orientation = vertical ? ChartOrientation.Vertical : ChartOrientation.Horizontal; });
    }
}
