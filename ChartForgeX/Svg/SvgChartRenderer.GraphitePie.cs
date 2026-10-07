using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private sealed class GraphiteSlice {
        internal string Label = string.Empty;
        internal double Value;
        internal int Index;
        internal int[] Sources = Array.Empty<int>();
        internal ChartColor Color;
    }

    private static void DrawGraphitePie(StringBuilder sb, Chart chart, ChartRect plot) {
        var s = chart.Series[0]; var t = chart.Options.Theme;
        var slices = s.Points.Select((p,i) => new GraphiteSlice { Label=SliceLabel(chart,p,i),Value=p.Y,Index=i,Sources=new[]{i},Color=PointColor(chart,s,0,i) })
            .Where(p => p.Value > 0).OrderByDescending(p => p.Value).ThenBy(p => p.Index).ToList();
        if(slices.Count == 0) return;
        // Pie point colours use the categorical sequence, independent of sorting.
        foreach(var v in slices) v.Color = v.Index < s.PointColors.Count && s.PointColors[v.Index].HasValue ? s.PointColors[v.Index]!.Value : s.Color ?? (v.Label.Equals("Other",StringComparison.OrdinalIgnoreCase) ? t.Neutral : t.Palette[v.Index % t.Palette.Length]);
        if(slices.Count > chart.Options.MaximumPieSlices) {
            var rest=slices.Skip(chart.Options.MaximumPieSlices-1).ToArray();
            slices=slices.Take(chart.Options.MaximumPieSlices-1).ToList();
            slices.Add(new GraphiteSlice {Label="Other",Value=rest.Sum(v=>v.Value),Index=-1,Sources=rest.SelectMany(v=>v.Sources).ToArray(),Color=t.Neutral});
        }
        var total=slices.Sum(v=>v.Value); var legend=ChartLegendVisibility.ForEntries(chart,slices.Count);
        var narrow=plot.Width<450; var rowHeight=30.0;
        var legendHeight=legend&&narrow?slices.Count*24.0+12:0;
        var pieWidth=legend&&!narrow?plot.Width*.44:plot.Width;
        var pieHeight=Math.Max(10,plot.Height-legendHeight);
        var r=Math.Max(1,Math.Min(pieWidth,pieHeight)/2-8);
        var cx=plot.Left+pieWidth/2; var cy=plot.Top+pieHeight/2;
        var inner=s.Kind==ChartSeriesKind.Donut?r*chart.Options.DonutInnerRadiusRatio:0;
        var a=-Math.PI/2; var w=new SvgMarkupWriter(4096);
        foreach(var v in slices) {
            var end=a+v.Value/total*Math.PI*2;
            w.StartElement("path").Attribute("data-cfx-role",s.Kind==ChartSeriesKind.Donut?"donut-slice":"pie-slice").Attribute("data-cfx-series",0).Attribute("data-cfx-point",v.Index).Attribute("data-cfx-source-points",string.Join(",",v.Sources)).Attribute("data-cfx-label",v.Label).Attribute("data-cfx-value",v.Value).Attribute("data-cfx-percent",v.Value/total).Attribute("data-cfx-pie-center-x",cx).Attribute("data-cfx-pie-center-y",cy).Attribute("data-cfx-pie-radius",r).Attribute("data-cfx-inner-radius-ratio",chart.Options.DonutInnerRadiusRatio).Attribute("role","img").Attribute("aria-label",v.Label+": "+v.Value.ToString("G17",System.Globalization.CultureInfo.InvariantCulture)).Attribute("d",BuildSlicePath(cx,cy,r,inner,a,end)).Attribute("fill",v.Color.ToCss()).Attribute("stroke",t.CardBackground.ToCss()).Attribute("stroke-width",2).EndEmptyElement();
            if(ShouldDrawDataLabels(chart,s)) { var mid=(a+end)/2; var style=DataLabelStyle(chart,s,Math.Max(0,v.Index)); DrawSvgTextCenteredX(w,chart,"data-label",FormatPieSliceLabel(chart,s,v.Label,v.Value,v.Value/total,Math.Max(0,v.Index)),cx+Math.Cos(mid)*(r+inner)/2,cy+Math.Sin(mid)*(r+inner)/2,ChartColorMath.AccessibleTextOnBackground(v.Color),StyleFontSize(style,12),r*.8,"400",style:style); }
            a=end;
        }
        if(inner>0 && chart.Options.ShowDonutCenterLabel && s.ShowDataLabels!=false) {
            var style=DataLabelStyle(chart,s); var valueSize=StyleFontSize(style,20); var captionSize=StyleFontSize(style,12);
            var gap=(valueSize+captionSize)/2+7;
            DrawSvgTextCenteredX(w,chart,"donut-total-label",chart.Options.DonutCenterValue??FormatValue(chart,total),cx,cy-5,t.Text,valueSize,inner*1.65,"700",style:style);
            DrawSvgTextCenteredX(w,chart,"donut-title",chart.Options.DonutCenterLabel??s.Name,cx,cy-5+gap,t.MutedText,captionSize,inner*1.7,"400",style:style);
        }
        if(legend) {
            var lx=narrow?plot.Left:plot.Left+pieWidth+18; var right=plot.Right;
            var ly=narrow?plot.Top+pieHeight+20:plot.Top+Math.Max(16,(plot.Height-slices.Count*rowHeight)/2)+12;
            if(narrow) rowHeight=24;
            w.StartElement("g").Attribute("data-cfx-role","slice-legend").Attribute("data-cfx-position",narrow?"Bottom":"Right").EndStartElement();
            foreach(var v in slices) {
                w.StartElement("g").Attribute("data-cfx-role","slice-legend-item").Attribute("data-cfx-point",v.Index).EndStartElement();
                w.StartElement("rect").Attribute("data-cfx-role","slice-legend-swatch").Attribute("x",lx).Attribute("y",ly-9).Attribute("width",10).Attribute("height",10).Attribute("rx",2).Attribute("fill",v.Color.ToCss()).EndEmptyElement();
                DrawSvgTextLeft(w,chart,"slice-legend-label",v.Label,lx+16,ly,t.Text,13,Math.Max(10,right-lx-118),"400");
                GraphiteEndText(w,chart,"slice-legend-value",FormatValue(chart,v.Value),right-51,ly,t.Text,13,60,"700");
                GraphiteEndText(w,chart,"slice-legend-percent",FormatPercent(v.Value/total),right,ly,t.MutedText,12,46,"400");
                w.EndElement(); ly+=rowHeight;
            }
            w.EndElement();
        }
        sb.Append(w.Build());
    }

}
