using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class V2RadialOptionTests {
    [Fact]
    public void PatternsRespectTheDonutHoleAndChangeOnlyTheFilledMark() {
        var chart = Chart.Create().AddDonut("Load", new[] { new ChartPoint(1, 100) }).WithDonutCenterLabel(false);
        chart.Series[0].ShowDataLabels = false;
        var plain = Compile(chart);
        chart.Series[0].WithFillPattern(ChartFillPattern.Crosshatch);
        var patterned = Compile(chart);
        Assert.Contains(patterned.Nodes, node => node.Role == "radial-fill-pattern");
        var svg = XDocument.Parse(VisualSceneSvgRenderer.Render(patterned));
        Assert.Contains(svg.Descendants(), element => element.Name.LocalName == "clipPath" && element.Descendants().Any(child => (string?)child.Attribute("clip-rule") == "evenodd"));
        var original = VisualSceneRasterRenderer.Render(plain); var image = VisualSceneRasterRenderer.Render(patterned);
        var slice = Assert.Single(patterned.Nodes.OfType<VisualSceneSlice>());
        var paintedChanges = 0;
        for (var y = 0; y < image.Height; y++) for (var x = 0; x < image.Width; x++) {
            var index = (y * image.Width + x) * 4;
            var distance = Math.Sqrt((x + .5 - slice.Cx) * (x + .5 - slice.Cx) + (y + .5 - slice.Cy) * (y + .5 - slice.Cy));
            if (distance < slice.Inner - 2 || distance > slice.Outer + 2) {
                for (var channel = 0; channel < 4; channel++) Assert.Equal(original.Pixels[index + channel], image.Pixels[index + channel]);
            }
            else if (original.Pixels[index] != image.Pixels[index] || original.Pixels[index + 1] != image.Pixels[index + 1] || original.Pixels[index + 2] != image.Pixels[index + 2]) paintedChanges++;
        }
        Assert.True(paintedChanges > 100);
    }

    [Fact]
    public void StateAndPointPaintResolveBeforeAggregationAndLegendLayout() {
        var chart = Chart.Create().AddPie("Load", new[] { new ChartPoint(1, 60), new ChartPoint(2, 40) });
        chart.Series[0].StateRole = ChartSeriesState.Danger;
        var explicitColor = ChartColor.FromHex("#123456"); chart.Series[0].WithPointColor(1, explicitColor);
        var scene = Compile(chart); var colors = new VisualRenderContext().Theme.Resolve(VisualThemeMode.Light);
        var slices = scene.Nodes.OfType<VisualSceneSlice>().ToArray();
        Assert.Equal(colors.Status.Critical.Fill, slices[0].Fill); Assert.Equal(explicitColor, slices[1].Fill);
        var legend = VisualRadialCompiler.LegendEntries(chart, colors);
        Assert.Equal(slices.Select(slice => slice.Fill!.Value), legend.Select(entry => entry.Color));
        Assert.All(legend, entry => Assert.Equal(ChartSeriesState.Danger, entry.StateRole));
    }

    [Fact]
    public void FormattingRunsOncePerSliceAndMixedAggregatePatternsRemainExplicit() {
        var calls = 0; var contexts = new List<ChartPieSliceLabelContext>();
        var chart = Chart.Create().AddDonut("Load", new[] { new ChartPoint(1, 70), new ChartPoint(2, 20), new ChartPoint(3, 10) })
            .WithDonutCenterLabel(false).WithDataLabels().WithPieSliceLabelFormatter(context => { contexts.Add(context); return context.FormattedValue; });
        chart.Options.MaximumPieSlices = 2;
        chart.Options.ValueFormatter = value => "Resolved " + (++calls) + ": " + value;
        chart.Series[0].WithPointFillPattern(1, ChartFillPattern.Crosshatch);
        var scene = Compile(chart);
        Assert.Equal(2, calls); Assert.Equal(2, contexts.Count);
        Assert.All(contexts, context => Assert.Contains(scene.Regions, region => region.Role == "radial-data-label" && region.Label == context.FormattedValue));
        Assert.Contains(scene.Diagnostics, diagnostic => diagnostic.Code == "radial.aggregate-patterns");
        var aggregate = Assert.Single(scene.Nodes.OfType<VisualSceneGroup>(), group => group.Role == "radial-point" && group.Metadata["data-cfx-point"] == "-1");
        Assert.Equal("1,2", aggregate.Metadata["data-cfx-source-points"]);
        Assert.Equal("Crosshatch,None", aggregate.Metadata["data-cfx-source-patterns"]);
    }

    private static VisualScene Compile(Chart chart) {
        var context = new VisualRenderContext(); var builder = new VisualSceneBuilder(new VisualSize(420, 320), context.Font);
        VisualRadialCompiler.Build(chart, context, builder, new ChartRect(0, 0, 420, 320)); return builder.Build();
    }
}
