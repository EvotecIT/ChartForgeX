/// <summary>Raw authored Pie/Donut rows retain native zero/Other membership and actual source layout.</summary>
internal static class NativeRawValueRecipe {
    internal static Chart Create(bool donut,string kind,double reading,int width,ThemeMode mode) {
        var values=kind is "other" or "source-reflow"?new[]{reading,25d,10d,4d,1d}:kind=="empty"?new[]{reading,0d,0d}:new[]{reading,25d,10d};
        var series=new ChartSeries(new ChartSeriesId("parts"),"Parts",donut?ChartSeriesKind.Donut:ChartSeriesKind.Pie,values.Select((value,index)=>new ChartPoint(index+1,value)))
            .ObservationIds(values.Select((_,index)=>new ChartObservationId(index==0?"primary":"part-"+index)));
        var chart=Chart.Create().Title("Captured native source values").Size(width,420).Theme(VisualTheme.Graphite()).ThemeMode(mode).Legend(true).DataLabels(true)
            .Pie(p => p.SliceLabelContent(ChartPieSliceLabelContent.LabelAndPercent)).XLabels("Subscriptions","Consulting","Support","Training","Services").AddSeries(series);
        chart.Options.Pie.MaxSlices=kind is "other" or "source-reflow"?3:8;return chart;
    }
    internal static ChartVisibilityView View(bool hidden)=>new(hidden?new[]{ChartVisibilityTarget.RadialOther(new ChartSeriesId("parts"))}:Array.Empty<ChartVisibilityTarget>(),ChartVisibilityLayout.Reflow);
    internal static (double First,double Last) Readings(string kind)=>kind is "zero" or "empty" or "initial"?(0,80):(60,5);
}
