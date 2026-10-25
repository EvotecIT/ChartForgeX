using ChartForgeX.Primitives;
using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidTreemapValueFormatMappingTests {
    private static string Source(string format) => "---\nconfig:\n  treemap:\n    valueFormat: '" + format + "'\n---\ntreemap-beta\n\"Root\"\n  \"Measured\": 1234.5\n  \"Zero\": 0";

    [Theory]
    [InlineData("", "1,234.5")]
    [InlineData(",", "1,234.5")]
    [InlineData("$", "$1234.5")]
    [InlineData("$0,0", "$1,234.5")]
    [InlineData(".0f", "1235")]
    [InlineData(".1f", "1234.5")]
    [InlineData(".2f", "1234.50")]
    [InlineData(",.2f", "1,234.50")]
    [InlineData("$.2f", "$1234.50")]
    [InlineData("$,.2f", "$1,234.50")]
    [InlineData(".1%", "123450.0%")]
    public void FiniteSourcePatternsUseImmutableNativeFormatWithoutChangingMagnitudes(string pattern, string expected) {
        var culture = CultureInfo.CurrentCulture;
        try {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            var result = MermaidRenderer.Render(Source(pattern)); Assert.Empty(result.Diagnostics);
            var chart = Assert.IsType<Chart>(result.Artifact!.Model);
            Assert.Equal(expected, chart.Options.ValueFormat.Format(1234.5));
            Assert.Equal(pattern, result.Artifact.ToInterchangeEnvelope().Extensions["mermaid.treemap.source.valueFormat"]);
            Assert.Equal(1234.5, chart.Series[0].HierarchyItems[1].Value);
            var xml = System.Xml.Linq.XDocument.Parse(result.Artifact.ToSvg());
            Assert.Equal(expected, (string?)MermaidTreemapStyleMappingTests.Node(xml, "node-1").Attribute("data-cfx-formatted-value"));
            Assert.Equal("zero", (string?)MermaidTreemapStyleMappingTests.Node(xml, "node-2").Attribute("data-cfx-geometry-status"));
            var fence = new MermaidVisualMarkupParser().Parse("```mermaid\n" + Source(pattern) + "\n```");
            Assert.Empty(fence.Diagnostics); Assert.Equal(result.Artifact.ToSvg(), Assert.Single(fence.Artifacts).ToSvg());
        } finally { CultureInfo.CurrentCulture = culture; }
    }

    [Fact]
    public void LaterSourcePolicyAndCapturedHostPolicyUseTheSameOwnerWhileHiddenValuesKeepFacts() {
        var source = Source(".2f").Replace("treemap-beta", "%%{init: {'treemap': {'valueFormat': '.1%', 'showValues': false}}}%%\ntreemap-beta");
        var result = MermaidRenderer.Render(source); Assert.Empty(result.Diagnostics);
        var chart = Assert.IsType<Chart>(result.Artifact!.Model);
        Assert.False(chart.Options.Treemap.ShowValues); Assert.Equal("123450.0%", chart.Options.ValueFormat.Format(1234.5));
        var policy = ChartValueFormat.Number("0.000", CultureInfo.InvariantCulture);
        var host = new MermaidTreemapRenderOptions { ValueFormat = policy };
        var options = new MermaidRenderOptions { Treemap = host };
        var direct = MermaidRenderer.Render(source, options);
        var parser = new MermaidVisualMarkupParser(options); host.ValueFormat = ChartValueFormat.Number("0");
        var fenced = parser.Parse("```mermaid\n" + source + "\n```");
        Assert.Equal("1234.500", Assert.IsType<Chart>(direct.Artifact!.Model).Options.ValueFormat.Format(1234.5));
        Assert.Equal(direct.Artifact.ToSvg(), Assert.Single(fenced.Artifacts).ToSvg());
        Assert.Same(policy, Assert.IsType<Chart>(fenced.Artifacts[0].Model).Options.ValueFormat);
    }

    [Theory]
    [InlineData(".2s")]
    [InlineData("20.2f")]
    [InlineData(".16f")]
    [InlineData("+,.2f")]
    [InlineData("not-a-format")]
    public void OtherD3PatternsRetainOneLocatedWarningAndStrictHostsRejectInsteadOfEvaluatingThem(string pattern) {
        var result = MermaidRenderer.Render(Source(pattern)); Assert.False(result.HasErrors); Assert.NotNull(result.Artifact);
        var warning = Assert.Single(result.Diagnostics); Assert.Equal(4, warning.Span.Line);
        Assert.Equal(MermaidDiagnosticCodes.UnsupportedConfiguration, warning.Code);
        var strict = new MermaidRenderOptions { WarningPolicy = MermaidWarningPolicy.Reject };
        Assert.Null(MermaidRenderer.Render(Source(pattern), strict).Artifact);
        Assert.Empty(new MermaidVisualMarkupParser(strict).Parse("```mermaid\n" + Source(pattern) + "\n```").Artifacts);
    }

    [Theory]
    [InlineData("4")]
    [InlineData("false")]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("[]")]
    public void NonStringFormatsAreLocatedConfigurationErrors(string value) {
        var source = Source(".2f").Replace("'.2f'", value);
        var result = MermaidRenderer.Render(source); Assert.True(result.HasErrors); Assert.Null(result.Artifact);
        var error = Assert.Single(result.Diagnostics); Assert.Equal(4, error.Span.Line);
        Assert.Equal(MermaidDiagnosticCodes.InvalidConfiguration, error.Code);
    }
}
