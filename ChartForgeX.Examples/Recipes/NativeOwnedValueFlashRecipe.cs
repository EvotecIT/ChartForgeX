/// <summary>Native value-ink flash with explicit font ownership; layout and source values stay native.</summary>
internal static class NativeOwnedValueFlashRecipe {
    internal static Chart Create(string kind,double value,int width,ThemeMode mode,string family) {
        ChartSeries series;
        if(kind is "treemap" or "sunburst") {
            var items=new[]{new ChartHierarchyItem("root","Portfolio"),new ChartHierarchyItem("first","Delivery","root",value),new ChartHierarchyItem("second","Support","root",20)};
            series=kind=="treemap"?ChartSeries.CreateTreemap(new ChartSeriesId("work"),"Work",items):ChartSeries.CreateSunburst(new ChartSeriesId("work"),"Work",items);
        } else {
            var radial=kind.Contains("pie",StringComparison.Ordinal)||kind.Contains("donut",StringComparison.Ordinal);var raw=kind.StartsWith("raw-",StringComparison.Ordinal);
            var values=raw?new[]{value,25d,10d,4d,1d}:new[]{value,25d,10d};
            series=new ChartSeries(new ChartSeriesId("work"),"Work",radial?kind.Contains("donut",StringComparison.Ordinal)?ChartSeriesKind.Donut:ChartSeriesKind.Pie:kind=="line"?ChartSeriesKind.Line:kind=="scatter"?ChartSeriesKind.Scatter:ChartSeriesKind.Bar,values.Select((reading,index)=>new ChartPoint(index+1,reading)))
                .ObservationIds(values.Select((_,index)=>new ChartObservationId("part-"+index)));
            series.FillPattern=ChartFillPattern.DiagonalForward;
        }
        var chart=Chart.Create().Title("Captured native value ink").Size(width,420).Theme(VisualTheme.Graphite().WithFontFamily(family)).ThemeMode(mode).Legend(false).DataLabels(false).AddSeries(series);
        if(kind=="line")chart.Lines(l => l.MarkerMode(ChartLineMarkerMode.All).LuminousStyle());
        if(kind=="bar-gradient")chart.Bars(b => b.VisualStyle(ChartBarVisualStyle.Solid()));if(kind=="bar-capsule")chart.Bars(b => b.VisualStyle(ChartBarVisualStyle.DashboardCapsule()));
        if(kind is "line" or "scatter" or "bar-gradient" or "bar-capsule") {chart.XAxis(axis=>{axis.Min=.5;axis.Max=3.5;});chart.YAxis(axis=>{axis.Min=0;axis.Max=100;});}
        if(kind.StartsWith("raw-",StringComparison.Ordinal))chart.Options.Pie.MaxSlices=3;return chart;
    }
}
