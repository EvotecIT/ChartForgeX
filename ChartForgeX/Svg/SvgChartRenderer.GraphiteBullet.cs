using System;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawGraphiteBullet(StringBuilder sb,Chart chart,ChartRect plot) {
        var rows=chart.Series.Select((s,i)=>new{s,i}).Where(v=>v.s.Kind==ChartSeriesKind.Bullet).ToArray();
        if(rows.Length==0)return;
        var t=chart.Options.Theme; var measured=ChartForgeX.Rendering.ChartBulletLayout.Create(chart,plot);
        var labeled=rows.Any(v=>v.s.ShowDataLabels!=false);
        var labelWidth=labeled?Math.Min(measured.LabelReserve,plot.Width*.45):0; var rightReserve=labeled?70.0:0;
        var x=plot.Left+labelWidth; var width=Math.Max(1,plot.Width-labelWidth-rightReserve);
        var rowHeight=Math.Min(42,(plot.Height-30)/rows.Length); var h=Math.Min(22,rowHeight*.65);
        var min=rows.Min(v=>BulletMin(v.s)); var max=rows.Max(v=>BulletMax(v.s)); if(max<=min)max=min+1;
        var w=new SvgMarkupWriter(4096);
        w.StartElement("g").Attribute("data-cfx-role","bullet-chart").EndStartElement();
        foreach(var row in rows.Select((v,i)=>new{v.s,v.i,row=i})) {
            var y=plot.Top+row.row*rowHeight; var value=BulletValue(row.s); var target=BulletTarget(row.s); var below=value<target; var style=DataLabelStyle(chart,row.s,0);
            w.StartElement("g").Attribute("data-cfx-role","bullet-row").Attribute("data-cfx-series",row.i).Attribute("data-cfx-value",value).Attribute("data-cfx-target",target).Attribute("role","group").Attribute("aria-label",row.s.Name+": "+value.ToString("G17",System.Globalization.CultureInfo.InvariantCulture)+", target "+target.ToString("G17",System.Globalization.CultureInfo.InvariantCulture)).EndStartElement();
            var ends=row.s.Points.Count>2?new[]{0.0}.Concat(BulletRangeEnds(row.s,min,max).Select(v=>Clamp((v-min)/(max-min),0,1))).ToArray():new[]{0.0,.6,.85,1}; var colours=new[]{t.Neutral2,t.Neutral3,t.CardBackground};
            for(var j=0;j<ends.Length-1;j++)w.StartElement("rect").Attribute("data-cfx-role","bullet-range").Attribute("data-cfx-range-end",min+(max-min)*ends[j+1]).Attribute("x",x+width*ends[j]).Attribute("y",y).Attribute("width",width*(ends[j+1]-ends[j])).Attribute("height",h).Attribute("fill",colours[Math.Min(j,2)].ToCss()).Attribute("stroke",j==ends.Length-2?t.Neutral2.ToCss():null).OptionalAttribute("stroke-width",j==ends.Length-2?1:null).EndEmptyElement();
            w.StartElement("rect").Attribute("data-cfx-role","bullet-value").Attribute("data-cfx-series",row.i).Attribute("data-cfx-value",value).Attribute("x",x).Attribute("y",y+h/3).Attribute("width",width*Clamp((value-min)/(max-min),0,1)).Attribute("height",h/3).Attribute("fill",t.Text.ToCss()).EndEmptyElement();
            var tx=x+width*Clamp((target-min)/(max-min),0,1);
            w.StartElement("line").Attribute("data-cfx-role","bullet-target").Attribute("data-cfx-target",target).Attribute("x1",tx).Attribute("x2",tx).Attribute("y1",y-2).Attribute("y2",y+h+2).Attribute("stroke",t.Text.ToCss()).Attribute("stroke-width",2).EndEmptyElement();
            if(row.s.ShowDataLabels!=false) {
                DrawSvgTextLeft(w,chart,"bullet-row-label",row.s.Name,plot.Left,y+h/2+4,t.Text,13,labelWidth-12,"400",style);
                DrawSvgTextLeft(w,chart,"bullet-value-label",FormatValue(chart,value),x+width+12,y+h/2+4,below?t.Negative:t.Text,12.5,30,"700",style);
                GraphiteEndText(w,chart,"bullet-target-label",FormatValue(chart,target),plot.Right,y+h/2+4,t.MutedText,12,28,"400",DataLabelStyle(chart,row.s,1));
            }
            w.EndElement();
        }
        if(chart.Options.ShowAxes) {
            var bottom=plot.Top+rowHeight*rows.Length+4;
            w.StartElement("line").Attribute("data-cfx-role","bullet-axis").Attribute("x1",x).Attribute("x2",x+width).Attribute("y1",bottom).Attribute("y2",bottom).Attribute("stroke",t.Axis.ToCss()).Attribute("stroke-width",1).EndEmptyElement();
            for(var i=0;i<=4;i++)DrawSvgTextCenteredX(w,chart,"bullet-axis-label",FormatValue(chart,min+(max-min)*i/4),x+width*i/4,bottom+18,t.MutedText,12,50,"400");
        }
        w.EndElement(); sb.Append(w.Build());
    }
}
