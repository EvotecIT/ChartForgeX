using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class ConeBoundaryCaptionTests {
    [Theory]
    [InlineData(390, VisualThemeMode.Light)]
    [InlineData(390, VisualThemeMode.Dark)]
    [InlineData(800, VisualThemeMode.Light)]
    [InlineData(800, VisualThemeMode.Dark)]
    public void CombinedConeValuesAndRatiosStayBesideVisibleStageBoundaries(int width, VisualThemeMode mode) {
        var chart = Chart.Create().WithSize(width, 440).WithThemeMode(mode).WithLegend(false).WithDataLabels()
            .WithXLabels("Visits", "Qualified", "Reviewed", "Won", "Renewed")
            .ConfigureFunnel(options => options.Form = ChartFunnelForm.Cone)
            .AddFunnel("Stages", new[] { 1000d, 410, 162.36, 14, 2 }.Select((value, index) => new ChartPoint(index + 1, value)));
        RoundedRadialSeriesTests.UseFixtureFont(chart);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var text = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "funnel-label").ToArray();
        Assert.Equal(new[] { "Visits: 1,000", "Qualified: 410", "Reviewed: 162", "Won: 14", "Renewed: 2" },
            text.Select(node => node.Text.Lines.Single().Text));
        var lines = prepared.Scene.Nodes.OfType<VisualScenePath>().Where(node => node.Role == "funnel-stage-line").ToArray();
        Assert.Equal(5, lines.Length);
        Assert.All(lines, line => Assert.True(Math.Abs(line.Commands[1].X - line.Commands[0].X) >= 12));
        Assert.All(lines.Skip(3), line => Assert.True(line.StrokeWidth >= 4));
        var ratios = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "funnel-ratio").ToArray();
        Assert.Equal(4, ratios.Length);
        for (var index = 0; index < ratios.Length; index++) {
            var region = prepared.Regions.Single(region => region.Id == ratios[index].Id);
            Assert.InRange(Math.Abs((region.Bounds.Top + region.Bounds.Bottom) / 2 - lines[index + 1].Commands[1].Y), 0, .01);
            Assert.InRange(region.Bounds.Left - lines[index + 1].Commands[1].X, 4, 12);
        }
        var leaders = prepared.Scene.Nodes.OfType<VisualScenePath>().Where(node => node.Role == "data-label-connector").ToArray();
        Assert.True(leaders.Length >= 2);
        Assert.All(leaders, line => Assert.InRange(Math.Abs(line.Commands.Last().X - line.Commands.First().X), 0, 12));
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code == "funnel.label-hidden");
        Assert.NotEmpty(prepared.ToPng());
        chart.WithValueFormat(ChartValueFormat.Number("0.00"));
        Assert.Contains(chart.Prepare(VisualExportRequest.ForChart(chart).Context).Scene.Nodes.OfType<VisualSceneText>(),
            node => node.Role == "funnel-label" && node.Text.Lines.Single().Text == "Reviewed: 162.36");
        var large = Chart.Create().WithSize(width, 440).WithThemeMode(mode).WithLegend(false).WithDataLabels()
            .WithXLabels("Visits", "Qualified", "Reviewed", "Won", "Renewed")
            .ConfigureFunnel(options => options.Form = ChartFunnelForm.Cone)
            .AddFunnel("Stages", new[] { 12400d, 5270, 2130, 860, 610 }.Select((value, index) => new ChartPoint(index + 1, value)));
        RoundedRadialSeriesTests.UseFixtureFont(large);
        var grouped = large.Prepare(VisualExportRequest.ForChart(large).Context);
        Assert.Equal(new[] { "Visits: 12,400", "Qualified: 5,270", "Reviewed: 2,130", "Won: 860", "Renewed: 610" },
            grouped.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "funnel-label").Select(node => node.Text.Lines.Single().Text));
    }
}
