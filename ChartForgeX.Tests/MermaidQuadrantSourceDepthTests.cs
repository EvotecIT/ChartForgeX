using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Quadrant source declarations map through native owners without losing raw facts or diagnostics.</summary>
public sealed class MermaidQuadrantSourceDepthTests {
    private const string Body = "quadrantChart\ntitle Priorities\nx-axis Low reach --> High reach\ny-axis Low confidence --> High confidence\nquadrant-1 Expand\nquadrant-2 Assess\nquadrant-3 Defer\nquadrant-4 Improve\nObserved: [0.2, 0.8]\n";
    private const string Configured = "%%{init: {'quadrantChart': {'chartWidth': 720, 'chartHeight': 480, 'pointRadius': 5, 'pointLabelFontSize': 15, 'titleFontSize': 21}, 'themeVariables': {'quadrant1Fill': '#FDE68A', 'quadrant2Fill': '#BFDBFE', 'quadrant3Fill': '#DDD6FE', 'quadrant4Fill': '#A7F3D0', 'quadrantPointFill': '#334455', 'quadrantPointTextFill': '#123456', 'quadrantXAxisTextFill': '#654321', 'quadrantTitleFill': '#234567'}}}%%\n" + Body;

    [Fact]
    public void ClassReplacementAndDirectOverridesUseNativeMarkersWithoutAnImplicitDefaultClass() {
        const string source = "%%{init: {'quadrantChart': {'pointRadius': 4}, 'themeVariables': {'quadrantPointFill': '#334455'}}}%%\n"
            + "quadrantChart\nOrdinary: [0.2, 0.2]\nSelected:::default: [0.4, 0.4]\nReplaced:::focus: [0.6, 0.6]\n"
            + "Direct:::focus: [0.8, 0.8] radius:9,color:#ABC,stroke-width:2px\n"
            + "classDef default color:#E11D48\nclassDef focus radius:11,color:#123456,stroke-color:#000,stroke-width:4px\nclassDef focus color:#654321";
        var parsed = new MermaidParser().ParseQuadrant(source);
        Assert.Empty(parsed.Diagnostics); var document = parsed.Document!; var chart = document.ToChart(); var series = Assert.Single(chart.Series);
        Assert.Equal(ChartSeriesKind.Scatter, series.Kind);
        Assert.Equal(new[] { "Ordinary", "Selected", "Replaced", "Direct" }, document.Points.Select(point => point.Label));
        Assert.Equal(new string?[] { null, "default", "focus", "focus" }, document.Points.Select(point => point.ClassName));
        Assert.Equal(new[] { "default", "focus", "focus" }, document.ClassDefinitions.Select(definition => definition.Name));
        Assert.Null(series.GetPointMarkers(0));
        Assert.Equal(ChartColor.FromHex("#E11D48"), series.GetPointMarkers(1)!.Fill);
        var replaced = series.GetPointMarkers(2)!;
        Assert.Null(replaced.Radius); Assert.Equal(ChartColor.FromHex("#654321"), replaced.Fill);
        Assert.Equal(0, replaced.StrokeWidth); Assert.Equal(ChartColor.FromHex("#334455"), replaced.Stroke);
        var direct = series.GetPointMarkers(3)!;
        Assert.Equal(9, direct.Radius); Assert.Equal(ChartColor.FromHex("#AABBCC"), direct.Fill);
        Assert.Equal(2, direct.StrokeWidth); Assert.Equal(ChartColor.FromHex("#334455"), direct.Stroke);
        var marks = Role(XDocument.Parse(chart.ToSvg()), "mermaid-quadrant-point");
        Assert.Equal(new[] { "#334455", "#E11D48", "#654321", "#AABBCC" }, marks.Select(mark => mark.RenderedColor("fill").ToHex()));
        Assert.Equal(new[] { "4", "4", "4", "9" }, marks.Select(mark => (string?)mark.Attribute("rx")));
        Assert.Equal("none", (string?)marks[2].Attribute("stroke"));
        Assert.Equal("#334455", (string?)marks[3].Attribute("stroke"));
    }

