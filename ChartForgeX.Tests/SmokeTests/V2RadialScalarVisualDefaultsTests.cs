using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects deliberate scalar and part-to-whole defaults together with their authored overrides.</summary>
public sealed class V2RadialScalarVisualDefaultsTests {
    [Theory]
    [InlineData(VisualThemeMode.Light)]
    [InlineData(VisualThemeMode.Dark)]
    public void GaugeWithoutDeclaredSeverityUsesCategoricalPaintAndThemeArcAndValueTokens(VisualThemeMode mode) {
        var context = new VisualRenderContext(themeMode: mode);
        var colors = context.Theme.Resolve(mode);
        foreach (var raw in new[] { 35d, 76d, 92d }) {
            var scene = Compile(Chart.Create().AddGauge("Observed capacity", raw), context);
            var value = Assert.Single(scene.Nodes.OfType<VisualSceneSlice>(), mark => mark.Role == "gauge-value");
            Assert.Equal(colors.Palette[0], value.Fill);
            Assert.Equal(context.Theme.GaugeStrokeWidth, value.Outer - value.Inner, 8);
            Assert.Equal(colors.Neutral2, Assert.Single(scene.Nodes.OfType<VisualSceneSlice>(), mark => mark.Role == "gauge-track").Fill);
            Assert.DoesNotContain(scene.Nodes, mark => mark.Role is "gauge-value-cap" or "gauge-track-cap");
            Assert.Equal(ChartSeriesState.None.ToString(), Assert.Single(scene.Nodes.OfType<VisualSceneGroup>(), group => group.Role == "gauge").Metadata["data-cfx-status"]);
            Assert.Equal(context.Theme.Typography.ScalarValueSize, Assert.Single(scene.Nodes.OfType<VisualSceneText>(), text => text.Role == "gauge-label").Text.Size);
        }
    }

    [Theory]
    [InlineData(ChartSeriesState.Success)]
    [InlineData(ChartSeriesState.Warning)]
    [InlineData(ChartSeriesState.Danger)]
    public void GaugeBandsReserveAttentionForWarningsAndDangersWhileExplicitStateAndPaintWin(ChartSeriesState state) {
        var context = new VisualRenderContext(); var colors = context.Theme.Resolve(context.ThemeMode);
        var chart = Chart.Create().AddGauge("Declared bands", 40).ConfigureGauge(options => options.Bands.Add(new ChartGaugeBand(0, 100, state)));
        var scene = Compile(chart, context);
        var expected = state == ChartSeriesState.Success ? colors.Palette[0] : ChartSeriesColours.State(state, colors, colors.Palette[0]);
        Assert.Equal(expected, Assert.Single(scene.Nodes.OfType<VisualSceneSlice>(), mark => mark.Role == "gauge-value").Fill);
        Assert.Equal(context.Theme.GaugeBandWidth, Assert.Single(scene.Nodes.OfType<VisualSceneSlice>(), mark => mark.Role == "gauge-band").Outer
            - Assert.Single(scene.Nodes.OfType<VisualSceneSlice>(), mark => mark.Role == "gauge-band").Inner, 8);
        chart.Series[0].StateRole = ChartSeriesState.Info;
        Assert.Equal(colors.Status.Info.Fill, Assert.Single(Compile(chart, context).Nodes.OfType<VisualSceneSlice>(), mark => mark.Role == "gauge-value").Fill);
        var authored = ChartColor.FromHex("#123456"); chart.Series[0].WithPointColor(0, authored);
        Assert.Equal(authored, Assert.Single(Compile(chart, context).Nodes.OfType<VisualSceneSlice>(), mark => mark.Role == "gauge-value").Fill);
    }

