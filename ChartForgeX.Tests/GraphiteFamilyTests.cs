using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class GraphiteFamilyTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OutsideFunnelLabelsRemainInTheirOwnStage(bool dark) {
        var chart = Chart.Create().WithSize(556, 324).WithTitle("Remediation").WithSubtitle("Share of first stage")
            .WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight())
            .WithXLabels("Detected", "Triaged", "Assigned", "Fixed", "Verified")
            .AddFunnel("Findings", new[] { 1284d, 1012, 744, 521, 466 }.Select((value, index) => new ChartPoint(index + 1, value)));
        var stages = Roles(chart, "funnel-stage");
        Assert.Equal(5, stages.Length);
        foreach (var stage in stages) {
            var mark = Assert.Single(stage.Elements(), e => (string?)e.Attribute("data-cfx-role") == "funnel-segment");
            var labels = stage.Elements().Where(e => e.Name.LocalName == "text").ToArray();
            Assert.Equal(2, labels.Length);
            Assert.All(labels, label => {
                Assert.Equal("placed", (string?)label.Attribute("data-cfx-label-status"));
                Assert.Equal((string?)mark.Attribute("data-cfx-mark-key"), (string?)label.Attribute("data-cfx-label-mark"));
                Assert.InRange((double)label.Attribute("data-cfx-label-y")!, (double)mark.Attribute("y")!,
                    (double)mark.Attribute("y")! + (double)mark.Attribute("height")! - (double)label.Attribute("data-cfx-label-height")! + .001);
            });
        }
        Assert.NotEmpty(chart.ToPng());
    }

    [Fact]
    public void LinearGaugeUsesBulletAnatomyAndValueTriangle() {
        var chart=Chart.Create().AddLinearGauge("Readiness",87).WithGauge(o=>o.Target=90);
        var bands=Roles(chart,"gauge-track");
        Assert.Equal(3,bands.Length);
        var measure=Assert.Single(Roles(chart,"gauge-value"));
        Assert.Equal((double)bands[0].Attribute("height")!/3,(double)measure.Attribute("height")!,2);
        Assert.Equal(chart.Options.Theme.Text.ToCss(),(string?)measure.Attribute("fill"));
        Assert.Single(Roles(chart,"gauge-value-marker"));
        Assert.Single(Roles(chart,"gauge-axis"));
        Assert.Empty(Roles(chart,"legend-item"));
        Assert.NotEmpty(chart.ToPng());
    }
    private static XElement[] Roles(Chart chart,string role) => XDocument.Parse(chart.ToSvg()).Descendants().Where(e=>(string?)e.Attribute("data-cfx-role")==role).ToArray();
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DonutSortsAggregatesAndRetainsFullSourceData(bool dark) {
        var chart=Chart.Create().WithTheme(dark?ChartTheme.GraphiteDark():ChartTheme.GraphiteLight()).WithXLabels("A","B","C","D","E","F","G","H").AddDonut("Total",Enumerable.Range(1,8).Select(i=>new ChartPoint(i,i*100)));
        var slices=Roles(chart,"donut-slice");
        Assert.Equal(6,slices.Length);
        Assert.Equal("800",(string?)slices[0].Attribute("data-cfx-value"));
        Assert.Equal("Other",(string?)slices[^1].Attribute("data-cfx-label"));
        Assert.Equal("600",(string?)slices[^1].Attribute("data-cfx-value"));
        Assert.Equal("0.62",(string?)slices[0].Attribute("data-cfx-inner-radius-ratio"));
        Assert.Equal(chart.Options.Theme.Neutral.ToCss(),(string?)slices[^1].Attribute("fill"));
        Assert.Equal(8,chart.Series[0].Points.Count);
        Assert.Equal(6,Roles(chart,"slice-legend-value").Length);
        Assert.Empty(Roles(chart,"data-label"));
        Assert.True(chart.ToPng().Length>1000);
    }

    [Theory]
    [InlineData(50,"#D4302F")]
    [InlineData(74,"#C78404")]
    [InlineData(87,"#2A78D6")]
    public void GaugeOnlyChangesValuePaintInsideDeclaredAlertBands(double value,string expected) {
        var chart=Chart.Create().AddGauge("Readiness",value).WithGauge(o=> { o.Target=90; o.Bands.Add(new(0,60,ChartSeriesState.Danger)); o.Bands.Add(new(60,80,ChartSeriesState.Warning)); o.Bands.Add(new(80,100,ChartSeriesState.Quiet)); });
        var arc=Assert.Single(Roles(chart,"gauge-value"));
        Assert.Equal(expected,(string?)arc.Attribute("stroke")); Assert.Equal("14",(string?)arc.Attribute("stroke-width")); Assert.Equal("butt",(string?)arc.Attribute("stroke-linecap"));
        Assert.Equal(3,Roles(chart,"gauge-band").Length); Assert.Single(Roles(chart,"gauge-target")); Assert.Empty(Roles(chart,"legend"));
        chart.Options.Gauge.Form=ChartGaugeForm.Needle; Assert.Single(Roles(chart,"gauge-needle"));
        chart.Options.Gauge.Form=ChartGaugeForm.Linear; Assert.Equal("rect",Assert.Single(Roles(chart,"gauge-value")).Name.LocalName);
        Assert.True(chart.ToPng().Length>1000);
    }

    [Fact]
    public void BulletSharesAxisAndUsesNeutralBandsAndAlertValueText() {
        var chart=Chart.Create().AddBullet("Below",60,90).AddBullet("Above",95,80);
        Assert.Equal(6,Roles(chart,"bullet-range").Length); Assert.Single(Roles(chart,"bullet-axis"));
        Assert.All(Roles(chart,"bullet-value"),e=> { Assert.Null(e.Attribute("rx")); Assert.Equal(chart.Options.Theme.Text.ToCss(),(string?)e.Attribute("fill")); });
        Assert.Equal(chart.Options.Theme.Negative.ToCss(),(string?)Roles(chart,"bullet-value-label")[0].Attribute("fill"));
        Assert.Empty(Roles(chart,"legend"));
    }

    [Fact]
    public void FlatSpecializedFamiliesRetainNumericMetadataWithoutLighting() {
        var funnel=Chart.Create().WithXLabels("Detected","Fixed").AddFunnel("Stages",new[]{new ChartPoint(1,1234),new ChartPoint(2,600)});
        Assert.All(Roles(funnel,"funnel-segment"),e=>Assert.Equal("rect",e.Name.LocalName));
        Assert.Equal("1234",(string?)Roles(funnel,"funnel-segment")[0].Attribute("data-cfx-value"));
        var heat=Chart.Create().AddHeatmapRow("Count",new[]{0d,20,100});
        Assert.Equal(2d,double.Parse((string)Roles(heat,"heatmap")[0].Attribute("data-cfx-cell-gap")!,System.Globalization.CultureInfo.InvariantCulture));
        Assert.Contains(Roles(heat,"heatmap-cell"),e=>(string?)e.Attribute("fill")==heat.Options.Theme.Neutral3.ToCss());
        Assert.Equal(5,Roles(heat,"heatmap-scale-step").Length);
        Assert.Contains(Roles(heat,"heatmap-scale-label"),e=>e.Value=="100");
        var sankey=Chart.Create().AddSankey("Flow",new[]{new ChartSankeyLink("A","Done",30),new("B","Done",20)}).WithSankeyNodeState("Done",ChartSeriesState.Neutral);
        Assert.All(Roles(sankey,"sankey-node"),e=>Assert.Equal("10",(string?)e.Attribute("width")));
        Assert.All(Roles(sankey,"sankey-link"),e=>Assert.Equal("0.35",(string?)e.Attribute("fill-opacity")));
        foreach(var chart in new[]{funnel,heat,sankey}) { Assert.DoesNotContain("linearGradient",chart.ToSvg()); Assert.True(chart.ToPng().Length>1000); }
    }
}
