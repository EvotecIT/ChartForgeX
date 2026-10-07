using System;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawGraphiteSankey(StringBuilder sb,Chart chart,ChartRect plot) {
        var t=chart.Options.Theme;
        var series=chart.Series.First(v=>v.Kind==ChartSeriesKind.Sankey);
        var style=DataLabelStyle(chart,series);
        var reserve=Math.Min(110,plot.Width*.24);
        var nodePlot=new ChartRect(plot.Left+reserve,plot.Top,Math.Max(1,plot.Width-2*reserve),plot.Height);
        var model=BuildSankeyModel(chart,nodePlot); if(model.Nodes.Count==0)return;
        var w=new SvgMarkupWriter(4096);
        w.StartElement("g").Attribute("data-cfx-role","sankey-chart").EndStartElement();
        foreach(var link in model.Links) DrawSankeyLink(w,chart,model,link);
        foreach(var n in model.Nodes) {
            var colour=SankeyColour(chart,n.Index); var left=n.Layer==0; var x=left?n.X-9:n.X+10+9;
            w.StartElement("rect").Attribute("data-cfx-role","sankey-node").Attribute("data-cfx-node",n.Index).Attribute("data-cfx-label",n.Label).Attribute("data-cfx-value",n.Value).Attribute("x",n.X).Attribute("y",n.Y).Attribute("width",10).Attribute("height",n.Height).Attribute("fill",colour.ToCss()).Attribute("role","img").Attribute("aria-label",n.Label+": "+n.Value.ToString("G17",System.Globalization.CultureInfo.InvariantCulture)).EndEmptyElement();
            if(series.ShowDataLabels!=false) {
                var size=StyleFontSize(style,12);
                var label=TrimSvgLabelToWidth(chart,n.Label,size,Math.Max(12,reserve-36),style);
                w.StartElement("text").Attribute("data-cfx-role","sankey-node-label").Attribute("data-cfx-node",n.Index).Attribute("x",x).Attribute("y",n.Y+n.Height/2+4).Attribute("text-anchor",left?"end":"start").Attribute("fill",StyleColor(style,t.Text).ToCss()).Attribute("font-family",SvgFontFamilyAttributeValue(StyleFontFamily(chart,style))).Attribute("font-size",size).Attribute("font-weight",StyleWeight(style,"400"));
                WriteSvgTextStyleAttributes(w,style);
                WriteSvgStyledTextContent(w,style,label+" ");
                w.StartElement("tspan").Attribute("font-weight",700).Text(FormatValue(chart,n.Value)).EndElement().EndElement();
            }
        }
        w.EndElement(); sb.Append(w.Build());
    }

    private static ChartColor SankeyColour(Chart chart,int index) {
        if (chart.Options.SankeyNodeStates.TryGetValue(index,out var role)) return GaugeBandColour(chart,role);
        var s=chart.Series.First(v=>v.Kind==ChartSeriesKind.Sankey);
        var sources=s.Points.Where((p,i)=>i%2==0).Select(p=>(int)p.X).Distinct().ToArray();
        var ordinal=Array.IndexOf(sources,index);
        return chart.Options.Theme.Palette[(ordinal<0?index:ordinal)%chart.Options.Theme.Palette.Length];
    }

    // Alternating weighted barycentres reduce crossings while stable source indices break ties.
    private static void OrderSankeyNodes(System.Collections.Generic.List<SankeyNode> nodes,System.Collections.Generic.List<SankeyLink> links,int maxLayer) {
        foreach(var n in nodes)n.Order=n.Index;
        for(var pass=0;pass<6;pass++) {
            var forward=pass%2==0;
            for(var step=0;step<=maxLayer;step++) {
                var layer=forward?step:maxLayer-step;
                var ordered=nodes.Where(n=>n.Layer==layer).Select(n=> {
                    var edges=links.Where(l=>forward?l.Target==n.Index:l.Source==n.Index).ToArray();
                    var sum=edges.Sum(l=>l.Value);
                    var barycentre=sum>0?edges.Sum(l=>nodes[forward?l.Source:l.Target].Order*l.Value)/sum:n.Order;
                    return new{n,barycentre};
                }).OrderBy(v=>v.barycentre).ThenBy(v=>v.n.Index).ToArray();
                for(var i=0;i<ordered.Length;i++)ordered[i].n.Order=i;
            }
        }
    }
}
