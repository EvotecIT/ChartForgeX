/// <summary>One native source for joint values and absolute whole-series Preserve visibility.</summary>
internal static class NativeJointValuesVisibilityRecipe {
    internal static Chart Create(bool bar,double reading,int width,ThemeMode mode,string family) {
        ChartSeries Series(string key,string name,double[] values)=>new ChartSeries(new ChartSeriesId(key),name,bar?ChartSeriesKind.Bar:ChartSeriesKind.Line,values.Select((value,index)=>new ChartPoint(index,value)))
            .ObservationIds(new[]{new ChartObservationId("earlier"),new ChartObservationId("current"),new ChartObservationId("later")});
        var chart=Chart.Create().Size(width,420).Theme(VisualTheme.Graphite().WithFontFamily(family)).ThemeMode(mode).ShowHeader(false).Legend(true).DataLabels(false)
            .AddSeries(Series("delivery","Delivery",new[]{30d,reading,60d})).AddSeries(Series("comparison","Comparison",new[]{10d,15d,20d}));
        chart.XAxis(axis=>{axis.Min=-.5;axis.Max=2.5;});chart.YAxis(axis=>{axis.Min=0;axis.Max=100;});if(!bar)chart.Lines(l => l.MarkerMode(ChartLineMarkerMode.All).LuminousStyle());return chart;
    }
    internal static ChartVisibilityView View(bool hidden)=>new(hidden?new[]{new ChartSeriesId("delivery")}:Array.Empty<ChartSeriesId>());
}
