using System;
using System.Linq;
using System.Xml.Linq;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

internal static partial class Program {
    private static void SankeyPaintContracts() {
        var chart = Chart.Create().WithSize(390, 460).WithHeader(false).WithLegend(false).AddSankey("Packed flows", new[] {
            new ChartNode("a", "Received"), new ChartNode("b", "Reviewed")
        }, new[] { new ChartFlowLink("flow", "a", "b", 7) }).ConfigureSankey(options => {
            options.FlowColorMode = ChartSankeyFlowColorMode.Gradient; options.LabelStyle = ChartSankeyLabelStyle.Outlined;
        });
        chart.Series[0].WithPointColor(0, ChartColor.FromHex("#2864B4")).WithPointColor(1, ChartColor.FromHex("#BB6428"));
        var xml = XDocument.Parse(chart.ToSvg());
        var labels = xml.Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "sankey-node-label").ToArray();
        PackageAssertions.Require(xml.Descendants().Any(node => node.Name.LocalName == "linearGradient") && labels.Length == 2
            && labels.All(node => {
                var text = node.Descendants().Where(item => item.Name.LocalName == "text").ToArray();
                return text.Length > 0 && text.All(item => (string?)item.Attribute("stroke-width") == "4");
            }),
            "Packed native Sankey endpoint gradient or outlined text is absent.");
        PackageAssertions.Require(chart.Series[0].FlowLinks.Single().Value == 7, "Packed paint changed the authored flow weight.");
        PackageAssertions.Png(chart.ToPng()); Console.WriteLine("Packed native Sankey endpoint paint and outlined labels passed.");
    }
}
