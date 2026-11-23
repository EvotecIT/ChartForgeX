/// <summary>Two native Pie/Donut bands with scoped observations and captured Other membership.</summary>
internal static class NativeRadialBandMotionRecipe {
    // Keep the two-band captured presentation within the existing transport cap.
    internal const int PresentationFrameCount = 5;
    internal static Chart Create(string family, bool automatic, bool other, double inner, double outer, int width, ThemeMode mode) {
        var chart = Chart.Create().Title("Native radial bands").Subtitle(other ? "Each Other target keeps its original contributors" : "Each band keeps its authored series and observations")
            .Size(width,420).Theme(VisualTheme.Graphite()).ThemeMode(mode).Legend(true).DataLabels(true).DataLabels(d => d.Placement(ChartDataLabelPlacement.Outside))
            .Pie(p => p.SliceLabelContent(ChartPieSliceLabelContent.LabelAndPercent)).XLabels("Subscriptions","Consulting","Support","Training","Other services");
        for (var index = 0; index < 2; index++) {
            var first = index == 0; var value = first ? inner : outer; var values = other ? new[] { value,25d,10d,4d,1d } : new[] { value,25d,10d };
            var kind = family == "pie" || family == "mixed" && first ? ChartSeriesKind.Pie : ChartSeriesKind.Donut;
            var series = new ChartSeries(new ChartSeriesId(first ? "inner" : "outer"),first ? "Inner" : "Outer",kind,values.Select((reading,point)=>new ChartPoint(point+1,reading)))
                .ObservationIds(values.Select((_,point)=>new ChartObservationId(point==0 ? "primary" : "part-"+point)));
            if (!automatic) series.RadialBand(first ? new ChartRadialBand(.12,.49) : new ChartRadialBand(.55,1));
            chart.AddSeries(series);
        }
        if (automatic) chart.RadialBandAllocation(new ChartRadialBandAllocation(.12,.06));
        chart.Options.Pie.MaxSlices = other ? 3 : 8; return chart;
    }
    internal static ChartVisibilityView View(bool hidden) => new(hidden ? new[] { ChartVisibilityTarget.RadialOther(new ChartSeriesId("outer")) } : Array.Empty<ChartVisibilityTarget>(),ChartVisibilityLayout.Reflow);
    internal static (string Family,bool Automatic,string Intent,int Width,ThemeMode Mode)[] Cases() => new[] {
        ("pie",true,"values",390,ThemeMode.Light), ("donut",false,"values",760,ThemeMode.Dark),
        ("mixed",true,"values",760,ThemeMode.Light), ("mixed",false,"initial",390,ThemeMode.Dark),
        ("donut",true,"initial",760,ThemeMode.Light), ("pie",false,"other",390,ThemeMode.Dark),
        ("mixed",true,"other",390,ThemeMode.Light), ("donut",false,"other",760,ThemeMode.Dark)
    };
}