    [Fact]
    public void SourceDimensionsPaintAndTypographyReachTheirExistingNativeOwners() {
        var parsed = new MermaidParser().ParseQuadrant(Configured); Assert.Empty(parsed.Diagnostics);
        var chart = parsed.Document!.ToChart(); var series = Assert.Single(chart.Series);
        Assert.Equal((720d, 480d), (chart.Options.Size.Width, chart.Options.Size.Height));
        Assert.Equal(5, series.Markers.Radius); Assert.Equal(ChartColor.FromHex("#334455"), series.Markers.Fill);
        Assert.Equal(15, series.DataLabelStyle.FontSize); Assert.Equal(ChartColor.FromHex("#123456"), series.DataLabelStyle.Color);
        Assert.Equal(21, chart.Options.TitleStyle.FontSize); Assert.Equal(ChartColor.FromHex("#234567"), chart.Options.TitleStyle.Color);
        Assert.Equal(new[] { "#FDE68A", "#BFDBFE", "#DDD6FE", "#A7F3D0" }, chart.PlotRegions.Select(region => region.Color!.Value.ToHex()));
        Assert.All(chart.PlotRegions, region => Assert.Equal(1, region.Opacity));
        Assert.Equal(ChartColor.FromHex("#654321"), chart.Options.XAxisLabelHighlights[.25]);
        Assert.Equal(ChartColor.FromHex("#654321"), chart.Options.XAxisLabelHighlights[.75]);
        Assert.Null(series.GetPointMarkers(0));
        Assert.Equal((.2, .8), (series.Points[0].X, series.Points[0].Y));
        Assert.Equal("Observed", series.PointLabels[0]);
        AssertAxis(chart.Options.XAxis, (.25, "Low reach"), (.75, "High reach"));
        AssertAxis(chart.Options.YAxis, (.25, "Low confidence"), (.75, "High confidence"));
        Assert.Equal(string.Empty, chart.XAxisTitle); Assert.Equal(string.Empty, chart.YAxisTitle);
    }

    [Fact]
    public void DirectFencedTypedAndGenericDocumentsShareNativeOutputAndDefaultFallbacks() {
        var typed = new MermaidParser().ParseQuadrant(Configured); var generic = new MermaidParser().Parse(Configured);
        Assert.Empty(typed.Diagnostics); Assert.Empty(generic.Diagnostics);
        var direct = MermaidRenderer.Render(Configured); Assert.Empty(direct.Diagnostics); var artifact = direct.Artifact!;
        var fenced = new MermaidVisualMarkupParser().Parse("```mermaid\n" + Configured + "\n```");
        Assert.Empty(fenced.Diagnostics); var fence = Assert.Single(fenced.Artifacts);
        Assert.Equal(artifact.ToSvg(), typed.Document!.ToVisualArtifact().ToSvg());
        Assert.Equal(artifact.ToSvg(), generic.Document!.ToVisualArtifact().ToSvg());
        Assert.Equal(artifact.ToSvg(), fence.ToSvg()); Assert.Equal(artifact.ToPng(), fence.ToPng());
        var xml = XDocument.Parse(artifact.ToSvg());
        Assert.Single(Role(xml, "mermaid-quadrant-point")); Assert.Equal(4, Role(xml, "plot-region-fill").Length);
        Assert.DoesNotContain(xml.Descendants(), node => node.Name.LocalName is "script" or "foreignObject");
        Assert.Equal(artifact.Metadata["mermaid.quadrant.sourceFacts"], fence.Metadata["mermaid.quadrant.sourceFacts"]);
        var plain = new MermaidParser().ParseQuadrant(Body).Document!.ToChart();
        Assert.Equal((960d, 560d), (plain.Options.Size.Width, plain.Options.Size.Height));
        Assert.All(plain.PlotRegions, region => { Assert.Null(region.Color); Assert.Equal(.14, region.Opacity); });
        Assert.Null(plain.Series[0].GetPointMarkers(0));
        Assert.False(new MermaidParser().ParseQuadrant(Body).Document!.ToVisualArtifact().Metadata.ContainsKey("mermaid.quadrant.sourceFacts"));
    }

