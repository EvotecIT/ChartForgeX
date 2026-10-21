using ChartForgeX.Core;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidHostPolicyTests {
    [Theory]
    [InlineData("flowchart LR\nA[Source] --> B[Target]")]
    [InlineData("mindmap\n Root\n   Child")]
    public void SharedHostPresentationOverridesSourceAndFamilyOverridesOnlyAuthoredFields(string diagram) {
        var source = "---\nconfig:\n  theme: dark\n  fontFamily: Source Font\n  themeVariables:\n    fontSize: 15\n---\n" + diagram;
        var options = new MermaidRenderOptions {
            Presentation = new() { ThemeMode = VisualThemeMode.Light, FontFamily = "Host Font", FontSize = 19,
                TopologySizing = MermaidTopologySizing.FixedViewport },
            Flowchart = new() { Presentation = new() { FontSize = 23 } },
            MindMap = new() { Presentation = new() { FontSize = 23 } }
        };
        var result = MermaidRenderer.Render(source, options);
        Assert.False(result.HasErrors);
        var model = Assert.IsType<TopologyChart>(result.Artifact!.Model);
        Assert.Equal(VisualThemeMode.Light, model.ThemeMode);
        Assert.Equal("Host Font", model.Theme.Typography.Family);
        Assert.Equal(23, model.Theme.Typography.DataLabelSize);
        Assert.True(model.DefaultRenderOptions!.FitContentToViewport);
        Assert.Equal("Source Font", result.Document!.Configuration.FontFamily);
        Assert.Equal(15, result.Document.Configuration.FontSize);
        var svg = result.Artifact.ToSvg();
        var png = result.Artifact.ToPng();
        Assert.Contains("Host Font", svg);
        Assert.True(png.Length > 64);
        var fenced = new MermaidVisualMarkupParser(options).Parse("```mermaid\n" + source + "\n```");
        Assert.False(fenced.HasErrors);
        var artifact = Assert.Single(fenced.Artifacts);
        Assert.Equal(svg, artifact.ToSvg());
        Assert.Equal(png, artifact.ToPng());
        Assert.Equal(19, options.Presentation.FontSize);
    }

    [Theory]
    [InlineData("pie\n\"One\": 1\n\"Two\": 2")]
    [InlineData("sequenceDiagram\nA->>B: One")]
    [InlineData("classDiagram\nA --> B")]
    [InlineData("gantt\ndateFormat YYYY-MM-DD\nTask : 2026-01-01, 2d")]
    public void SharedTypographyAndModeReachDifferentNativeOwners(string source) {
        var result = MermaidRenderer.Render(source, new MermaidRenderOptions {
            Presentation = new() { ThemeMode = VisualThemeMode.Dark, FontFamily = "CFXProofCarlito", FontSize = 18 }
        });
        Assert.False(result.HasErrors);
        var svg = result.Artifact!.ToSvg();
        Assert.Contains("CFXProofCarlito", svg);
        Assert.True(result.Artifact!.ToPng().Length > 64);
    }

    [Fact]
    public void NaturalCompoundOutputKeepsMeasuredCanvasAndScrollableHost() {
        var options = new MermaidRenderOptions {
            Presentation = new() { TopologySizing = MermaidTopologySizing.Natural },
            Flowchart = new() { Width = 320, Height = 240 }
        };
        var output = MermaidRenderer.Render(MermaidFlowchartCompoundTests.Source, options);
        Assert.False(output.HasErrors);
        var artifact = output.Artifact!;
        Assert.False(artifact.PreserveNaturalSize);
        Assert.Equal(VisualArtifactHtmlSizing.PreserveSize, artifact.HtmlSizing);
        var html = artifact.ToHtmlPage();
        Assert.Contains("data-cfx-html-sizing=\"preserve-size\"", html);
        Assert.DoesNotContain("data-cfx-html-sizing=\"preserve-size\"", artifact.ToHtmlPage(new() { HtmlSizing = VisualArtifactHtmlSizing.FitToWidth }));
        var clone = artifact.Clone();
        Assert.Equal(artifact.HtmlSizing, clone.HtmlSizing);
        Assert.True(artifact.ToPng().Length > 64);
    }

    [Fact]
    public void RejectWarningsIsIdenticalForSourceAndFencedHosts() {
        const string source = "requirementDiagram\nrequirement r {\nid: 1\ntext: Work\nrisk: high\nverifymethod: test\n}\nstyle r fill:#ff0000";
        var options = new MermaidRenderOptions { WarningPolicy = MermaidWarningPolicy.Reject };
        var direct = MermaidRenderer.Render(source, options);
        Assert.True(direct.HasErrors);
        Assert.Null(direct.Artifact);
        Assert.Contains(direct.Diagnostics, item => item.Code == MermaidDiagnosticCodes.UnsupportedStatement);
        Assert.Contains(direct.Diagnostics, item => item.Code == MermaidDiagnosticCodes.RejectedWarning);
        var fenced = new MermaidVisualMarkupParser(options).Parse("```mermaid\n" + source + "\n```");
        Assert.True(fenced.HasErrors);
        Assert.Empty(fenced.Artifacts);
        Assert.Contains(fenced.Diagnostics, item => item.Code == MermaidDiagnosticCodes.RejectedWarning);
    }

    [Fact]
    public void ExportLimitsAreCapturedAndRejectSvgAndPixelsBeforeRasterAllocation() {
        var limits = new MermaidResourceLimits { MaximumOutputBytes = 128, MaximumRasterPixels = 100 };
        var options = new MermaidRenderOptions { ParseOptions = new() { ResourceLimits = limits } };
        var smallSvg = MermaidRenderer.Render("flowchart LR\nA-->B", options);
        Assert.False(smallSvg.HasErrors);
        limits.MaximumOutputBytes = 1000000;
        Assert.Equal(nameof(limits.MaximumOutputBytes), Assert.Throws<MermaidResourceLimitException>(() => smallSvg.Artifact!.ToSvg()).LimitName);
        var pixels = MermaidRenderer.Render("sequenceDiagram\nA->>B: Work", options);
        Assert.False(pixels.HasErrors);
        Assert.Equal(nameof(limits.MaximumRasterPixels), Assert.Throws<MermaidResourceLimitException>(() => pixels.Artifact!.ToPng()).LimitName);
    }

    [Fact]
    public void MarkupParserCapturesSharedAndFamilyPresentation() {
        var options = new MermaidRenderOptions { Presentation = new() { FontFamily = "Original Font" },
            Flowchart = new() { Presentation = new() { ThemeMode = VisualThemeMode.Dark } } };
        var parser = new MermaidVisualMarkupParser(options);
        options.Presentation.FontFamily = "Changed Font";
        options.Flowchart.Presentation.ThemeMode = VisualThemeMode.Light;
        var result = parser.Parse("```mermaid\nflowchart LR\nA-->B\n```");
        Assert.False(result.HasErrors);
        var model = Assert.IsType<TopologyChart>(Assert.Single(result.Artifacts).Model);
        Assert.Equal("Original Font", model.Theme.Typography.Family);
        Assert.Equal(VisualThemeMode.Dark, model.ThemeMode);
    }

    [Fact]
    public void CancellationIsNotReportedAsMalformedSourceAndIsRetainedForDeferredExport() {
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        Assert.Throws<OperationCanceledException>(() => MermaidRenderer.Render("flowchart LR\nA-->B", null, canceled.Token));
        using var later = new CancellationTokenSource();
        var result = MermaidRenderer.Render("flowchart LR\nA-->B", null, later.Token);
        later.Cancel();
        Assert.Throws<OperationCanceledException>(() => result.Artifact!.ToSvg());
    }
    [Fact]
    public void GuardedArtifactsPreserveEnvelopeAndNativeExportOverrides() {
        var result = MermaidRenderer.Render("flowchart LR\nA[First]-->B[Second]");
        var artifact = result.Artifact!;
        artifact.Accessibility.Name = "Host accessible name";
        var svg = artifact.ToSvg(new() { Topology = new() { IncludeNodeLabels = false } });
        Assert.Contains("Host accessible name", svg);
        Assert.DoesNotContain(">First</text>", svg);
        var basePixels = artifact.ToRgbaImage();
        var twice = artifact.ToRgbaImage(new() { Topology = new() { PngOutputScale = 2 } });
        Assert.Equal(basePixels.Width * 2, twice.Width);
        Assert.Equal(basePixels.Height * 2, twice.Height);
        var clone = artifact.Clone();
        clone.Accessibility.Name = "Cloned host name";
        Assert.Contains("Cloned host name", clone.ToSvg());
        Assert.DoesNotContain("Cloned host name", artifact.ToSvg());
    }

    [Fact]
    public void FinalPngAndHtmlBytesAreBoundedAlongsideSvg() {
        const string source = "flowchart LR\nA[First]-->B[Second]";
        var reference = MermaidRenderer.Render(source).Artifact!;
        var pngBytes = reference.ToPng().LongLength;
        var htmlBytes = System.Text.Encoding.UTF8.GetByteCount(reference.ToHtmlPage());
        var pngBounded = MermaidRenderer.Render(source, new() { ParseOptions = new() {
            ResourceLimits = new() { MaximumOutputBytes = pngBytes - 1 }
        } }).Artifact!;
        Assert.Equal(nameof(MermaidResourceLimits.MaximumOutputBytes), Assert.Throws<MermaidResourceLimitException>(() => pngBounded.ToPng()).LimitName);
        var exactPng = MermaidRenderer.Render(source, new() { ParseOptions = new() {
            ResourceLimits = new() { MaximumOutputBytes = pngBytes }
        } }).Artifact!;
        Assert.Equal(pngBytes, exactPng.ToPng().LongLength);
        var htmlBounded = MermaidRenderer.Render(source, new() { ParseOptions = new() {
            ResourceLimits = new() { MaximumOutputBytes = htmlBytes - 1 }
        } }).Artifact!;
        Assert.Equal(nameof(MermaidResourceLimits.MaximumOutputBytes), Assert.Throws<MermaidResourceLimitException>(() => htmlBounded.ToHtmlPage()).LimitName);
    }
    [Theory]
    [InlineData("flowchart LR\nA-->B")]
    [InlineData("sequenceDiagram\nA->>B: Message")]
    [InlineData("pie\n\"One\": 1\n\"Two\": 2")]
    [InlineData("packet-beta\n0-7: \"Header\"")]
    public void MeasuredDestinationMatchesActualNativePixels(string source) {
        var artifact = MermaidRenderer.Render(source, new() {
            Flowchart = new() { Width = 641.125, Height = 420.375 },
            Sequence = new() { Width = 641.125, Height = 420.375 }
        }).Artifact!;
        var options = new VisualArtifactRenderOptions { Topology = new() { PngOutputScale = 2 } };
        var measured = artifact.MeasureRasterSize(options);
        var pixels = artifact.ToRgbaImage(options);
        Assert.Equal(pixels.Width, measured.Width);
        Assert.Equal(pixels.Height, measured.Height);
    }

    [Fact]
    public void HugeAuthoredRasterIsRejectedByTheSourcePolicyBeforeNativeAllocation() {
        var result = MermaidRenderer.Render("pie\n\"One\": 1", new() {
            Pie = new() { Width = 12000, Height = 12000 }
        });
        Assert.False(result.HasErrors);
        var rejected = Assert.Throws<MermaidResourceLimitException>(() => result.Artifact!.ToPng());
        Assert.Equal(nameof(MermaidResourceLimits.MaximumRasterPixels), rejected.LimitName);
        Assert.Equal(144000000, rejected.Actual);
    }
}
