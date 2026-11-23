using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Themes;
using ChartForgeX.VisualBlocks;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class GridPanelDensityTests {
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void RetinaVisualGridPanelMatchesTheChartRenderedAlone(int panelScale) {
        var chart = Chart.Create().Size(320, 220).ShowHeader(false).ShowCard(false).ShowPlotBackground(false)
            .TransparentBackground().Legend(false)
            .AddLine("Thin line", new[] { new ChartPoint(0, 1), new ChartPoint(1, 7), new ChartPoint(2, 3) });
        chart.Raster(p => p.OutputScale(2 * panelScale));
        var expected = chart.ToRgbaImage();
        chart.Raster(p => p.OutputScale(1));
        var theme = VisualTheme.Graphite(); theme = theme.Configure(builder => { builder.Light.Background = builder.Dark.Background = ChartColor.Transparent; }); theme = theme.Configure(builder => { builder.Light.ElevatedSurface = builder.Dark.ElevatedSurface = ChartColor.Transparent; });
        var grid = VisualGrid.Create().Padding(0).Gap(0).PanelSize(320 * panelScale, 220 * panelScale).RasterScale(2).Add(chart);
        grid.Theme = theme;
        var actual = new PngVisualGridRenderer().RenderImage(grid);
        Assert.Equal(expected.Width, actual.Width); Assert.Equal(expected.Height, actual.Height);
        var worst = 0d;
        for (var i = 0; i < actual.Pixels.Length; i += 4) {
            worst = Math.Max(worst, Math.Abs(actual.Pixels[i + 3] - expected.Pixels[i + 3]));
            for (var channel = 0; channel < 3; channel++) {
                var observed = actual.Pixels[i + channel] * actual.Pixels[i + 3] / 255d;
                var alone = expected.Pixels[i + channel] * expected.Pixels[i + 3] / 255d;
                worst = Math.Max(worst, Math.Abs(observed - alone));
            }
        }
        Assert.InRange(worst, 0, 1);
        Assert.Equal(1, chart.Options.Raster.OutputScale);
        Assert.True(chart.Options.TransparentBackground);
    }

}