    [Fact]
    public void ExplicitHostDimensionsOverrideOnlyTheAuthoredDimensionIncludingDefaultValuedOverrides() {
        var document = new MermaidParser().ParseQuadrant(Configured).Document!;
        var width = document.ToChart(new MermaidQuadrantRenderOptions { Width = 960 });
        Assert.Equal((960d, 480d), (width.Options.Size.Width, width.Options.Size.Height));
        var height = document.ToChart(new MermaidQuadrantRenderOptions { Height = 560 });
        Assert.Equal((720d, 560d), (height.Options.Size.Width, height.Options.Size.Height));
        var fence = new MermaidVisualMarkupParser().Parse("```mermaid {width=\"390\"}\n" + Configured + "\n```");
        Assert.Empty(fence.Diagnostics); var artifact = Assert.Single(fence.Artifacts);
        Assert.Equal((390d, 480d), (artifact.NaturalSize!.Value.Width, artifact.NaturalSize.Value.Height));
    }

    [Theory]
    [InlineData("x-axis Low", 0, "Low", null)]
    [InlineData("x-axis --> High", .75, "High", null)]
    [InlineData("x-axis Low -->", 0, "Low ⟶", null)]
    [InlineData("x-axis Low ---> High", .25, "Low", "High")]
    public void EndpointCaptionsKeepPinnedNativeCoordinatePositions(string axis, double position, string first, string? second) {
        var parsed = new MermaidParser().ParseQuadrant("quadrantChart\n" + axis + "\nObserved: [0.2, 0.8]");
        Assert.Empty(parsed.Diagnostics); var chart = parsed.Document!.ToChart();
        var expected = second == null ? new[] { (position, first) } : new[] { (position, first), (.75, second!) };
        AssertAxis(chart.Options.XAxis, expected);
        Assert.Equal(string.Empty, chart.XAxisTitle); Assert.Equal("Y", chart.YAxisTitle);
        Assert.Equal((.2, .8), (chart.Series[0].Points[0].X, chart.Series[0].Points[0].Y));
    }

    [Fact]
    public void QuotedLiteralArrowsColonsAndClassDelimitersRemainTextAndScientificCoordinatesRemainNumeric() {
        const string source = "quadrantChart\nx-axis \"Low --> literal\" ---> \"High: x\"\ny-axis \"Low ::: literal\"\n"
            + "  \"Call:::ops: west\":::focus: [1e-1, 8e-1] radius:9\nclassDef focus color:#123456";
        var parsed = new MermaidParser().ParseQuadrant(source); Assert.Empty(parsed.Diagnostics); var document = parsed.Document!;
        Assert.Equal("Low --> literal", document.XAxisStart); Assert.Equal("High: x", document.XAxisEnd);
        Assert.Equal("Low ::: literal", document.YAxisStart); Assert.Equal(string.Empty, document.YAxisEnd);
        var point = Assert.Single(document.Points);
        Assert.Equal("Call:::ops: west", point.Label); Assert.Equal("focus", point.ClassName);
        Assert.Equal((.1, .8), (point.X, point.Y)); Assert.Equal((4, 3), (point.Span.Line, point.Span.Column));
        AssertAxis(document.ToChart().Options.XAxis, (.25, "Low --> literal"), (.75, "High: x"));
        AssertAxis(document.ToChart().Options.YAxis, (0, "Low ::: literal"));
    }

