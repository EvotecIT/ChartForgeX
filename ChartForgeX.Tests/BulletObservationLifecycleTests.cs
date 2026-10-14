using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class BulletObservationLifecycleTests {
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void EmptyPeersLeaveDenseRowsAndOriginalIdentitiesAndCanBeRestored(int emptyIndex) {
        var colors = new[] { ChartColor.FromHex("#2468AC"), ChartColor.FromHex("#B34C89"), ChartColor.FromHex("#168257") };
        var source = Enumerable.Range(0, 3).Select(index => new ChartBulletValue(20 + index * 20, 90,
            maximum: index == emptyIndex ? 100000 : 300, rangeEnds: new[] { 100d, 200d })).ToArray();
        var series = source.Select((value, index) => ChartSeries.CreateBullet(new ChartSeriesId("peer-" + index),
            "Peer " + index, value, colors[index]).ConfigureDataLabelStyle(style => style.FontSize = 14 + index)).ToArray();
        var chart = Chart.Create().WithLegend(false).WithHeader(false);
        foreach (var item in series) chart.AddSeries(item);
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(520, 260), 0),
            VisualTheme.Graphite(), frame: new VisualFrame(showLegend: false));
        var render = new VisualRenderOptions(supersampling: 1);
        var full = chart.Prepare(context);
        var fullSvg = full.ToSvg(); var fullPng = full.ToPng(render);
        var originalHeight = full.Regions.First(region => region.Role == "bullet-row").Bounds.Height;

        series[emptyIndex].ReplaceBulletValues(Array.Empty<ChartBulletValue>());
        var mixed = chart.Prepare(context);
        var groups = Rows(mixed);
        var active = Enumerable.Range(0, 3).Where(index => index != emptyIndex).ToArray();
        Assert.Equal(active.Select(index => "series-" + index), groups.Select(group => group.Id));
        Assert.Equal(active.Select(index => "peer-" + index), groups.Select(group => group.Metadata["data-cfx-series-key"]));
        for (var row = 0; row < active.Length; row++) {
            var index = active[row]; var facts = groups[row].Metadata;
            Assert.Equal(index.ToString(CultureInfo.InvariantCulture), facts["data-cfx-series"]);
            Assert.Equal("0", facts["data-cfx-source-point"]); Assert.Equal("1", facts["data-cfx-source-count"]);
            Assert.Equal("300", facts["data-cfx-scale-max"]);
            Assert.Equal(source[index].Value.ToString(CultureInfo.InvariantCulture), facts["data-cfx-value"]);
            Assert.Equal(source[index].Target.ToString(CultureInfo.InvariantCulture), facts["data-cfx-target"]);
        }
        var rows = mixed.Regions.Where(region => region.Role == "bullet-row").ToArray();
        Assert.Equal(2, rows.Length);
        Assert.All(rows, row => Assert.Equal(originalHeight * 1.5, row.Bounds.Height, 8));
        Assert.Equal(rows[0].Bounds.Bottom, rows[1].Bounds.Top, 8);
        var bars = mixed.Scene.Nodes.OfType<VisualSceneRectangle>().Where(node => node.Role == "bullet-value").ToArray();
        Assert.Equal(active.Select(index => (ChartColor?)colors[index]), bars.Select(bar => bar.Fill));
        Assert.Equal(active.Select(index => "series-" + index + "-label"), mixed.Scene.Nodes.OfType<VisualSceneText>()
            .Where(node => node.Role == "bullet-row-label").Select(node => node.Id));
        var mixedSvg = mixed.ToSvg(); var mixedPng = mixed.ToPng(render);
        Assert.Equal(fullSvg, full.ToSvg()); Assert.Equal(fullPng, full.ToPng(render));

        series[emptyIndex].ReplaceBulletValues(new[] { source[emptyIndex] });
        var restored = chart.Prepare(context);
        Assert.Equal(series.Select(item => item.Id.Value), Rows(restored).Select(group => group.Metadata["data-cfx-series-key"]));
        Assert.Equal(colors[emptyIndex], series[emptyIndex].Color);
        Assert.Equal(14 + emptyIndex, series[emptyIndex].DataLabelStyle.FontSize);
        Assert.Equal(fullSvg, restored.ToSvg()); Assert.Equal(fullPng, restored.ToPng(render));
        Assert.Equal(mixedSvg, mixed.ToSvg()); Assert.Equal(mixedPng, mixed.ToPng(render));

        foreach (var item in series) item.ReplaceBulletValues(Array.Empty<ChartBulletValue>());
        var empty = chart.Prepare(context);
        Assert.Empty(Rows(empty)); Assert.NotEmpty(empty.Diagnostics);
        Assert.NotEmpty(empty.ToSvg()); Assert.NotEmpty(empty.ToPng(render));
    }

    private static VisualSceneGroup[] Rows(PreparedVisual prepared) => prepared.Scene.Nodes.OfType<VisualSceneGroup>()
        .Where(group => group.Role == "bullet-row").ToArray();
}
