using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidXYChartSourceDepthTests {
    internal static string Source(bool horizontal = false) => XYChartSourceDepthFixture.Read(horizontal);
    internal static MermaidRenderOptions Options(int width, bool dark, bool? labels = null) => new() {
        XYChart = new MermaidXYChartRenderOptions { Width = width, Height = 520, ShowDataLabels = labels,
            Presentation = new MermaidPresentationOptions { ThemeMode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light, FontFamily = BrowserProofFonts.Family } }
    };

    [Fact]
    public void QuotedCaptionsRetainDatumIndexRawEntityTokenAndPreciseSourceSpan() {
        const string source = "xychart-beta\n  line \"Delivery\" [12 \"Opened\", 60, 33 \"Reviewed, ready\", 75 \"Release #quot;B#quot;\"]";
        var parsed = new MermaidParser().ParseXYChart(source); Assert.Empty(parsed.Diagnostics);
        var series = Assert.Single(parsed.Document!.Series); Assert.Equal("Delivery", series.Name);
        Assert.Equal(new[] { 12d, 60, 33, 75 }, series.Values);
        Assert.Equal(new[] { 0, 2, 3 }, series.PointLabels.Select(label => label.PointIndex));
        Assert.Equal(new[] { "Opened", "Reviewed, ready", "Release #quot;B#quot;" }, series.PointLabels.Select(label => label.Text));
        var line = source.Split('\n')[1];
        foreach (var label in series.PointLabels) {
            Assert.Equal(2, label.Span.Line);
            Assert.Equal(label.RawText, line.Substring(label.Span.Column - 1, label.Span.Length));
        }
        Assert.Equal("Release \"B\"", parsed.Document.ToChart().Series[0].PointLabels[3]);
        Assert.Equal(source, parsed.Document.SourceText);
        Assert.Equal(line.Trim(), Assert.Single(parsed.Document.Statements).Text);
    }

    [Theory]
    [InlineData("[1 Open]")] [InlineData("[1 \"open\" trailing]")]
    [InlineData("[NaN \"open\"]")] [InlineData("[1 \"open\",,2]")]
    [InlineData("[1 \"open]")]
    public void MalformedCaptionDataReturnsLocatedErrorsWithoutDirectOrFenceArtifacts(string list) {
        var source = "xychart-beta\nline " + list;
        var direct = MermaidRenderer.Render(source); Assert.True(direct.HasErrors); Assert.Null(direct.Artifact);
        Assert.Contains(direct.Diagnostics, diagnostic => diagnostic.Severity == MermaidDiagnosticSeverity.Error && diagnostic.Span.Line == 2);
        var fenced = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```"); Assert.True(fenced.HasErrors); Assert.Empty(fenced.Artifacts);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void LineCaptionsUseExistingPointLabelsAndBarValueVisibilityRemainsIndependent(bool horizontal) {
        var parsed = new MermaidParser().ParseXYChart(Source(horizontal)); Assert.Empty(parsed.Diagnostics);
        var document = Assert.IsType<MermaidXYChartDocument>(parsed.Document);
        var chart = document.ToChart(); var line = chart.Series[1];
        Assert.Equal(new[] { "Intake", "Completed" }, chart.Series.Select(series => series.Name)); Assert.True(chart.Options.ShowLegend);
        Assert.Equal(new[] { "Opened", "", "Reviewed, ready", "Released" }, line.PointLabels);
        Assert.True(line.ShowDataLabels); Assert.False(chart.Series[0].ShowDataLabels);
        Assert.Equal(ChartDataLabelPlacement.Outside, chart.Series[0].DataLabelPlacement);
        Assert.Equal(new[] { 12d, 60, 33, 75 }, line.Points.Select(point => point.Y));
        var hosted = document.ToChart(new MermaidXYChartRenderOptions { ShowDataLabels = true });
        Assert.True(hosted.Options.ShowDataLabels); Assert.Null(hosted.Series[1].PointLabels[1]);
        var hidden = document.ToChart(new MermaidXYChartRenderOptions { ShowDataLabels = false });
        Assert.False(hidden.Options.ShowDataLabels); Assert.Null(hidden.Series[1].ShowDataLabels);
        Assert.Equal("Opened", hidden.Series[1].PointLabels[0]);
    }

    [Theory]
    [InlineData("bar", 390, false)] [InlineData("bar", 390, true)] [InlineData("bar", 1100, false)] [InlineData("bar", 1100, true)]
    [InlineData("line", 390, false)] [InlineData("line", 390, true)] [InlineData("line", 1100, false)] [InlineData("line", 1100, true)]
    [InlineData("mixed", 390, false)] [InlineData("mixed", 390, true)] [InlineData("mixed", 1100, false)] [InlineData("mixed", 1100, true)]
    public void CompleteFamilyNativeAndFenceUseIdenticalSvgPngAndPortableFacts(string family, int width, bool dark) {
        var source = Source(horizontal: width == 390);
        if (family == "bar") source = source.Substring(0, source.IndexOf("line \"Completed\"", StringComparison.Ordinal));
        if (family == "line") source = source.Replace("bar \"Intake\" [18, 70, 44, 85]\n", "", StringComparison.Ordinal);
        var options = Options(width, dark); var direct = MermaidRenderer.Render(source, options); Assert.Empty(direct.Diagnostics);
        var artifact = direct.Artifact!; var fenced = new MermaidVisualMarkupParser(options).Parse("```mermaid\n" + source + "\n```");
        Assert.Empty(fenced.Diagnostics); var other = Assert.Single(fenced.Artifacts);
        var svg = artifact.ToSvg(); var png = artifact.ToPng(); Capture("xy-source-" + family + "-" + width + (dark ? "-dark" : "-light"), artifact);
        Assert.Equal(svg, other.ToSvg()); Assert.Equal(png, other.ToPng());
        var pixels = RasterImageDecoder.Decode(png); Assert.Equal(width, pixels.Width); Assert.Equal(520, pixels.Height);
        Assert.DoesNotContain(XDocument.Parse(svg).Descendants(), node => node.Name.LocalName is "script" or "image" or "foreignObject");
        var model = Assert.IsType<Chart>(artifact.Model); Assert.Equal(family == "mixed" ? 2 : 1, model.Series.Count);
        var portable = VisualArtifactInterchangeEnvelope.FromJson(artifact.ToInterchangeJson()); Assert.Equal(artifact.ToInterchangeJson(), portable.ToJson());
        Assert.Equal(artifact.Metadata["mermaid.orientation"], portable.Extensions["mermaid.orientation"]);
        Assert.Equal(artifact.Metadata["mermaid.pointLabels"], portable.Extensions["mermaid.pointLabels"]);
        if (family != "bar") {
            foreach (var caption in new[] { "Opened", "Reviewed, ready", "Released" }) Assert.Contains(caption, model.Series.Last().PointLabels);
            Assert.Equal("3", artifact.Metadata["mermaid.pointLabels"]);
        }
    }

    [Fact]
    public void UnconfiguredNumericSourcePreservesTheOriginalNativeDefaultsAndFallbackLegendNames() {
        const string source = "xychart-beta\ntitle Tickets\nx-axis Month [Jan,Feb,Mar]\ny-axis Count 0 --> 100\nbar [20,50,80]\nline [30,60,90]";
        var actual = new MermaidParser().ParseXYChart(source).Document!.ToChart();
        var expected = Chart.Create().WithTitle("Tickets").WithSubtitle("").WithSize(900, 520).WithXAxis("Month").WithYAxis("Count")
            .AddBar("Bar 1", new[] { new ChartPoint(1,20), new ChartPoint(2,50), new ChartPoint(3,80) })
            .AddLine("Line 1", new[] { new ChartPoint(1,30), new ChartPoint(2,60), new ChartPoint(3,90) });
        expected.Options.YAxis.Minimum = 0; expected.Options.YAxis.Maximum = 100;
        for (var index = 0; index < 3; index++) expected.Options.XAxis.Labels.Add(new ChartAxisLabel(index + 1, new[] { "Jan", "Feb", "Mar" }[index]));
        Assert.True(actual.Options.ShowLegend); Assert.Null(actual.Options.ShowDataLabels);
        Assert.All(actual.Series, series => { Assert.Null(series.ShowDataLabels); Assert.Empty(series.PointLabels); });
        Assert.Equal(expected.ToSvg(), actual.ToSvg()); Assert.Equal(expected.ToPng(), actual.ToPng());
    }

    [Fact]
    public void RawBarItemCaptionsAreRetainedWithoutInventingBarCaptionSemantics() {
        var parsed = new MermaidParser().ParseXYChart("xychart-beta\nbar \"Intake\" [1 \"retained\", 2]"); Assert.Empty(parsed.Diagnostics);
        Assert.Equal("retained", Assert.Single(parsed.Document!.Series[0].PointLabels).Text);
        Assert.Empty(parsed.Document.ToChart().Series[0].PointLabels);
    }

    [Fact]
    public void RichCaptionLossIsLocatedAndStrictPolicyCannotPublishAFallbackArtifact() {
        const string source = "xychart-beta\nline [1 \"<b>unsafe</b> $$x$$\", 2]";
        var direct = MermaidRenderer.Render(source); Assert.False(direct.HasErrors);
        Assert.Contains(direct.Diagnostics, warning => warning.Span.Line == 2 && warning.Code == MermaidDiagnosticCodes.UnsupportedStatement);
        var svg = XDocument.Parse(direct.Artifact!.ToSvg()); Assert.DoesNotContain(svg.Descendants(), node => node.Name.LocalName == "b");
        var strict = MermaidRenderer.Render(source, new MermaidRenderOptions { WarningPolicy = MermaidWarningPolicy.Reject });
        Assert.True(strict.HasErrors); Assert.Null(strict.Artifact);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void NumericXCaptionsRetainTheExistingInterpolatedDomainAndSingletonPolicy(bool horizontal) {
        var source = "xychart-beta" + (horizontal ? " horizontal" : "") + "\nx-axis Time 2 --> 8\ny-axis Count 0 --> 100\nline \"Delivery\" [10 \"First\", 50, 75 \"Last\"]";
        var parsed = new MermaidParser().ParseXYChart(source); Assert.Empty(parsed.Diagnostics); var chart = parsed.Document!.ToChart();
        Assert.Equal(new[] { 2d, 5, 8 }, chart.Series[0].Points.Select(point => point.X));
        Assert.Equal(new[] { "First", "", "Last" }, chart.Series[0].PointLabels);
        Assert.Equal(2, (horizontal ? chart.Options.YAxis : chart.Options.XAxis).Minimum);
        var single = new MermaidParser().ParseXYChart(source.Replace("[10 \"First\", 50, 75 \"Last\"]", "[10 \"First\"]", StringComparison.Ordinal)).Document!.ToChart();
        Assert.Equal(2, Assert.Single(single.Series[0].Points).X);
    }

    [Fact]
    public void EditedTypedCaptionIndicesCannotAllocateAnUnboundedNativeLabelList() {
        var document = new MermaidParser().ParseXYChart("xychart-beta\nline [1,2]").Document!;
        document.Series[0].PointLabels.Add(new MermaidXYChartPointLabel(int.MaxValue, "invalid", "", new MermaidSourceSpan(2, 1, 1)));
        Assert.Throws<ArgumentException>(() => document.ToChart());
    }

    internal static void Capture(string name, VisualArtifact artifact) {
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(output)) return;
        Directory.CreateDirectory(output); var stem = Path.Combine(output, name);
        File.WriteAllText(stem + ".svg", artifact.ToSvg()); File.WriteAllBytes(stem + ".native.png", artifact.ToPng());
        File.WriteAllText(stem + ".native.json", artifact.ToInterchangeJson());
    }
}
