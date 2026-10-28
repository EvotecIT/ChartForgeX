using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidEventModelingReviewBoundaryTests {
    [Theory]
    [InlineData("data Payload `text`{Say \"hello")]
    [InlineData("data Payload `text`{")]
    public void DataExamplesInsideAccessibilityDescriptionsCannotHideLaterRealConfiguration(string example) {
        var source = "eventmodeling\naccDescr {\n" + example + "\n}\n%%{init: {\"theme\":\"dark\"}}%%\ntf 1 ui Screen";
        var parsed = new MermaidParser().ParseEventModeling(source);
        Assert.False(parsed.HasErrors);
        Assert.Equal("dark", parsed.Document!.Configuration.Theme);
        Assert.Equal(source, parsed.Document.SourceText);
    }

    [Fact]
    public void ConfigurationExamplesInRealDataRemainOpaque() {
        const string body = "\n%%{init: {\"theme\":\"dark\"}}%%\n";
        var parsed = new MermaidParser().ParseEventModeling("eventmodeling\ndata Payload `text`{" + body + "}\ntf 1 ui Screen [[Payload]]");
        Assert.False(parsed.HasErrors);
        Assert.Null(parsed.Document!.Configuration.Theme);
        Assert.Equal(body, Assert.Single(parsed.Document.DataBlocks).Content);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NativeComposedDetailRejectsAtTheSourceBoundaryEvenWhenItsLiteralTableCellFits(bool withInline) {
        var parsed = new MermaidParser().ParseEventModeling(Source(VisualArtifactInterchangeValidation.MaximumTextCharacters, withInline));
        Assert.False(parsed.HasErrors);
        var document = parsed.Document!;
        Assert.Equal(VisualArtifactInterchangeValidation.MaximumTextCharacters, Assert.Single(document.DataBlocks).ToDataTable().Rows[0].Cells[2].DisplayText.Length);
        var error = Assert.Throws<MermaidResourceLimitException>(() => document.ToTopologyChart());
        Assert.Equal("Interchange.MaximumTextCharacters", error.LimitName);
        Assert.True(error.Actual > error.Limit);
        Assert.Equal(2, error.Span.Line);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RenderingReportsLocatedResourceFailureForOversizedComposedDetails(bool withInline) {
        var result = MermaidRenderer.Render(Source(VisualArtifactInterchangeValidation.MaximumTextCharacters, withInline));
        Assert.True(result.HasErrors); Assert.Null(result.Artifact);
        Assert.Contains(result.Diagnostics, item => item.Code == MermaidDiagnosticCodes.ResourceLimitExceeded && item.Span.Line == 2);
        Assert.DoesNotContain(result.Diagnostics, item => item.Code == MermaidDiagnosticCodes.ConversionFailed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CompleteComposedValueAtTheExistingTextLimitRemainsAccepted(bool withInline) {
        const int literalPrefixLength = 9; // "Literal: " is native factual presentation.
        var parsed = new MermaidParser().ParseEventModeling(Source(VisualArtifactInterchangeValidation.MaximumTextCharacters - literalPrefixLength, withInline));
        Assert.False(parsed.HasErrors);
        var chart = parsed.Document!.ToTopologyChart();
        Assert.Equal(VisualArtifactInterchangeValidation.MaximumTextCharacters, chart.Nodes[0].Details.Last().Value!.Length);
    }

    [Fact]
    public void ExpandedJsonPointerFactsRespectThePortableBoundBeforeTableRowsAreReturned() {
        var key = new string('/', 7000);
        var body = string.Concat(Enumerable.Repeat("\n\"" + key + "\":{", 5)) + "\"leaf\":1" + new string('}', 5);
        var parsed = new MermaidParser().ParseEventModeling("eventmodeling\ntf 1 ui Screen [[Payload]]\ndata Payload `json`{" + body + "}");
        if (parsed.HasErrors) {
            Assert.Contains(parsed.Diagnostics, item => item.Code == MermaidDiagnosticCodes.ResourceLimitExceeded && item.Span.Line >= 2);
            return;
        }
        var error = Assert.Throws<MermaidResourceLimitException>(() => parsed.Document!.ToDataTable("1"));
        Assert.Equal("Interchange.MaximumTextCharacters", error.LimitName);
        Assert.True(error.Actual > error.Limit);
    }

    private static string Source(int length, bool withInline) {
        var body = new System.Text.StringBuilder(length);
        while (length > 1024) { body.Append('x', 1023).Append('\n'); length -= 1024; }
        body.Append('x', length);
        return "eventmodeling\ntf 1 ui Screen " + (withInline ? "{label: short} " : "") + "[[Payload]]\ndata Payload `text`{" + body + "}";
    }
}
