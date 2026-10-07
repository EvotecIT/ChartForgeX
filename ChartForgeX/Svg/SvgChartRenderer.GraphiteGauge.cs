using System;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawGraphiteGauge(StringBuilder sb, Chart chart, ChartRect plot) {
        var s = chart.Series.First(v => v.Kind == ChartSeriesKind.Gauge);
        var t = chart.Options.Theme; var o = chart.Options.Gauge;
        var min = s.Points[0].X; var max = s.Points[1].X; var raw = s.Points[0].Y;
        var value = Clamp(raw, min, max); var ratio = (value - min) / (max - min);
        var bands = o.Bands.OrderBy(b => b.Minimum).ToArray();
        for (var i = 1; i < bands.Length; i++) if (bands[i].Minimum < bands[i-1].Maximum) throw new InvalidOperationException("Gauge bands must not overlap.");
        var active = bands.FirstOrDefault(b => value >= b.Minimum && (value < b.Maximum || value == max && value == b.Maximum));
        var c = s.Color ?? (active?.State == ChartSeriesState.Danger ? t.Negative : active?.State == ChartSeriesState.Warning ? t.Warning : t.Palette[0]);
        var w = new SvgMarkupWriter(2048);
        w.StartElement("g").Attribute("data-cfx-role", "gauge").Attribute("data-cfx-series",0).Attribute("data-cfx-status",active?.State.ToString().ToLowerInvariant()??"normal").Attribute("data-cfx-value", raw).Attribute("data-cfx-min", min).Attribute("data-cfx-max", max).Attribute("role", "img").Attribute("aria-label", s.Name + ": " + raw.ToString("G17", System.Globalization.CultureInfo.InvariantCulture)).EndStartElement();
        if (o.Form == ChartGaugeForm.Linear) DrawGraphiteLinearGauge(w, chart, plot, min, max, raw, ratio, c);
        else {
            var r = Math.Max(1, Math.Min(plot.Width / 2 - 35, plot.Height / 1.8 - 24));
            var cx = plot.Left + plot.Width / 2; var cy = plot.Top + r + 17;
            var start = Math.PI * 5 / 6; var sweep = Math.PI * 4 / 3;
            GraphiteArc(w, "gauge-track", cx, cy, r, start, start + sweep, t.Neutral2, 14);
            foreach (var band in bands) {
                var lo = Clamp((band.Minimum-min)/(max-min),0,1); var hi = Clamp((band.Maximum-min)/(max-min),0,1);
                if (hi > lo) GraphiteArc(w, "gauge-band", cx, cy, r+13, start+sweep*lo, start+sweep*hi, GaugeBandColour(chart, band.State), 4);
            }
            if (o.Form == ChartGaugeForm.Arc && ratio > 0) GraphiteArc(w, "gauge-value", cx, cy, r, start, start+sweep*ratio, c, 14);
            if (o.Form == ChartGaugeForm.Needle) {
                var a = start+sweep*ratio;
                w.StartElement("line").Attribute("data-cfx-role", "gauge-needle").Attribute("x1", cx).Attribute("y1", cy).Attribute("x2", cx+Math.Cos(a)*(r-10)).Attribute("y2", cy+Math.Sin(a)*(r-10)).Attribute("stroke", c.ToCss()).Attribute("stroke-width", 2).EndEmptyElement();
                w.StartElement("circle").Attribute("cx",cx).Attribute("cy",cy).Attribute("r",4).Attribute("fill",t.Text.ToCss()).EndEmptyElement();
            }
            if (o.Target.HasValue) {
                var a = start+sweep*Clamp((o.Target.Value-min)/(max-min),0,1);
                w.StartElement("line").Attribute("data-cfx-role", "gauge-target").Attribute("data-cfx-target", o.Target.Value).Attribute("x1",cx+Math.Cos(a)*(r-10)).Attribute("y1",cy+Math.Sin(a)*(r-10)).Attribute("x2",cx+Math.Cos(a)*(r+10)).Attribute("y2",cy+Math.Sin(a)*(r+10)).Attribute("stroke",t.Text.ToCss()).Attribute("stroke-width",2).EndEmptyElement();
                DrawSvgTextCenteredX(w,chart,"gauge-target-label",FormatValue(chart,o.Target.Value),cx+Math.Cos(a)*(r+29),cy+Math.Sin(a)*(r+29),t.MutedText,12,55,"400");
            }
            if (s.ShowDataLabels != false) {
                var style=DataLabelStyle(chart,s,0);
                DrawSvgTextCenteredX(w,chart,"gauge-label",FormatDataLabel(chart,s,0,raw),cx,cy-7,t.Text,StyleFontSize(style,34),r*1.7,"700",style:style);
                DrawSvgTextCenteredX(w,chart,"gauge-title",o.Caption??s.Name,cx,cy+23,t.MutedText,StyleFontSize(style,12),r*1.8,"400",style:style);
            }
            if (chart.Options.ShowAxes) {
                DrawSvgTextCenteredX(w,chart,"gauge-min-label",FormatValue(chart,min),cx+Math.Cos(start)*r,cy+Math.Sin(start)*r+24,t.MutedText,12,60,"400");
                DrawSvgTextCenteredX(w,chart,"gauge-max-label",FormatValue(chart,max),cx+Math.Cos(start+sweep)*r,cy+Math.Sin(start+sweep)*r+24,t.MutedText,12,60,"400");
            }
        }
        w.EndElement(); sb.Append(w.Build());
    }

    private static ChartColor GaugeBandColour(Chart chart, ChartSeriesState state) => state switch { ChartSeriesState.Danger => chart.Options.Theme.Negative, ChartSeriesState.Warning => chart.Options.Theme.Warning, ChartSeriesState.Info => chart.Options.Theme.Info, ChartSeriesState.Success => chart.Options.Theme.Positive, ChartSeriesState.Quiet => chart.Options.Theme.Quiet, _ => chart.Options.Theme.Neutral };

    private static void GraphiteArc(SvgMarkupWriter w,string role,double cx,double cy,double r,double start,double end,ChartColor color,double stroke) => w.StartElement("path").Attribute("data-cfx-role",role).Attribute("d",BuildGaugeArc(cx,cy,r,start,end)).Attribute("fill","none").Attribute("stroke",color.ToCss()).Attribute("stroke-width",stroke).Attribute("stroke-linecap","butt").EndEmptyElement();

    private static void DrawGraphiteLinearGauge(SvgMarkupWriter w,Chart chart,ChartRect plot,double min,double max,double raw,double ratio,ChartColor color) {
        var t=chart.Options.Theme; var o=chart.Options.Gauge; var y=plot.Top+plot.Height*.45; var x=plot.Left+10;
        var labels=chart.Series[0].ShowDataLabels!=false; var width=Math.Max(1,plot.Width-20-(labels?84:0)); const double height=22;
        var ends=o.Bands.Count==0?new[]{0.0,.6,.85,1.0}:new[]{0.0}.Concat(o.Bands.OrderBy(b=>b.Maximum).Select(b=>Clamp((b.Maximum-min)/(max-min),0,1))).Concat(new[]{1.0}).Distinct().OrderBy(v=>v).ToArray();
        var colours=new[]{t.Neutral2,t.Neutral3,t.CardBackground};
        for(var i=0;i<ends.Length-1;i++)w.StartElement("rect").Attribute("data-cfx-role","gauge-track").Attribute("x",x+width*ends[i]).Attribute("y",y).Attribute("width",width*(ends[i+1]-ends[i])).Attribute("height",height).Attribute("fill",colours[Math.Min(i,2)].ToCss()).Attribute("stroke",i==ends.Length-2?t.Neutral2.ToCss():null).OptionalAttribute("stroke-width",i==ends.Length-2?1:null).EndEmptyElement();
        w.StartElement("rect").Attribute("data-cfx-role","gauge-value").Attribute("x",x).Attribute("y",y+height/3).Attribute("width",width*ratio).Attribute("height",height/3).Attribute("fill",t.Text.ToCss()).EndEmptyElement();
        var vx=x+width*ratio;
        w.StartElement("path").Attribute("data-cfx-role","gauge-value-marker").Attribute("d",$"M {F(vx-4)} {F(y-9)} L {F(vx+4)} {F(y-9)} L {F(vx)} {F(y-3)} Z").Attribute("fill",t.Text.ToCss()).EndEmptyElement();
        if(o.Target.HasValue) { var tx=x+width*Clamp((o.Target.Value-min)/(max-min),0,1); w.StartElement("line").Attribute("data-cfx-role","gauge-target").Attribute("x1",tx).Attribute("x2",tx).Attribute("y1",y-2).Attribute("y2",y+height+2).Attribute("stroke",t.Text.ToCss()).Attribute("stroke-width",2).EndEmptyElement(); }
        var style=DataLabelStyle(chart,chart.Series[0],0);
        if(labels) {
            DrawSvgTextLeft(w,chart,"gauge-label",FormatDataLabel(chart,chart.Series[0],0,raw),x+width+12,y+height/2+4,o.Target.HasValue&&raw<o.Target.Value?t.Negative:t.Text,12.5,32,"700",style);
            if(o.Target.HasValue) GraphiteEndText(w,chart,"gauge-target-label",FormatValue(chart,o.Target.Value),plot.Right,y+height/2+4,t.MutedText,12,28,"400");
            DrawSvgTextLeft(w,chart,"gauge-title",o.Caption??chart.Series[0].Name,x,y-22,t.MutedText,12,width,"400",style);
        }
        if(chart.Options.ShowAxes) {
            var bottom=y+height+16;
            w.StartElement("line").Attribute("data-cfx-role","gauge-axis").Attribute("x1",x).Attribute("x2",x+width).Attribute("y1",bottom).Attribute("y2",bottom).Attribute("stroke",t.Axis.ToCss()).Attribute("stroke-width",1).EndEmptyElement();
            for(var i=0;i<=4;i++)DrawSvgTextCenteredX(w,chart,"gauge-axis-label",FormatValue(chart,min+(max-min)*i/4),x+width*i/4,bottom+18,t.MutedText,12,50,"400");
        }
    }
}
