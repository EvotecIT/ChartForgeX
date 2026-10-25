using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class ChordRibbonTensionTests {
    [Theory]
    [InlineData(0)]
    [InlineData(.55)]
    [InlineData(.8)]
    [InlineData(1)]
    public void TensionMovesOnlyConnectorControlsAndPreservesSlotsArcsDirectionsAndFacts(double tension) {
        var chart = ChordDepthRecipe.Create("source").ConfigureChord(options => options.RibbonTension = tension);
        var model = ChartChordLayout.Compute(chart.Series[0], chart.Options.Chord, new ChartRect(0, 0, 800, 520));
        var defaultChart = ChordDepthRecipe.Create("source");
        var baseline = ChartChordLayout.Compute(defaultChart.Series[0], defaultChart.Options.Chord, new ChartRect(0, 0, 800, 520));
        Assert.Equal(baseline.Nodes.Select(node => (node.Start, node.Sweep)), model.Nodes.Select(node => (node.Start, node.Sweep)));
        Assert.Equal(baseline.Links.Select(link => (link.SourceStart, link.TargetStart, link.Sweep)), model.Links.Select(link => (link.SourceStart, link.TargetStart, link.Sweep)));
        for (var index = 0; index < model.Links.Count; index++) {
            var flow = model.Links[index]; var path = ChartChordLayout.Ribbon(model, flow); var old = ChartChordLayout.Ribbon(baseline, baseline.Links[index]);
            Assert.Equal(old.Commands.Select(command => (command.Kind, command.X, command.Y)), path.Commands.Select(command => (command.Kind, command.X, command.Y)));
            // The first connector follows the source circular arc; find its target endpoint, not an arc's cubic.
            var start = model.On(flow.TargetStart, model.InnerRadius);
            var end = model.On(flow.SourceStart + flow.Sweep, model.InnerRadius);
            var connector = path.Commands.First(command => command.Kind == ChartPathCommandKind.CubicTo && command.X == start.X && command.Y == start.Y);
            var factor = tension == .8 ? .2 : 1 - tension;
            Assert.Equal(model.CenterX + (end.X - model.CenterX) * factor, connector.Control1X);
            Assert.Equal(model.CenterY + (end.Y - model.CenterY) * factor, connector.Control1Y);
            Assert.Equal(model.CenterX + (start.X - model.CenterX) * factor, connector.Control2X);
            Assert.Equal(model.CenterY + (start.Y - model.CenterY) * factor, connector.Control2Y);
            Assert.Equal(ChartChordLayout.DirectionCue(baseline, baseline.Links[index]).Commands, ChartChordLayout.DirectionCue(model, flow).Commands);
        }
    }

    [Fact]
    public void NullOrderAndDefaultTensionRetainIncumbentSvgPngAndControlArithmetic() {
        var chart = ChordDepthRecipe.Create("source"); var context = PreparedChordTests.Context();
        var baseline = chart.Prepare(context); var svg = baseline.ToSvg("default"); var png = baseline.ToPng(new VisualRenderOptions(supersampling: 1));
        chart.ConfigureChord(options => { options.Ordering = new ChartChordOrdering(); options.RibbonTension = .8; });
        var explicitDefaults = chart.Prepare(context);
        Assert.Equal(svg, explicitDefaults.ToSvg("default")); Assert.Equal(png, explicitDefaults.ToPng(new VisualRenderOptions(supersampling: 1)));
        var model = ChartChordLayout.Compute(chart.Series[0], chart.Options.Chord, new ChartRect(0, 0, 800, 520));
        Assert.Equal(.2, model.ControlPointRatio); Assert.DoesNotContain("data-cfx-ribbon-tension", svg, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(-.01)]
    [InlineData(1.01)]
    public void InvalidTensionDoesNotCommitOptions(double value) {
        var chart = ChordDepthRecipe.Create(); var options = chart.Options.Chord;
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.ConfigureChord(candidate => candidate.RibbonTension = value));
        Assert.Same(options, chart.Options.Chord); Assert.Equal(.55, options.RibbonTension);
    }
}
