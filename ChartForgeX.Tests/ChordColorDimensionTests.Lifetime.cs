using ChartForgeX.Core;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class ChordColorDimensionTests {
    [Fact]
    public void PreparedColorFactsAndFormattingAreDetachedFromReplacementAndLaterOptions() {
        var chart = Model(new double?[] { -10, 0, null, 10, 30 }); var calls = new Dictionary<double, int>();
        var allowCalls = true;
        chart.Options.Chord.ColorDimension.ValueFormat = ChartValueFormat.Custom(value => {
            Assert.True(allowCalls, "Prepared exports must not rerun a mutable model formatter.");
            calls[value] = calls.TryGetValue(value, out var count) ? count + 1 : 1;
            return value.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + " ms";
        });
        var prepared = Prepare(chart, legend: true); var source = chart.Series[0];
        var svg = prepared.ToSvg(); var png = prepared.ToPng(new VisualRenderOptions(supersampling: 1));
        Assert.All(calls.Values, count => Assert.Equal(1, count));
        Assert.Equal(new[] { -10d, 0, 10, 20, 30 }, calls.Keys.OrderBy(value => value));
        var frozenCalls = calls.OrderBy(item => item.Key).ToArray();
        source.ReplaceFlowData(source.Nodes, source.FlowLinks.Select(flow => new ChartFlowLink(flow.Id, flow.SourceId, flow.TargetId, flow.Value, 99)));
        chart.ConfigureChord(options => {
            options.ColorDimension.Name = "Changed"; options.ColorScale = ChartColorScale.Sequential(High, Low).WithValueRange(0, 100);
        });
        allowCalls = false;
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
        Assert.Equal(frozenCalls, calls.OrderBy(item => item.Key));
        Assert.Equal("0", (string?)Link(prepared, "priority").Attribute("data-cfx-color-value"));
        Assert.Equal("true", (string?)Link(prepared, "return").Attribute("data-cfx-color-missing"));
        Assert.All(source.FlowLinks, flow => Assert.Equal((double?)99, flow.ColorValue));
    }

    [Fact]
    public void NestedColorConfigurationAndFlowReplacementCommitOnlyCompleteSnapshots() {
        var chart = Model(new double?[] { -10, 0, 10, 30, null }); var original = chart.Options.Chord;
        Assert.Throws<InvalidOperationException>(() => chart.ConfigureChord(options => {
            options.ColorDimension.Name = "Rejected"; options.ColorDimension.ShowLegend = false;
            throw new InvalidOperationException("Callback did not commit.");
        }));
        Assert.Same(original, chart.Options.Chord); Assert.Equal("Delay", chart.Options.Chord.ColorDimension.Name);
        ChartChordOptions? retained = null;
        chart.ConfigureChord(options => { retained = options; options.ColorDimension.Name = "Committed"; });
        retained!.ColorDimension.Name = "Retained edit";
        Assert.Equal("Committed", chart.Options.Chord.ColorDimension.Name);
        var series = chart.Series[0]; var scale = chart.Options.Chord.ColorScale;
        series.WithFlowStyle("priority", new ChartFlowStyle(fill: Low)).WithFlowState("priority", ChartSeriesState.Warning);
        var before = Prepare(chart).ToSvg(); var facts = series.FlowLinks.ToArray();
        Assert.Throws<ArgumentException>(() => series.ReplaceFlowData(series.Nodes, new[] { new ChartFlowLink("bad", "a", "unknown", 1, 99) }));
        Assert.Throws<InvalidOperationException>(() => series.ReplaceFlowData(series.Nodes, Interrupted()));
        Assert.Equal(facts, series.FlowLinks); Assert.Equal(before, Prepare(chart).ToSvg());
        Assert.Equal(Low, series.FlowStyles["priority"].Fill); Assert.Equal(ChartSeriesState.Warning, series.FlowStates["priority"]);
        var replacement = facts.Reverse().Select(flow => new ChartFlowLink(flow.Id, flow.SourceId, flow.TargetId, flow.Value, 99)).ToList();
        series.ReplaceFlowData(series.Nodes, replacement); replacement.Clear();
        Assert.Equal(facts.Reverse().Select(flow => flow.Id), series.FlowLinks.Select(flow => flow.Id));
        Assert.All(series.FlowLinks, flow => Assert.Equal((double?)99, flow.ColorValue));
        Assert.Empty(series.FlowStyles); Assert.Empty(series.FlowStates);
        Assert.Same(scale, chart.Options.Chord.ColorScale); Assert.Equal("Committed", chart.Options.Chord.ColorDimension.Name);

        static IEnumerable<ChartFlowLink> Interrupted() {
            yield return new ChartFlowLink("first", "a", "b", 1, 99);
            throw new InvalidOperationException("Source enumeration failed before replacement.");
        }
    }

    [Theory]
    [InlineData(360, false)]
    [InlineData(360, true)]
    [InlineData(800, false)]
    [InlineData(800, true)]
    public void RegisteredColorRecipeRetainsNineTransferFactsAndProducesBothNativeExports(int width, bool dark) {
        var variant = width == 360 ? "compact-color-dimension" : "color-dimension";
        var recipe = ExampleRecipes.OptionVariants.Single(item => item.Key == (ChartSeriesKind.Chord, "color-dimension")).Value;
        var chart = (Chart)recipe.Create(variant, dark ? VisualThemeMode.Dark : VisualThemeMode.Light);
        chart.WithSize(width, width == 360 ? 360 : 440).WithTitle("Transfer volume and latency")
            .WithSubtitle("Illustrative latency colors flows; transfer counts retain their weights")
            .WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light);
        var original = ChordRecipe.Create("wide").Series[0].FlowLinks;
        Assert.Equal(original.Select(flow => (flow.Id, flow.SourceId, flow.TargetId, flow.Value)),
            chart.Series[0].FlowLinks.Select(flow => (flow.Id, flow.SourceId, flow.TargetId, flow.Value)));
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        Assert.Equal(9, Links(prepared).Length); Assert.Equal(4, PreparedChordTests.Role(prepared, "chord-node").Length);
        Assert.Equal("0", (string?)Link(prepared, "support-south").Attribute("data-cfx-color-value"));
        Assert.Equal("true", (string?)Link(prepared, "south-support").Attribute("data-cfx-color-missing"));
        Assert.NotEmpty(PreparedChordTests.Role(prepared, "chord-color-scale-caption-source"));
        Assert.NotEmpty(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
        Assert.All(prepared.Regions, region => {
            Assert.True(double.IsFinite(region.Bounds.Left)); Assert.True(double.IsFinite(region.Bounds.Top));
            Assert.True(region.Bounds.Width >= 0); Assert.True(region.Bounds.Height >= 0);
        });
        ScatterBubbleColorRenderingTests.Capture(prepared, "chord-color-" + width + "-" + (dark ? "dark" : "light"));
    }
}
