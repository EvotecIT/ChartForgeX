using System.Xml.Linq;
using ChartForgeX.Markup;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MarkupAnnotationIdentityTests {
    [Fact]
    public void OptionalAnnotationIdsForwardToTheNativePolicyForAllFourKinds() {
        const string source = "labels Jan Feb Mar\nvalues 2 5 8\n"
            + "annotation hLine 4 Limit id:warning\nannotation vLine 2 Limit id=critical\n"
            + "annotation hBand 5 6 Band id:range-y\nannotation vBand 1 2 Band id:range-x\n";
        var result = new MarkupChartParser().Parse(source);
        Assert.False(result.HasErrors, string.Join("; ", result.Diagnostics.Select(item => item.Message)));
        var chart = result.Document!.Chart;
        Assert.Equal(new[] { "warning", "critical", "range-y", "range-x" }, chart.Annotations.Select(item => item.Id!.Value.Value));
        var prepared = chart.Prepare(new VisualRenderContext());
        Assert.Equal(4, prepared.Scene.Annotations.Count);
        var svg = XDocument.Parse(prepared.ToSvg());
        Assert.Contains(svg.Descendants(), node => (string?)node.Attribute("data-cfx-annotation-id") == "warning");
        Assert.NotEmpty(prepared.ToPng());
    }

    [Fact]
    public void DuplicateIdsAcrossAnnotationKindsProduceDiagnosticsWithoutAPartialDocument() {
        var result = new MarkupChartParser().Parse("values 2 5 8\nannotation hLine 4 Limit id:warning\nannotation vBand 1 2 Band id:warning\n");
        Assert.True(result.HasErrors);
        Assert.Null(result.Document);
        Assert.Contains(result.Diagnostics, item => item.Message.Contains("unique within a chart", StringComparison.Ordinal));
    }
}