    [Fact]
    public void SourceVisibilityBooleansUseNativeHeaderAndAxisOptions() {
        var parsed = new MermaidParser().ParseQuadrant("%%{init: {'quadrantChart': {'showTitle': false, 'showXAxis': false, 'showYAxis': true}}}%%\n" + Body);
        Assert.Empty(parsed.Diagnostics); var chart = parsed.Document!.ToChart(new MermaidQuadrantRenderOptions { Subtitle = "Host context" });
        Assert.True(chart.Options.ShowHeader); Assert.False(chart.Options.XAxis.Visible); Assert.True(chart.Options.YAxis.Visible);
        Assert.Equal(string.Empty, chart.Title); Assert.Equal("Priorities", parsed.Document!.Title);
        Assert.Equal("Host context", chart.Subtitle); Assert.Equal(2, chart.Options.XAxis.Labels.Count);
    }

    [Theory]
    [InlineData("opacity:0.5")]
    [InlineData("fill:#123456")]
    [InlineData("Radius:8")]
    public void UnsupportedPointStylesRemainInertLocatedAndSubjectToStrictWarningPolicy(string declaration) {
        var source = "quadrantChart\n  Observed: [0.2, 0.8] " + declaration;
        var parsed = new MermaidParser().ParseQuadrant(source); Assert.False(parsed.HasErrors);
        var diagnostic = Assert.Single(parsed.Diagnostics);
        Assert.Equal(MermaidDiagnosticCodes.UnsupportedStatement, diagnostic.Code); Assert.Equal((2, 3), (diagnostic.Span.Line, diagnostic.Span.Column));
        Assert.NotNull(MermaidRenderer.Render(source).Artifact);
        var strict = new MermaidRenderOptions { WarningPolicy = MermaidWarningPolicy.Reject };
        Assert.Null(MermaidRenderer.Render(source, strict).Artifact);
        Assert.Empty(new MermaidVisualMarkupParser(strict).Parse("```mermaid\n" + source + "\n```").Artifacts);
        Assert.Equal(declaration.Substring(declaration.IndexOf(':') + 1), parsed.Document!.Points[0].Styles[declaration.Substring(0, declaration.IndexOf(':'))]);
        Assert.Null(parsed.Document.ToChart().Series[0].GetPointMarkers(0)?.Radius);
    }

