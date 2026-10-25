/// <summary>Two native Pie/Donut bands with scoped observations and captured Other membership.</summary>
internal static class NativeRadialBandMotionRecipe {
    internal static Chart Create(string family, bool automatic, bool other, double inner, double outer, int width, VisualThemeMode mode) {
        var chart = Chart.Create().WithTitle("Native radial bands").WithSubtitle(other ? "Each Other target keeps its original contributors" : "Each band keeps its authored series and observations")
            .WithSize(width,420).WithTheme(VisualTheme.Graphite(),mode).WithLegend(true).WithDataLabels(true).WithDataLabelPlacement(ChartDataLabelPlacement.Outside)
            .WithPieSliceLabelContent(ChartPieSliceLabelContent.LabelAndPercent).WithXLabels("Subscriptions","Consulting","Support","Training","Other services");
        for (var index = 0; index < 2; index++) {
            var first = index == 0; var value = first ? inner : outer; var values = other ? new[] { value,25d,10d,4d,1d } : new[] { value,25d,10d };
            var kind = family == "pie" || family == "mixed" && first ? ChartSeriesKind.Pie : ChartSeriesKind.Donut;
            var series = new ChartSeries(new ChartSeriesId(first ? "inner" : "outer"),first ? "Inner" : "Outer",kind,values.Select((reading,point)=>new ChartPoint(point+1,reading)))
                .WithObservationIds(values.Select((_,point)=>new ChartObservationId(point==0 ? "primary" : "part-"+point)));
            if (!automatic) series.WithRadialBand(first ? new ChartRadialBand(.12,.49) : new ChartRadialBand(.55,1));
            chart.AddSeries(series);
        }
        if (automatic) chart.WithAutomaticRadialBands(new ChartRadialBandAllocation(.12,.06));
        chart.Options.MaximumPieSlices = other ? 3 : 8; return chart;
    }
    internal static ChartVisibilityView View(bool hidden) => new(hidden ? new[] { ChartVisibilityTarget.RadialOther(new ChartSeriesId("outer")) } : Array.Empty<ChartVisibilityTarget>(),ChartVisibilityLayout.Reflow);
    internal static (string Family,bool Automatic,string Intent,int Width,VisualThemeMode Mode)[] Cases() => new[] {
        ("pie",true,"values",390,VisualThemeMode.Light), ("donut",false,"values",760,VisualThemeMode.Dark),
        ("mixed",true,"values",760,VisualThemeMode.Light), ("mixed",false,"initial",390,VisualThemeMode.Dark),
        ("donut",true,"initial",760,VisualThemeMode.Light), ("pie",false,"other",390,VisualThemeMode.Dark),
        ("mixed",true,"other",390,VisualThemeMode.Light), ("donut",false,"other",760,VisualThemeMode.Dark)
    };
}
