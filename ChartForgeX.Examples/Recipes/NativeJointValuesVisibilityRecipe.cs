/// <summary>One native source for joint values and absolute whole-series Preserve visibility.</summary>
internal static class NativeJointValuesVisibilityRecipe {
    internal static Chart Create(bool bar,double reading,int width,VisualThemeMode mode,string family) {
        ChartSeries Series(string key,string name,double[] values)=>new ChartSeries(new ChartSeriesId(key),name,bar?ChartSeriesKind.Bar:ChartSeriesKind.Line,values.Select((value,index)=>new ChartPoint(index,value)))
            .WithObservationIds(new[]{new ChartObservationId("earlier"),new ChartObservationId("current"),new ChartObservationId("later")});
        var chart=Chart.Create().WithSize(width,420).WithTheme(VisualTheme.Graphite().WithFontFamily(family),mode).WithHeader(false).WithLegend(true).WithDataLabels(false)
            .AddSeries(Series("delivery","Delivery",new[]{30d,reading,60d})).AddSeries(Series("comparison","Comparison",new[]{10d,15d,20d}));
        chart.ConfigureXAxis(axis=>{axis.Minimum=-.5;axis.Maximum=2.5;});chart.ConfigureYAxis(axis=>{axis.Minimum=0;axis.Maximum=100;});if(!bar)chart.WithLineMarkers(ChartLineMarkerMode.All).WithLuminousLineStyle();return chart;
    }
    internal static ChartVisibilityView View(bool hidden)=>new(hidden?new[]{new ChartSeriesId("delivery")}:Array.Empty<ChartSeriesId>());
}