    [Theory]
    [InlineData("radius:1.5")]
    [InlineData("radius:-1")]
    [InlineData("radius:1025")]
    [InlineData("stroke-width:2")]
    [InlineData("stroke-width:2.5px")]
    [InlineData("color:#12")]
    [InlineData("color:rgba(1,2,3,0.5)")]
    public void InvalidPinnedStyleSyntaxAndBoundedNativeDimensionsProduceLocatedErrors(string declaration) {
        var result = MermaidRenderer.Render("quadrantChart\n  Observed: [0.2, 0.8] " + declaration);
        Assert.True(result.HasErrors); Assert.Null(result.Artifact);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == MermaidDiagnosticCodes.InvalidStatement
            && diagnostic.Severity == MermaidDiagnosticSeverity.Error && diagnostic.Span.Line == 2 && diagnostic.Span.Column == 3);
    }

    [Theory]
    [InlineData("chartWidth", "0")]
    [InlineData("chartHeight", "480.5")]
    [InlineData("pointRadius", "-1")]
    [InlineData("pointRadius", "1025")]
    [InlineData("pointLabelFontSize", "0")]
    [InlineData("titleFontSize", "257")]
    [InlineData("showXAxis", "'false'")]
    [InlineData("chartWidth", "{'nested': 720}")]
    public void InvalidAppliedSourceConfigurationProducesOneLocatedError(string key, string value) {
        var source = "%%{init: {'quadrantChart': {'" + key + "': " + value + "}}}%%\n" + Body;
        var parsed = new MermaidParser().ParseQuadrant(source); var diagnostic = Assert.Single(parsed.Diagnostics);
        Assert.Equal(MermaidDiagnosticCodes.InvalidConfiguration, diagnostic.Code); Assert.Equal(1, diagnostic.Span.Line);
        var direct = MermaidRenderer.Render(source); Assert.True(direct.HasErrors); Assert.Null(direct.Artifact);
    }

    [Theory]
    [InlineData("'quadrantChart': {'xAxisLabelFontSize': 22}")]
    [InlineData("'quadrantChart': {'pointTextPadding': 9}")]
    [InlineData("'themeVariables': {'quadrantYAxisTextFill': '#123456'}")]
    public void ConfigurationWithoutAFaithfulNativeOwnerRetainsItsWarningAndStrictRejection(string declaration) {
        var source = "%%{init: {" + declaration + "}}%%\n" + Body;
        var parsed = new MermaidParser().ParseQuadrant(source); Assert.False(parsed.HasErrors);
        var diagnostic = Assert.Single(parsed.Diagnostics);
        Assert.Equal(MermaidDiagnosticCodes.UnsupportedConfiguration, diagnostic.Code); Assert.Equal(1, diagnostic.Span.Line);
        Assert.Single(parsed.Document!.Configuration.Settings);
        Assert.NotNull(MermaidRenderer.Render(source).Artifact);
        Assert.Null(MermaidRenderer.Render(source, new MermaidRenderOptions { WarningPolicy = MermaidWarningPolicy.Reject }).Artifact);
    }

    [Fact]
    public void RawDeclarationsSpansAndNumberSpellingSurvivePortableExportAndLaterAstEdits() {
        const string source = "quadrantChart\n  classDef focus color:#ABCDEF,host-note:keep,__proto__:literal\n"
            + "  \"Call:::ops: west\":::focus: [0.20, 0.80] radius:9,host-note:point\n";
        var parsed = new MermaidParser().ParseQuadrant(source); Assert.False(parsed.HasErrors);
        var document = parsed.Document!; var artifact = document.ToVisualArtifact();
        var raw = artifact.Metadata["mermaid.quadrant.sourceFacts"];
        using var facts = JsonDocument.Parse(raw); var root = facts.RootElement;
        var definition = root.GetProperty("classes")[0]; var point = root.GetProperty("points")[0];
        Assert.Equal("focus", definition.GetProperty("name").GetString());
        Assert.Equal(new[] { "color", "host-note", "__proto__" }, definition.GetProperty("styles").EnumerateArray().Select(item => item.GetProperty("name").GetString()));
        Assert.Equal("literal", definition.GetProperty("styles")[2].GetProperty("value").GetString());
        Assert.Equal("Call:::ops: west", point.GetProperty("label").GetString()); Assert.Equal("focus", point.GetProperty("className").GetString());
        Assert.Equal((.2, .8), (point.GetProperty("x").GetDouble(), point.GetProperty("y").GetDouble()));
        Assert.Equal((3, 3), (point.GetProperty("span").GetProperty("line").GetInt32(), point.GetProperty("span").GetProperty("column").GetInt32()));
        Assert.Contains("[0.20, 0.80]", root.GetProperty("statements")[1].GetProperty("text").GetString()!);
        var envelope = VisualArtifactInterchangeEnvelope.FromJson(artifact.ToInterchangeJson());
        Assert.Equal(raw, envelope.Extensions["mermaid.quadrant.sourceFacts"]);
        document.ClassDefinitions[0].Styles["color"] = "#000000";
        document.Points[0].Label = "Changed"; document.Points[0].Styles["radius"] = "30";
        Assert.Equal(raw, artifact.Metadata["mermaid.quadrant.sourceFacts"]);
        Assert.Equal("Call:::ops: west", Assert.IsType<Chart>(artifact.Model).Series[0].PointLabels[0]);
        Assert.Equal(9, Assert.IsType<Chart>(artifact.Model).Series[0].GetPointMarkers(0)!.Radius);
    }

    [Theory]
    [InlineData(double.NaN, .5)]
    [InlineData(double.PositiveInfinity, .5)]
    [InlineData(-.1, .5)]
    [InlineData(.5, 1.1)]
    public void DirectlyAuthoredTypedPointsCannotBypassNormalizedFiniteCoordinateValidation(double x, double y) {
        var document = new MermaidParser().ParseQuadrant("quadrantChart\nObserved: [0.2, 0.8]").Document!;
        document.Points[0].X = x; document.Points[0].Y = y;
        Assert.Throws<ArgumentException>(() => document.ToChart());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WholeFamilySharedSourceKeepsEveryNativeObservationAndExportsDetachedFacts(bool dark) {
        var rendered = MermaidRenderer.Render(QuadrantSourceDepthFixture.Read(dark), new MermaidRenderOptions { WarningPolicy = MermaidWarningPolicy.Reject });
        Assert.Empty(rendered.Diagnostics); var artifact = rendered.Artifact!; var chart = Assert.IsType<Chart>(artifact.Model);
        Assert.Equal((800d, 500d), (chart.Options.Size.Width, chart.Options.Size.Height));
        var series = Assert.Single(chart.Series); Assert.Equal(4, series.Points.Count);
        Assert.Equal(new[] { "Opportunity", "Investment", "Experiment", "Review" }, series.PointLabels);
        Assert.Equal(new double?[] { 9, 7, 6, null }, Enumerable.Range(0, 4).Select(series.GetPointMarkers).Select(marker => marker?.Radius));
        var svg = artifact.ToSvg(); var png = artifact.ToPng();
        Assert.Equal(4, Role(XDocument.Parse(svg), "mermaid-quadrant-point").Length);
        Assert.Equal(artifact.Metadata["mermaid.quadrant.sourceFacts"], VisualArtifactInterchangeEnvelope.FromJson(artifact.ToInterchangeJson()).Extensions["mermaid.quadrant.sourceFacts"]);
        Assert.NotEmpty(png);
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) {
            Directory.CreateDirectory(output); var stem = Path.Combine(output, "quadrant-source-native-" + dark);
            File.WriteAllText(stem + ".svg", svg); File.WriteAllBytes(stem + ".png", png); File.WriteAllText(stem + ".json", artifact.ToInterchangeJson());
        }
    }

    [Fact]
    public void RepeatedPointClassesConsumeTheCapturedSharedExpansionBudget() {
        const string source = "quadrantChart\nclassDef focus color:#123456,radius:4\nA:::focus: [0.2, 0.2]\nB:::focus: [0.8, 0.8]";
        var options = new MermaidRenderOptions { ParseOptions = new MermaidParseOptions {
            ResourceLimits = new MermaidResourceLimits { MaximumElements = 8 }
        } };
        var parsed = new MermaidParser().Parse(source, options.ParseOptions);
        Assert.False(parsed.HasErrors);
        var rendered = MermaidRenderer.RenderParsed(parsed, options);
        Assert.Null(rendered.Artifact);
        Assert.Contains(rendered.Diagnostics, diagnostic => diagnostic.Code == MermaidDiagnosticCodes.ResourceLimitExceeded
            && diagnostic.Span.Line == 3);
    }

    [Fact]
    public void TypedSourceFactGrowthFailsTheSharedPortableBudgetWithoutTruncatingTheDocument() {
        var document = new MermaidParser().ParseQuadrant("quadrantChart\nA: [0.2, 0.8] radius:4").Document!;
        var large = new string('x', 65537);
        document.Statements.Add(new MermaidRawStatement(large, document.HeaderSpan));
        var exception = Assert.Throws<MermaidResourceLimitException>(() => document.ToVisualArtifact());
        Assert.Equal("Interchange.MaximumTextCharacters", exception.LimitName);
        Assert.Equal(large, document.Statements.Last().Text);
    }

    private static void AssertAxis(ChartAxis axis, params (double Value, string Text)[] expected) =>
        Assert.Equal(expected, axis.Labels.Select(label => (label.Value, label.Text)).ToArray());
    private static XElement[] Role(XDocument xml, string role) => xml.Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == role).ToArray();
}
