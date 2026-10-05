using System;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawGraphiteFunnel(StringBuilder sb,Chart chart,ChartRect plot) {
        var s=chart.Series.First(v=>v.Kind==ChartSeriesKind.Funnel); if(s.Points.Count==0)return;
        var t=chart.Options.Theme; var maximum=s.Points.Max(v=>v.Y); var count=s.Points.Count;
        var labelWidth=Math.Min(175,plot.Width*.3); var bandWidth=Math.Max(1,plot.Width-labelWidth-20);
        var rowHeight=Math.Min(40,plot.Height/count); var h=Math.Max(1,rowHeight-2); var cx=plot.Left+bandWidth/2;
        var w=new SvgMarkupWriter(2048);
        var stageOpacity=new[]{1.0,.85,.70,.56,.44};
        w.StartElement("g").Attribute("data-cfx-role","funnel-chart").EndStartElement();
        for(var i=0;i<count;i++) {
            var p=s.Points[i]; var style=DataLabelStyle(chart,s,i); var dropOff=i>0&&s.Points[i-1].Y>0?1-p.Y/s.Points[i-1].Y:0; var width=maximum<=0?0:bandWidth*p.Y/maximum; var y=plot.Top+i*rowHeight;
            var position=count==1?0:4.0*i/(count-1); var step=Math.Min(3,(int)position);
            var opacity=stageOpacity[step]+(stageOpacity[step+1]-stageOpacity[step])*(position-step); var retention=s.Points[0].Y<=0?0:p.Y/s.Points[0].Y;
            var colour=i<s.PointColors.Count&&s.PointColors[i].HasValue?s.PointColors[i]!.Value:s.Color??t.Palette[0];
            w.StartElement("rect").Attribute("data-cfx-role","funnel-segment").Attribute("data-cfx-series",0).Attribute("data-cfx-point",i).Attribute("data-cfx-label",FormatX(chart,p.X)).Attribute("data-cfx-value",p.Y).Attribute("data-cfx-retention",retention).Attribute("data-cfx-dropoff",dropOff).Attribute("role","img").Attribute("aria-label",FormatX(chart,p.X)+": "+p.Y.ToString("G17",System.Globalization.CultureInfo.InvariantCulture)).Attribute("x",cx-width/2).Attribute("y",y).Attribute("width",width).Attribute("height",h).Attribute("rx",2).Attribute("fill",colour.ToCss()).Attribute("fill-opacity",opacity).EndEmptyElement();
            if(s.ShowDataLabels!=false) {
                style=style.Clone();
                var limit=Math.Max(8,(h-6)/2/1.2);
                if(style.FontSize.HasValue) style.FontSize=Math.Min(style.FontSize.Value,limit);
                var labelSize=StyleFontSize(style,Math.Min(13,limit)); var valueSize=StyleFontSize(style,Math.Min(12,limit));
                var labelY=y+h/2-3; var valueY=labelY+(labelSize+valueSize)*.6+1;
                DrawSvgTextLeft(w,chart,"funnel-label",FormatX(chart,p.X),plot.Left+bandWidth+20,labelY,t.Text,labelSize,labelWidth,"400",style);
                DrawSvgTextLeft(w,chart,"funnel-value",FormatValue(chart,p.Y)+" · "+FormatPercent(retention),plot.Left+bandWidth+20,valueY,t.MutedText,valueSize,labelWidth,"400",style);
            }
        }
        w.EndElement(); sb.Append(w.Build());
    }
}
