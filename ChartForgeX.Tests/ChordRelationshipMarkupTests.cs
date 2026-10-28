using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Markup;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class ChordRelationshipMarkupTests {
    internal const string Commands = """
        type chord
        title Transfers
        series Volume
        node a "Support"
        node b "Support"
        node idle "Standby"
        flow ab a b 7 colorValue=-10
        flow parallel a b 2
        flow reverse b a 3 colorValue=0
        flow self a a 1 colorValue=5
        flow zero b a 0
        option chordNodeOrder b a idle
        option chordOutgoingFlowOrder parallel self reverse ab zero
        option chordIncomingFlowOrder ab parallel zero reverse self
        option chordRibbonTension .55
        """;
    private const string Tables = """
        type chord
        title Transfers
        series Volume
        nodes:
        | id | label |
        | --- | --- |
        | a | Support |
        | b | Support |
        | idle | Standby |
        end
        flows:
        | id | source | target | value | colorValue |
        | --- | --- | --- | --- | --- |
        | ab | a | b | 7 | -10 |
        | parallel | a | b | 2 | |
        | reverse | b | a | 3 | 0 |
        | self | a | a | 1 | 5 |
        | zero | b | a | 0 | |
        end
        options:
        | key | value |
        | --- | --- |
        | chordNodeOrder | b a idle |
        | chordOutgoingFlowOrder | parallel self reverse ab zero |
        | chordIncomingFlowOrder | ab parallel zero reverse self |
        | chordRibbonTension | .55 |
        end
        """;

    [Theory]
    [InlineData(390, VisualThemeMode.Light)]
    [InlineData(390, VisualThemeMode.Dark)]
    [InlineData(800, VisualThemeMode.Light)]
    [InlineData(800, VisualThemeMode.Dark)]
    public void TypedCommandsTablesAndFenceShareManagedFactsAndExactPreparedSvgPng(int width, VisualThemeMode mode) {
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(width, 520)),
            theme: VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family), themeMode: mode);
        var direct = Chart.Create().WithTitle("Transfers").AddChord("Volume", new[] { new ChartNode("a", "Support"), new ChartNode("b", "Support"), new ChartNode("idle", "Standby") }, new[] {
            new ChartFlowLink("ab", "a", "b", 7, -10), new ChartFlowLink("parallel", "a", "b", 2),
            new ChartFlowLink("reverse", "b", "a", 3, 0), new ChartFlowLink("self", "a", "a", 1, 5), new ChartFlowLink("zero", "b", "a", 0)
        }).ConfigureChord(options => {
            options.Ordering = new ChartChordOrdering(new[] { "b", "a", "idle" }, new[] { "parallel", "self", "reverse", "ab", "zero" }, new[] { "ab", "parallel", "zero", "reverse", "self" });
            options.RibbonTension = .55;
        }).Prepare(context);
        var expectedSvg = direct.ToSvg("parity"); var expectedPng = direct.ToPng();
        foreach (var source in new[] { Commands, Tables }) {
            var parsed = new MarkupChartParser().Parse(source); Assert.Empty(parsed.Diagnostics);
            var artifact = new VisualMarkupParser().Parse("```chartforgex chart v1\n" + source + "\n```"); Assert.Empty(artifact.Diagnostics);
            var models = new[] { parsed.Document!.Chart, Assert.IsType<Chart>(Assert.Single(artifact.Artifacts).Model) };
            foreach (var model in models) {
                var actual = model.Prepare(context); var actualPng = actual.ToPng();
                Assert.Equal(expectedSvg, actual.ToSvg("parity")); Assert.Equal(expectedPng, actualPng);
                Assert.Equal(new[] { "ab", "parallel", "reverse", "self", "zero" }, model.Series[0].FlowLinks.Select(flow => flow.Id));
                Assert.Equal(new double?[] { -10, null, 0, 5, null }, model.Series[0].FlowLinks.Select(flow => flow.ColorValue));
                Assert.Empty(model.Series[0].Points);
                Assert.DoesNotContain(XDocument.Parse(actual.ToSvg()).Descendants(), node => node.Name.LocalName == "script");
                var image = RasterImageDecoder.Decode(actualPng); Assert.Equal(width, image.Width); Assert.Equal(520, image.Height);
                ChordOrderingTests.Save("chord-markup-" + width + "-" + mode + (source == Commands ? "-commands" : "-tables"), actual);
            }
        }
    }

    [Theory]
    [InlineData("type chord\nnode a A\nnode a Again", 3)]
    [InlineData("type chord\nnode a A\nflow bad a missing 1", 3)]
    [InlineData("type chord\nnode a A\nflow self a a 1\nflow self a a 2", 4)]
    [InlineData("type chord\nnode a A\nflow self a a -1", 3)]
    [InlineData("type chord\nnode a A\nflow self a a NaN", 3)]
    [InlineData("type chord\nnode a A\noption chordNodeOrder a missing", 3)]
    [InlineData("type chord\nnode a A\noption chordRibbonTension 2", 3)]
    [InlineData("type chord\nnode a A\nflow self a a 1 unknown=2", 3)]
    [InlineData("type line\nnode a A", 2)]
    [InlineData("type chord\nnodes:\ntitle Accidental", 3)]
    [InlineData("type chord\nnode a A\nflow self a a 1 ignored", 3)]
    public void InvalidTypedSourceHasLocatedErrorsAndNoPartialChart(string source, int line) {
        var parsed = new MarkupChartParser().Parse(source);
        Assert.True(parsed.HasErrors); Assert.Null(parsed.Document);
        Assert.Contains(parsed.Diagnostics, item => item.Line == line && item.Severity == MarkupDiagnosticSeverity.Error);
    }

    [Fact]
    public void ExplicitEmptyTypedChordCanRenderAndRefillWithoutFabricatedNumericSlots() {
        var parsed = new MarkupChartParser().Parse("type chord"); Assert.Empty(parsed.Diagnostics);
        var chart = parsed.Document!.Chart; var source = Assert.Single(chart.Series); Assert.Empty(source.Nodes); Assert.Empty(source.FlowLinks); Assert.Empty(source.Points);
        Assert.Contains(chart.Prepare(PreparedChordTests.Context()).Diagnostics, item => item.Code == "chord.no-positive-flow");
        source.ReplaceFlowData(new[] { new ChartNode("only", "Only") }, new[] { new ChartFlowLink("self", "only", "only", 3) });
        Assert.Single(chart.Prepare(PreparedChordTests.Context()).Scene.Nodes, item => item.Role == "chord-ribbon");
    }
}