    [Theory]
    [InlineData(ChartGaugeForm.Arc, VisualThemeMode.Light)]
    [InlineData(ChartGaugeForm.Arc, VisualThemeMode.Dark)]
    [InlineData(ChartGaugeForm.Needle, VisualThemeMode.Light)]
    [InlineData(ChartGaugeForm.Needle, VisualThemeMode.Dark)]
    public void ResolvedCarlitoGaugeRowsPaintTheScalarAndCaptionWithoutOverlapping(ChartGaugeForm form, VisualThemeMode mode) {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf");
        Assert.True(File.Exists(path), "The gallery's Carlito fixture must be available.");
        var chart = Chart.Create().AddGauge("Capacity", 76).ConfigureGauge(options => {
            options.Form = form; options.Target = 85; options.Caption = "Available capacity";
            options.Bands.Add(new ChartGaugeBand(0, 50, ChartSeriesState.Danger));
            options.Bands.Add(new ChartGaugeBand(50, 80, ChartSeriesState.Warning));
            options.Bands.Add(new ChartGaugeBand(80, 100, ChartSeriesState.Success));
        });
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(800, 440)), themeMode: mode,
            frame: new VisualFrame("Available capacity against target", "SVG and native PNG from one prepared scene"),
            font: new FontSpec { Family = "Carlito", FilePath = path });
        var prepared = chart.Prepare(context);
        var value = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneText>(), text => text.Role == "gauge-label");
        var caption = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneText>(), text => text.Role == "gauge-title");
        Assert.Equal("76", Assert.Single(value.Text.Lines).Text);
        Assert.Equal("Available capacity", Assert.Single(caption.Text.Lines).Text);
        Assert.Equal(context.Theme.Typography.ScalarValueSize, value.Text.Size);
        Assert.Equal(context.Theme.Resolve(mode).MutedForeground, caption.Color);
        var valueRegion = Assert.Single(prepared.Regions, region => region.Role == "gauge-label");
        var captionRegion = Assert.Single(prepared.Regions, region => region.Role == "gauge-title");
        Assert.True(value.Text.Metrics.Height <= valueRegion.Bounds.Height + .000001);
        Assert.True(caption.Text.Metrics.Height <= captionRegion.Bounds.Height + .000001);
        Assert.True(valueRegion.Bounds.Bottom <= captionRegion.Bounds.Top);
        var svg = System.Xml.Linq.XDocument.Parse(prepared.ToSvg());
        Assert.Contains(svg.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "gauge-label" && element.Value == "76");
        Assert.True(prepared.ToPng().Length > 64);
    }

    [Fact]
    public void ScalarGeometryAndTypographyAreThemeValuesWithDataLabelOverridesPreserved() {
        var graphite = VisualTheme.Graphite();
        var theme = new VisualTheme(graphite.Resolve(VisualThemeMode.Light).ToTokens(), graphite.Resolve(VisualThemeMode.Dark).ToTokens(),
            new VisualTypography(scalarValueSize: 40, centerValueSize: 24), gaugeStrokeWidth: 8, gaugeBandWidth: 2);
        var context = new VisualRenderContext(theme: theme);
        var gauge = Chart.Create().AddGauge("Capacity", 75);
        var scene = Compile(gauge, context);
        var arc = Assert.Single(scene.Nodes.OfType<VisualSceneSlice>(), mark => mark.Role == "gauge-value");
        Assert.Equal(8, arc.Outer - arc.Inner, 8);
        Assert.Equal(40, Assert.Single(scene.Nodes.OfType<VisualSceneText>(), text => text.Role == "gauge-label").Text.Size);
        gauge.Series[0].DataLabelStyle.FontSize = 18;
        Assert.Equal(18, Assert.Single(Compile(gauge, context).Nodes.OfType<VisualSceneText>(), text => text.Role == "gauge-label").Text.Size);
        var donut = Chart.Create().AddDonut("Total", new[] { new ChartPoint(1, 30), new ChartPoint(2, 70) });
        Assert.Equal(24, Assert.Single(Compile(donut, context).Nodes.OfType<VisualSceneText>(), text => text.Role == "donut-total-label").Text.Size);
    }

    [Theory]
    [InlineData(VisualThemeMode.Light)]
    [InlineData(VisualThemeMode.Dark)]
    public void BulletRowsShareNeutralBandsAndForegroundMeasuresWithAlignedValueAndTargetText(VisualThemeMode mode) {
        var context = new VisualRenderContext(themeMode: mode); var colors = context.Theme.Resolve(mode);
        var chart = Chart.Create().AddBullet("Below", 60, 80).AddBullet("Reached", 90, 80);
        var scene = Compile(chart, context);
        var ranges = scene.Nodes.OfType<VisualSceneRectangle>().Where(mark => mark.Role == "bullet-range").ToArray();
        Assert.Equal(new[] { colors.Neutral, colors.Neutral2, colors.Neutral3 }, ranges.Take(3).Select(mark => mark.Fill!.Value));
        Assert.Equal(ranges.Take(3).Select(mark => mark.Fill), ranges.Skip(3).Select(mark => mark.Fill));
        var measures = scene.Nodes.OfType<VisualSceneRectangle>().Where(mark => mark.Role == "bullet-value").ToArray();
        Assert.All(measures, mark => Assert.Equal(colors.Foreground, mark.Fill));
        Assert.Equal(ranges[0].Bounds.Height / 3, measures[0].Bounds.Height, 8);
        Assert.DoesNotContain(scene.Nodes, mark => mark.Role == "bullet-status-marker");
        var values = scene.Nodes.OfType<VisualSceneText>().Where(text => text.Role == "bullet-value-label").ToArray();
        var targets = scene.Nodes.OfType<VisualSceneText>().Where(text => text.Role == "bullet-target-label").ToArray();
        Assert.Equal(colors.Status.Critical.Ink, values[0].Color); Assert.Equal(colors.Foreground, values[1].Color);
        for (var index = 0; index < values.Length; index++) {
            Assert.Equal(TextAlignment.Left, values[index].Alignment);
            Assert.Equal(values[index].Baseline - values[index].Text.Ascent + values[index].Text.Metrics.Height / 2,
                targets[index].Baseline - targets[index].Text.Ascent + targets[index].Text.Metrics.Height / 2, 8);
            Assert.True(targets[index].X >= values[index].X + values[index].Text.Metrics.Width);
        }
        chart.Series[0].WithPointColor(0, ChartColor.FromHex("#123456"));
        Assert.Equal(ChartColor.FromHex("#123456"), Compile(chart, context).Nodes.OfType<VisualSceneRectangle>().First(mark => mark.Role == "bullet-value").Fill);
    }

    [Fact]
    public void DonutDescendingLayoutAndNumericLegendKeepAuthoredSourceIdentityAndPaint() {
        var points = new[] { new ChartPoint(1, 20), new ChartPoint(2, 200), new ChartPoint(3, 80) };
        var chart = Chart.Create().AddDonut("Findings", points).WithXLabels("Small", "Largest", "Middle");
        chart.Options.ValueFormatter = value => value.ToString("0", CultureInfo.InvariantCulture) + " units";
        var authored = ChartColor.FromHex("#123456"); chart.Series[0].WithPointColor(0, authored).WithPointSliceOffset(0, .1);
        var context = new VisualRenderContext(); var colors = context.Theme.Resolve(context.ThemeMode);
        var scene = Compile(chart, context);
        var marks = scene.Nodes.OfType<VisualSceneSlice>().Where(mark => mark.Role == "donut-slice").ToArray();
        Assert.Equal(new[] { "series-0-point-1", "series-0-point-2", "series-0-point-0" }, marks.Select(mark => mark.Id));
        Assert.Equal(-Math.PI / 2, marks[0].Start, 8);
        Assert.Equal(new[] { colors.Palette[1], colors.Palette[2], authored }, marks.Select(mark => mark.Fill!.Value));
        Assert.NotEqual(marks[0].Cx, marks[2].Cx);
        Assert.Equal(points, chart.Series[0].Points);
        var legend = VisualRadialCompiler.LegendEntries(chart, colors);
        Assert.Equal(new[] { "Largest", "Middle", "Small" }, legend.Select(entry => entry.Label));
        Assert.Equal(new[] { "200 units", "80 units", "20 units" }, legend.Select(entry => entry.Value));
        Assert.Equal(new[] { "66.7%", "26.7%", "6.7%" }, legend.Select(entry => entry.Percentage));
        Assert.Equal(marks.Select(mark => mark.Id), legend.Select(entry => entry.Id));
        Assert.Contains(scene.Nodes.OfType<VisualSceneGroup>(), group => group.Role == "radial-point" && group.Metadata["data-cfx-source-points"] == "0");
    }

    private static VisualScene Compile(Chart chart, VisualRenderContext context) {
        var builder = new VisualSceneBuilder(new VisualSize(420, 320), context.Font);
        VisualChartCompiler.Build(VisualChartCompiler.Family(chart), chart, context, builder, new ChartRect(0, 0, 420, 320));
        return builder.Build();
    }
}
