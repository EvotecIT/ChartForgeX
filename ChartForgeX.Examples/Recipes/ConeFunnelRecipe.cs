/// <summary>A cone funnel: trial sign-ups narrowing to renewed subscriptions, with retention captions.</summary>
public static class ConeFunnelRecipe {
    public static Chart Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("wide" or "compact")) throw new ArgumentOutOfRangeException(nameof(variant));
        var stages = new (string Name, double Count)[] {
            ("Visitors", 12400), ("Sign-ups", 5270), ("Trials", 2130), ("Paid", 860), ("Renewed", 610)
        };
        var series = new ChartSeries(new ChartSeriesId("customers"), "Customers", ChartSeriesKind.Funnel,
            stages.Select((stage, index) => new ChartPoint(index + 1, stage.Count)));
        return Chart.Create().WithLegend(false).WithDataLabels().WithXLabels(stages.Select(stage => stage.Name).ToArray())
            .AddSeries(series).ConfigureFunnel(options => {
                options.Form = ChartFunnelForm.Cone;
                // Stage names and retention sit beside the cone, so narrow stages keep their full captions.
                options.LabelContent = ChartStageLabelContent.Separate;
            }).WithValueFormat(ChartValueFormat.Number("#,0", CultureInfo.InvariantCulture));
    }
}
