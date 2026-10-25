using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidCrossingConfigurationTests {
    [Theory]
    [InlineData("json-null", "null", false, true)] [InlineData("json-array", "[]", false, false)] [InlineData("json-object", "{}", false, false)]
    [InlineData("json-nested-array", "[1]", false, false)] [InlineData("json-nested-object", "{\"x\":1}", false, false)]
    [InlineData("json-quoted-null", "\"null\"", false, false)] [InlineData("json-boolean", "true", false, true)]
    [InlineData("yaml-null", "null", true, true)] [InlineData("yaml-tilde", "~", true, true)]
    [InlineData("yaml-array", "[]", true, false)] [InlineData("yaml-object", "{}", true, false)]
    [InlineData("yaml-block-array", "\n      - item", true, false)] [InlineData("yaml-block-object", "\n      x: 1", true, false)]
    public void ActualScalarNullDiffersFromEmptyStructuredAndOpaqueValuesWithoutChangingPublicProjection(string name, string value, bool yaml, bool accepted) {
        var source = MermaidCrossingConfigurationBeforeControls.Source(value, yaml); var parsed = new MermaidParser().ParseFlowchart(source);
        MermaidCrossingConfigurationBeforeControls.Capture("after-" + name, source, parsed);
        Assert.NotNull(parsed.Document);
        if (accepted) { Assert.Empty(parsed.Diagnostics); Assert.Equal(TopologyCrossingPaintStyle.Arc, parsed.Document.ToTopologyChart().CrossingPaint!.Style); }
        else Assert.Contains(parsed.Diagnostics, diagnostic => diagnostic.Severity == MermaidDiagnosticSeverity.Error && diagnostic.Message.Contains("lineHops", StringComparison.Ordinal) && diagnostic.Span.Line >= 1);
    }
}
