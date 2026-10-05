using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawGraphiteHeatmapScale(StringBuilder sb,Chart chart,ChartRect plot,double min,double max,double y) {
        var t=chart.Options.Theme; var ramp=t.SequentialRamp!; var x=plot.Left; var w=new SvgMarkupWriter(1024);
        w.StartElement("g").Attribute("data-cfx-role","heatmap-scale").Attribute("data-cfx-min",min).Attribute("data-cfx-max",max).EndStartElement();
        DrawSvgTextLeft(w,chart,"heatmap-scale-label",FormatValue(chart,min),x,y+9,t.MutedText,12,32,"400");
        x+=36;
        for(var i=1;i<ramp.Length;i++)w.StartElement("rect").Attribute("data-cfx-role","heatmap-scale-step").Attribute("data-cfx-level",i).Attribute("x",x+(i-1)*20).Attribute("y",y).Attribute("width",18).Attribute("height",10).Attribute("rx",2).Attribute("fill",ramp[i].ToCss()).EndEmptyElement();
        x+=(ramp.Length-1)*20+4;
        DrawSvgTextLeft(w,chart,"heatmap-scale-label",FormatValue(chart,max),x,y+9,t.MutedText,12,40,"400");
        x+=50;
        w.StartElement("rect").Attribute("data-cfx-role","heatmap-scale-zero").Attribute("x",x).Attribute("y",y).Attribute("width",18).Attribute("height",10).Attribute("rx",2).Attribute("fill",t.Neutral3.ToCss()).EndEmptyElement();
        DrawSvgTextLeft(w,chart,"heatmap-scale-label","0",x+24,y+9,t.MutedText,12,25,"400");
        w.EndElement(); sb.Append(w.Build());
    }
}
