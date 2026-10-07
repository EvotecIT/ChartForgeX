using System;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawGraphiteHeatmapScale(StringBuilder sb,Chart chart,ChartRect plot,double min,double max,double y) {
        var t=chart.Options.Theme; var ramp=t.SequentialRamp!; var x=plot.Left; var w=new SvgMarkupWriter(1024);
        w.StartElement("g").Attribute("data-cfx-role","heatmap-scale").Attribute("data-cfx-min",min).Attribute("data-cfx-max",max).EndStartElement();
        var style=chart.Options.TickLabelStyle; var size=StyleFontSize(style,12);
        double LabelWidth(string text) => EstimateSvgStyledTextWidth(chart,text,size,style)+2;
        var zero=FormatValue(chart,0); var zeroWidth=LabelWidth(zero);
        DrawSvgTextLeft(w,chart,"heatmap-scale-label",zero,x,y+9,t.MutedText,12,zeroWidth,"400");
        x+=zeroWidth+8;
        w.StartElement("rect").Attribute("data-cfx-role","heatmap-scale-zero").Attribute("x",x).Attribute("y",y).Attribute("width",18).Attribute("height",10).Attribute("rx",2).Attribute("fill",t.Neutral3.ToCss()).EndEmptyElement();
        var nonZero=chart.Series.Where(s=>s.Kind==ChartSeriesKind.Heatmap).SelectMany(s=>s.Points).Where(p=>p.Y!=0).Select(p=>p.Y).ToArray();
        if(nonZero.Length>0) {
            x+=30;
            for(var i=1;i<ramp.Length;i++)w.StartElement("rect").Attribute("data-cfx-role","heatmap-scale-step").Attribute("data-cfx-level",i).Attribute("x",x+(i-1)*20).Attribute("y",y).Attribute("width",18).Attribute("height",10).Attribute("rx",2).Attribute("fill",ramp[i].ToCss()).EndEmptyElement();
            x+=(ramp.Length-1)*20+4;
            var low=FormatValue(chart,nonZero.Min()); var high=FormatValue(chart,nonZero.Max());
            var lowWidth=LabelWidth(low); var highWidth=LabelWidth(high); var separatorWidth=LabelWidth("…");
            var available=Math.Max(24,plot.Right-x-separatorWidth-12);
            var fit=Math.Min(1,available/(lowWidth+highWidth)); lowWidth*=fit; highWidth*=fit;
            DrawSvgTextLeft(w,chart,"heatmap-scale-label",low,x,y+9,t.MutedText,12,lowWidth,"400");
            x+=lowWidth+6;
            DrawSvgTextLeft(w,chart,"heatmap-scale-separator","…",x,y+9,t.MutedText,12,separatorWidth,"400");
            x+=separatorWidth+6;
            DrawSvgTextLeft(w,chart,"heatmap-scale-label",high,x,y+9,t.MutedText,12,highWidth,"400");
        }
        w.EndElement(); sb.Append(w.Build());
    }
}
