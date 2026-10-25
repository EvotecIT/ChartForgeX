using System.Text.Json;
using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed class MermaidCrossingPaintTests {
    [Theory]
    [InlineData("absent")] [InlineData("null")] [InlineData("false")] [InlineData("true")] [InlineData("\"arc\"")] [InlineData("\"gap\"")]
    public void SourceKeepsPinnedDefaultAndUnsupportedRoundedGraphsRemainAnErrorFreePaintNoOp(string value) {
        var source = CrossingPaintControlFixture.Swimlane(value); var parsed = new MermaidParser().ParseFlowchart(source);
        var chart = parsed.Document!.ToTopologyChart(); var prepared = chart.Prepare();
        CrossingPaintControlFixture.Capture(prepared.Visual, "crossing-after-source-" + value.Replace("\"", ""));
        Assert.Empty(parsed.Diagnostics); Assert.All(chart.Edges, edge => Assert.Equal(TopologyEdgeInterpolation.Rounded, edge.Interpolation));
        if (value == "false") Assert.Null(chart.CrossingPaint);
        else { Assert.NotNull(chart.CrossingPaint); Assert.Equal(TopologyCrossingCurveHandling.SkipUnsupported, chart.CrossingPaint.CurveHandling); Assert.Equal(value == "\"gap\"" ? TopologyCrossingPaintStyle.Gap : TopologyCrossingPaintStyle.Arc, chart.CrossingPaint.Style); }
        chart.CrossingPaint = null; var ordinary = chart.Prepare(); Assert.Equal(ordinary.ToSvg(), prepared.ToSvg()); Assert.Equal(ordinary.ToPng(), prepared.ToPng());
    }
    [Theory] [InlineData("true")] [InlineData("\"arc\"")] [InlineData("\"gap\"")]
    public void ExplicitLinearSourceUsesTheNativeResolvedPathAndRetainsSourcePolicy(string value) {
        var source = CrossingPaintControlFixture.Swimlane(value, "linear"); var parsed = new MermaidParser().ParseFlowchart(source);
        var chart = parsed.Document!.ToTopologyChart(); var prepared = chart.Prepare(); CrossingPaintControlFixture.Capture(prepared.Visual, "crossing-linear-source-" + value.Replace("\"", ""));
        Assert.Empty(parsed.Diagnostics); Assert.All(chart.Edges, edge => Assert.Equal(TopologyEdgeInterpolation.Linear, edge.Interpolation));
        Assert.Equal(TopologyCrossingCurveHandling.SkipUnsupported, prepared.ToInterchangeEnvelope().Topology!.CrossingPaint!.CurveHandling);
        Assert.Equal(source, parsed.Document!.SourceText); Assert.NotEmpty(prepared.ToPng());
    }
    [Theory] [InlineData("7")] [InlineData("\"lineJumps\"")] [InlineData("{}")] [InlineData("[]")]
    public void UnsupportedSourceValuesProduceLocatedErrorsInsteadOfGuessingPaint(string value) {
        var source = CrossingPaintControlFixture.Swimlane(value); var parsed = new MermaidParser().ParseFlowchart(source);
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) {
            Directory.CreateDirectory(output); File.WriteAllText(Path.Combine(output, "crossing-invalid-" + value.Replace("\"", "").Replace("{", "object").Replace("}", "").Replace("[", "array").Replace("]", "") + ".diagnostics.json"), JsonSerializer.Serialize(parsed.Diagnostics));
        }
        Assert.Contains(parsed.Diagnostics, diagnostic => diagnostic.Severity == MermaidDiagnosticSeverity.Error && diagnostic.Span.Line == 1 && diagnostic.Message.Contains("lineHops", StringComparison.Ordinal));
    }
    [Fact]
    public void DirectivePrecedenceUsesTheExistingLocatedConfigurationOwner() {
        var source = "---\nconfig:\n  swimlane:\n    lineHops: false\n---\n" + CrossingPaintControlFixture.Swimlane("\"gap\"");
        var parsed = new MermaidParser().ParseFlowchart(source); var chart = parsed.Document!.ToTopologyChart();
        CrossingPaintControlFixture.Capture(chart.Prepare().Visual, "crossing-source-precedence"); Assert.Empty(parsed.Diagnostics);
        Assert.Equal(TopologyCrossingPaintStyle.Gap, chart.CrossingPaint!.Style); Assert.Equal(TopologyCrossingCurveHandling.SkipUnsupported, chart.CrossingPaint.CurveHandling);
        Assert.Equal(2, parsed.Document!.Configuration.Settings.Count(setting => setting.Path == "swimlane.lineHops"));
    }
}
