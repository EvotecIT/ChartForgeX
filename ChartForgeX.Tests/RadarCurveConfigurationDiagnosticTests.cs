using ChartForgeX.Mermaid;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class RadarCurveConfigurationDiagnosticTests {
    private const string Body = "radar-beta\naxis a[A], b[B], c[C]\ncurve sample[Sample]{3, 5, 4}\ngraticule polygon\n";

    [Theory]
    [InlineData("0")]
    [InlineData("0.25")]
    public void PolygonReportsTheRetainedSourceTensionAtItsSpan(string tension) {
        var parsed = new MermaidParser().ParseRadar("---\nconfig:\n  radar:\n    curveTension: " + tension + "\n---\n" + Body);
        Assert.False(parsed.HasErrors);
        var warning = Assert.Single(parsed.Diagnostics, diagnostic => diagnostic.Code == MermaidDiagnosticCodes.UnsupportedConfiguration);
        var setting = parsed.Document!.Configuration.Settings.Single(setting => setting.Path == "radar.curveTension");
        Assert.Equal(setting.Span, warning.Span);
        Assert.Equal(MermaidDiagnosticSeverity.Warning, warning.Severity);
        Assert.Equal(0, parsed.Document.ToChart().Series[0].Radar.CurveTension);
        Assert.Equal(.4, parsed.Document.ToChart(new MermaidRadarRenderOptions { CurveTension = .4 }).Series[0].Radar.CurveTension);
    }

    [Fact]
    public void PolygonWarnsOnlyForTheEffectiveDirectiveAndRetainsValidation() {
        var parsed = new MermaidParser().ParseRadar("---\nconfig:\n  radar:\n    curveTension: 1\n---\n%%{init: {'radar': {'curveTension': 0.25}}}%%\n" + Body);
        var warning = Assert.Single(parsed.Diagnostics);
        Assert.Equal(6, warning.Span.Line);
        Assert.Equal(MermaidDiagnosticCodes.UnsupportedConfiguration, warning.Code);
        Assert.Equal("0.25", parsed.Document!.Configuration.Settings.Last(setting => setting.Path == "radar.curveTension").Value);
        var invalid = new MermaidParser().ParseRadar("%%{init: {'radar': {'curveTension': 2}}}%%\n" + Body);
        Assert.True(invalid.HasErrors);
        Assert.DoesNotContain(invalid.Diagnostics, diagnostic => diagnostic.Code == MermaidDiagnosticCodes.UnsupportedConfiguration);
        Assert.Empty(new MermaidParser().ParseRadar(Body.Replace("polygon", "circle")).Diagnostics);
    }
}
