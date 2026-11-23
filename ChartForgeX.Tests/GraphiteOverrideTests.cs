using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class GraphiteOverrideTests {
    private static XElement[] Roles(Chart chart,string role) => XDocument.Parse(chart.ToSvg()).Descendants().Where(e=>(string?)e.Attribute("data-cfx-role")==role).ToArray();

    [Fact]
    public void FullValuesRemainAvailableWhenLabelsAreCompact() {
        const double value=12345.678912345;
        var chart=Chart.Create().DataLabels().AddBar("Count",new[]{new ChartPoint(1,value)});
        var point=Assert.Single(Roles(chart,"point"));
        Assert.Equal(value,double.Parse((string)point.Attribute("data-cfx-y")!,System.Globalization.CultureInfo.InvariantCulture));
        Assert.Contains(value.ToString("R",System.Globalization.CultureInfo.InvariantCulture),(string)point.Attribute("data-cfx-description")!);
        Assert.DoesNotContain(value.ToString("R",System.Globalization.CultureInfo.InvariantCulture),Assert.Single(Roles(chart,"data-label")).Value);
    }

    [Theory]
    [InlineData("pie","data-label")]
    [InlineData("gauge","gauge-label")]
    [InlineData("bullet","bullet-row-label")]
    [InlineData("funnel","funnel-label")]
    [InlineData("sankey","sankey-node-label")]
    public void SpecializedLabelsKeepExplicitTypography(string kind,string role) {
        var chart=Chart.Create().Size(900,500).XLabels("alpha","beta");
        var points=new[]{new ChartPoint(1,70),new ChartPoint(2,30)};
        switch(kind) {
            case "pie": chart.AddPie("alpha",points).DataLabels(); break;
            case "gauge": chart.AddGauge("alpha",70); break;
            case "bullet": chart.AddBullet("alpha",70,90); break;
            case "funnel": chart.AddFunnel("alpha",points).DataLabels(); break;
            default: chart.AddSankey("Flow", new[] { new ChartNode("alpha", "alpha"), new ChartNode("beta", "beta") }, new[]{new ChartFlowLink("flow-1", "alpha","beta",70)}); break;
        }
        chart.Series[0].DataLabelStyle.FontSize=16;
        chart.Series[0].DataLabelStyle.Color=ChartColor.FromHex("#7B61E8");
        chart.Series[0].DataLabelStyle.Italic=true;
        var labels=Roles(chart,role).SelectMany(element => element.DescendantsAndSelf().Where(node => node.Name.LocalName == "text")).ToArray();
        Assert.NotEmpty(labels);
        Assert.All(labels,e=> { Assert.Equal("#7B61E8",(string?)e.Attribute("fill")); Assert.Equal("italic",(string?)e.Attribute("font-style")); Assert.InRange((double)e.Attribute("font-size")!,12,16); });
        Assert.NotEmpty(chart.ToPng());
    }

    [Fact]
    public void ExplicitPiePlacementAndBudgetStillFitWithoutGradients() {
        var chart=Chart.Create().Size(700,440).XLabels("A","B","C").AddPie("Total",new[]{new ChartPoint(1,70),new ChartPoint(2,20),new ChartPoint(3,10)})
            .DataLabels().DataLabels(d => d.Placement(ChartDataLabelPlacement.Outside)).Legend(l => l.Position(ChartLegendPosition.Bottom).MaxHeightFraction(.35).MaxRows(2));
        Assert.NotEmpty(Roles(chart,"data-label-connector"));
        Assert.All(Roles(chart,"pie-slice"),e=>Assert.DoesNotContain("url(",(string)e.Attribute("fill")!));
        Assert.NotEmpty(chart.ToPng());
    }

    [Fact]
    public void HiddenBulletLabelsReclaimSpaceAndExplicitRangesRetainThresholds() {
        var chart=Chart.Create().AddBullet("Count",70,90,rangeEnds:new[]{40d,80d});
        var x=(double)Assert.Single(Roles(chart,"bullet-value")).Attribute("x")!;
        Assert.Contains(Roles(chart,"bullet-range-source"),e=>(string?)e.Attribute("data-cfx-max")=="40");
        chart.Series[0].ShowDataLabels=false;
        Assert.Empty(Roles(chart,"bullet-row-label"));
        Assert.True((double)Assert.Single(Roles(chart,"bullet-value")).Attribute("x")!<x);
    }

    [Fact]
    public void NamedEffectsAndExplicitAxisRuleRemainAvailable() {
        var chart=Chart.Create().AddBar("Count",new[]{new ChartPoint(1,5),new ChartPoint(2,8)}).Bars(b => b.Style(ChartBarStyle.Solid));
        Assert.Contains("linearGradient",chart.ToSvg());
        chart.YAxis(axis=>axis.ShowLine=true);
        var svg=XDocument.Parse(chart.ToSvg());
        var rule=Assert.Single(svg.Descendants(),e=>(string?)e.Attribute("data-cfx-role")=="axis-y");
        Assert.Equal((string?)rule.Attribute("x1"),(string?)rule.Attribute("x2"));
        Assert.Equal(ChartForgeX.Rendering.VisualExportRequest.ForChart(chart).Context.Theme.Resolve(ChartForgeX.ThemeMode.Light).Axis.ToCss(),(string?)rule.Attribute("stroke"));
        Assert.Throws<ArgumentOutOfRangeException>(()=>chart.Series[0].StateRole=(ChartSeriesState)100);
    }
}
