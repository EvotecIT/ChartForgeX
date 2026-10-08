using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class GraphiteFamilyTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FunnelLabelsFitTheirStagesAndRetainValuesAndRatios(bool dark) {
        var values = new[] { 1284d, 1012, 744, 521, 466 };
        var chart = Chart.Create().WithSize(556, 324).WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight())
            .WithDataLabels().WithXLabels("Detected", "Triaged", "Assigned", "Fixed", "Verified")
            .AddFunnel("Findings", values.Select((value, index) => new ChartPoint(index + 1, value)));
        var prepared = Prepare(chart);
        var stages = prepared.Regions.Where(region => region.Role == "funnel-stage").ToArray();
        var labels = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "funnel-label").ToArray();
        Assert.Equal(values.Length, stages.Length); Assert.Equal(stages.Length, labels.Length);
        for (var index = 0; index < stages.Length; index++) {
            Assert.Contains(ChartNumericFormatter.FormatValue(chart.Options, values[index]), stages[index].Label);
            Assert.InRange(labels[index].X, stages[index].Bounds.Left, stages[index].Bounds.Right);
            Assert.InRange(labels[index].Baseline, stages[index].Bounds.Top, stages[index].Bounds.Bottom);
        }
        Assert.Equal(values.Length - 1, prepared.Scene.Nodes.OfType<VisualSceneText>().Count(node => node.Role == "funnel-ratio"));
        Assert.NotEmpty(prepared.ToPng());
    }

    [Fact]
    public void LinearGaugePlacesValueAndTargetOnItsDeclaredScale() {
        var chart = Chart.Create().AddLinearGauge("Readiness", 87).WithGauge(options => options.Target = 90);
        var svg = Literal(chart);
        var track = Assert.Single(Roles(svg, "gauge-track")); var value = Assert.Single(Roles(svg, "gauge-value"));
        Assert.Equal(Number(track, "height") / 3, Number(value, "height"), 3);
        Assert.Equal(Number(track, "width") * .87, Number(value, "width"), 3);
        Assert.Single(Roles(svg, "gauge-value-marker"));
        var target = Assert.Single(Roles(svg, "gauge-target"));
        Assert.Equal(Number(track, "x") + Number(track, "width") * .9, Number(target, "x1"), 3);
        Assert.Equal("87", (string?)Assert.Single(Roles(svg, "gauge")).Attribute("data-cfx-value"));
        Assert.NotEmpty(chart.ToPng());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DonutAggregatesAnExplicitBudgetWithoutLosingSourceIdentity(bool dark) {
        var chart = Chart.Create().WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight())
            .WithXLabels("A", "B", "C", "D", "E", "F", "G", "H")
            .AddDonut("Total", Enumerable.Range(1, 8).Select(index => new ChartPoint(index, index * 100)));
        chart.Options.MaximumPieSlices = 6;
        var prepared = Prepare(chart); var svg = XDocument.Parse(prepared.ToSvg(new VisualSvgOptions()));
        var points = Roles(svg, "radial-point");
        Assert.Equal(6, points.Length);
        Assert.Equal(new[] { "H", "G", "F", "E", "D", "Other" }, points.Select(point => (string?)point.Attribute("data-cfx-label")));
        Assert.Equal("600", (string?)points[^1].Attribute("data-cfx-value"));
        Assert.Equal("0,1,2", (string?)points[^1].Attribute("data-cfx-source-points"));
        Assert.Equal("7,6,5,4,3,0,1,2", string.Join(",", points.Select(point => (string?)point.Attribute("data-cfx-source-points"))));
        Assert.Equal(8, chart.Series[0].Points.Count);
        Assert.Equal(Enumerable.Range(1, 8).Select(index => new ChartPoint(index, index * 100)), chart.Series[0].Points);
        Assert.Equal(6, prepared.Scene.Nodes.OfType<VisualSceneSlice>().Count());
        Assert.Equal(3600, points.Sum(point => Number(point, "data-cfx-value")));
        Assert.NotEmpty(prepared.ToPng());
    }

    [Theory]
    [InlineData(50, ChartSeriesState.Danger)]
    [InlineData(74, ChartSeriesState.Warning)]
    [InlineData(87, ChartSeriesState.Quiet)]
    public void GaugeUsesDeclaredBandStateAcrossItsForms(double value, ChartSeriesState state) {
        var chart = Chart.Create().AddGauge("Readiness", value).WithGauge(options => {
            options.Target = 90; options.Bands.Add(new(0, 60, ChartSeriesState.Danger));
            options.Bands.Add(new(60, 80, ChartSeriesState.Warning)); options.Bands.Add(new(80, 100, ChartSeriesState.Quiet));
        });
        var context = VisualExportRequest.ForChart(chart).Context;
        var colors = context.Theme.Resolve(context.ThemeMode);
        var expected = state is ChartSeriesState.Warning or ChartSeriesState.Danger
            ? ChartSeriesColours.State(state, colors, colors.Palette[0]) : colors.Palette[0];
        foreach (var form in new[] { ChartGaugeForm.Arc, ChartGaugeForm.Needle, ChartGaugeForm.Linear }) {
            chart.Options.Gauge.Form = form; var svg = Literal(chart);
            if (form == ChartGaugeForm.Needle)
                Assert.Equal(expected.ToCss(), (string?)Assert.Single(Roles(svg, "gauge-needle")).Attribute("stroke"));
            else Assert.Equal(expected.ToCss(), (string?)Assert.Single(Roles(svg, "gauge-value")).Attribute("fill"));
            Assert.Equal(state.ToString(), (string?)Assert.Single(Roles(svg, "gauge")).Attribute("data-cfx-status"));
            Assert.Equal(3, Roles(svg, "gauge-band-source").Length); Assert.Single(Roles(svg, "gauge-target"));
            if (form == ChartGaugeForm.Needle) Assert.Single(Roles(svg, "gauge-needle"));
            if (form == ChartGaugeForm.Linear) Assert.Equal("rect", Assert.Single(Roles(svg, "gauge-value")).Name.LocalName);
        }
        Assert.NotEmpty(chart.ToPng());
    }

    [Fact]
    public void BulletRowsShareOneScaleAndRetainTargetStates() {
        var chart = Chart.Create().AddBullet("Below", 60, 90).AddBullet("Above", 95, 80);
        var prepared = Prepare(chart); var svg = XDocument.Parse(prepared.ToSvg(new VisualSvgOptions()));
        Assert.Equal(6, Roles(svg, "bullet-range").Length); Assert.Single(Roles(svg, "bullet-axis"));
        var rows = Roles(svg, "bullet-row");
        Assert.Equal(new[] { "below-target", "above-target" }, rows.Select(row => (string?)row.Attribute("data-cfx-status")));
        Assert.All(rows, row => { Assert.Equal("0", (string?)row.Attribute("data-cfx-scale-min")); Assert.Equal("100", (string?)row.Attribute("data-cfx-scale-max")); });
        var marks = Roles(svg, "bullet-value");
        Assert.Equal(Number(marks[0], "x"), Number(marks[1], "x"));
        var geometry = prepared.Scene.Nodes.OfType<VisualSceneRectangle>().Where(mark => mark.Role == "bullet-value").ToArray();
        Assert.Equal(60d / 95, geometry[0].Bounds.Width / geometry[1].Bounds.Width, 12);
        for (var index = 0; index < geometry.Length; index++)
            Assert.InRange(Math.Abs(geometry[index].Bounds.Width - Number(marks[index], "width")), 0, .000501);
        var colors = VisualExportRequest.ForChart(chart).Context.Theme.Resolve(VisualThemeMode.Light);
        Assert.All(geometry, mark => Assert.Equal(colors.Foreground, mark.Fill));
        var labels = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(label => label.Role == "bullet-value-label").ToArray();
        Assert.Equal(colors.Status.Critical.Ink, labels[0].Color);
        Assert.Equal(colors.Foreground, labels[1].Color);
    }

    [Fact]
    public void FlatFunnelAndSankeyRetainTheirWeightedSourceData() {
        var funnel = Chart.Create().WithXLabels("Detected", "Fixed").AddFunnel("Stages", new[] { new ChartPoint(1, 1234), new ChartPoint(2, 600) });
        Assert.Equal(new[] { "1234", "600" }, Roles(Literal(funnel), "funnel-stage").Select(stage => (string?)stage.Attribute("data-cfx-value")));
        var sankey = Chart.Create().AddSankey("Flow", new[] { new ChartSankeyLink("A", "Done", 30), new("B", "Done", 20) }).WithSankeyNodeState("Done", ChartSeriesState.Neutral);
        var links = Roles(Literal(sankey), "sankey-link");
        Assert.Equal(new[] { "30", "20" }, links.Select(link => (string?)link.Attribute("data-cfx-value")));
        Assert.Equal(1.5, Number(links[0], "data-cfx-width") / Number(links[1], "data-cfx-width"), 6);
        foreach (var chart in new[] { funnel, sankey }) { Assert.DoesNotContain("linearGradient", chart.ToSvg()); Assert.NotEmpty(chart.ToPng()); }
    }

    private static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
    private static XDocument Literal(Chart chart) => XDocument.Parse(Prepare(chart).ToSvg(new VisualSvgOptions()));
    private static XElement[] Roles(XDocument svg, string role) => svg.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == role).ToArray();
    private static double Number(XElement element, string attribute) => double.Parse((string)element.Attribute(attribute)!, CultureInfo.InvariantCulture);
}
