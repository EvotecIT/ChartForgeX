using System;
using System.Linq;
using System.Text.Json;
using ChartForgeX.Effects;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class SequenceRetainedSurfaceSourceTests {
    private const string Body = "sequenceDiagram\nparticipant A as Review\nparticipant B as Archive\nA->>B: Save";
    private const string Annotations = "\nNote over A,B: Retain facts\nloop Audit\nA->>B: Stored\nend";

    [Theory]
    [InlineData("redux-color", "#000000")]
    [InlineData("redux-dark-color", "#FFFFFF")]
    [InlineData("base", "#FFFFFF")]
    public void PinnedNeoGlyphShadowKeepsExactFractionRegionAndLiteralPresetTintEvenWhenStylesAreDisabled(string preset, string color) {
        var result = MermaidRenderer.Render(Directive(new { theme = preset, themeVariables = new { dropShadow = "none" } }) + Body,
            new MermaidRenderOptions { WarningPolicy = MermaidWarningPolicy.Reject });
        SequencePresetFixture.Capture("surface-source-glyph-" + preset, result);
        Assert.Empty(result.Diagnostics);
        var model = Assert.IsType<SequenceArtifact>(result.Artifact!.Model);
        foreach (var participant in model.Participants) {
            var shadow = Assert.IsType<VisualDropShadow>(participant.SurfaceShadow);
            Assert.Equal(color, shadow.Color.ToCss());
            Assert.Equal(4, shadow.OffsetX);
            Assert.Equal(4, shadow.OffsetY);
            Assert.Equal(0, shadow.SigmaX);
            Assert.Equal(.06, shadow.Opacity);
            Assert.Equal(new ChartRect(-.1, -.1, 1.3, 1.3), shadow.RelativeRegion);
        }
        Assert.Null(model.Paint.NoteShadow);
        Assert.Null(model.Paint.FragmentTabShadow);
    }

    [Theory]
    [InlineData("base", 1d)] [InlineData("forest", .5)]
    [InlineData("neo-dark", .2)] [InlineData("neo", .25)]
    public void NamedFunctionRolesUseOneNativeDescriptorAndExplicitHostThemeOnlySuppressesPresetDefaults(string name, double opacity) {
        var source = Directive(new { theme = name }) + Body + Annotations;
        var result = MermaidRenderer.Render(source);
        SequencePresetFixture.Capture("surface-source-function-" + name, result);
        Assert.Empty(result.Diagnostics);
        var model = Assert.IsType<SequenceArtifact>(result.Artifact!.Model);
        Assert.Equal(opacity, model.Paint.NoteShadow!.Opacity);
        Assert.Same(model.Paint.NoteShadow, model.Paint.FragmentTabShadow);
        Assert.Same(model.Paint.NoteShadow, model.Paint.BackgroundShadow);
        var hosted = MermaidRenderer.Render(source, new MermaidRenderOptions {
            Presentation = new MermaidPresentationOptions { Theme = VisualTheme.Graphite() }
        });
        var hostModel = Assert.IsType<SequenceArtifact>(hosted.Artifact!.Model);
        Assert.Null(hostModel.Paint.NoteShadow);
        Assert.NotNull(hostModel.Participants[0].SurfaceShadow);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void ExplicitLiteralFunctionUsesPreciseAlphaAndSurvivesFenceHostThemeAndPortableRestoration(bool fence) {
        var settings = Directive(new { theme = "redux-color", themeVariables = new {
            dropShadow = "drop-shadow(-2px 3px 1.5px rgba(10, 20, 30, 0.25))" } });
        var source = settings + Body + Annotations;
        var options = new MermaidRenderOptions {
            WarningPolicy = MermaidWarningPolicy.Reject,
            Presentation = new MermaidPresentationOptions { Theme = VisualTheme.Graphite(), FontFamily = "Arial" }
        };
        var direct = MermaidRenderer.Render(source, options);
        var artifact = fence
            ? Assert.Single(new MermaidVisualMarkupParser(options).Parse("```mermaid\n" + source + "\n```").Artifacts)
            : direct.Artifact!;
        SequencePresetFixture.Capture("surface-source-literal-" + fence, direct);
        Assert.Empty(direct.Diagnostics);
        var model = Assert.IsType<SequenceArtifact>(artifact.Model);
        var shadow = model.Paint.NoteShadow!;
        Assert.Equal(ChartColor.FromRgb(10, 20, 30), shadow.Color);
        Assert.Equal(.25, shadow.Opacity);
        Assert.Equal(-2, shadow.OffsetX);
        Assert.Equal(3, shadow.OffsetY);
        Assert.Equal(1.5, shadow.SigmaX);
        var portable = artifact.ToInterchangeEnvelope();
        var restored = VisualArtifactInterchangeEnvelope.FromJson(portable.ToJson()).ToSequenceArtifact();
        Assert.Equal(shadow.Color, restored.Paint.NoteShadow!.Color);
        Assert.Equal(shadow.Opacity, restored.Paint.NoteShadow.Opacity);
        Assert.Equal(shadow.SigmaX, restored.Paint.NoteShadow.SigmaX);
        Assert.Equal(new[] { "A", "B" }, restored.Participants.Select(participant => participant.Id));
    }

    [Fact]
    public void PlainActorHasNoInsertedGlyphFilterAndKnownPresetStyleUrlPreservesStandaloneNoPaint() {
        var actor = MermaidRenderer.Render("sequenceDiagram\nactor A\nA->>A: Save");
        SequencePresetFixture.Capture("surface-source-stick-actor", actor);
        Assert.Empty(actor.Diagnostics);
        Assert.Null(Assert.IsType<SequenceArtifact>(actor.Artifact!.Model).Participants[0].SurfaceShadow);
        var ordinary = MermaidRenderer.Render(Body + Annotations,
            new MermaidRenderOptions { WarningPolicy = MermaidWarningPolicy.Reject });
        SequencePresetFixture.Capture("surface-source-known-preset-url-no-paint", ordinary);
        Assert.Empty(ordinary.Diagnostics);
        var model = Assert.IsType<SequenceArtifact>(ordinary.Artifact!.Model);
        Assert.NotNull(model.Participants[0].SurfaceShadow);
        Assert.Null(model.Paint.NoteShadow);
        Assert.Null(model.Paint.FragmentTabShadow);
        Assert.Null(model.Paint.BackgroundShadow);
    }

    [Theory]
    [InlineData(VisualThemeMode.Light)] [InlineData(VisualThemeMode.Dark)]
    public void ExplicitNoneChangesNoStandaloneStylePixelsAndCannotDisableInsertedGlyphShadow(VisualThemeMode mode) {
        var options = new MermaidRenderOptions {
            WarningPolicy = MermaidWarningPolicy.Reject,
            Presentation = new MermaidPresentationOptions { ThemeMode = mode }
        };
        var ordinary = MermaidRenderer.Render(Body + Annotations, options);
        var disabled = MermaidRenderer.Render(Directive(new { themeVariables = new { dropShadow = "none" } }) + Body + Annotations, options);
        SequencePresetFixture.Capture("surface-source-default-actual164-" + mode, ordinary);
        SequencePresetFixture.Capture("surface-source-none-post-" + mode, disabled);
        Assert.Empty(ordinary.Diagnostics);
        Assert.Empty(disabled.Diagnostics);
        var first = Assert.IsType<SequenceArtifact>(ordinary.Artifact!.Model);
        var second = Assert.IsType<SequenceArtifact>(disabled.Artifact!.Model);
        Assert.All(second.Participants, participant => Assert.Equal(.06, participant.SurfaceShadow!.Opacity));
        Assert.Null(first.Paint.NoteShadow);
        Assert.Null(second.Paint.NoteShadow);
        Assert.Equal(ordinary.Artifact!.ToPng(), disabled.Artifact!.ToPng());
    }

    [Theory]
    [InlineData("drop-shadow(.5px 2px 1px #000)")]
    [InlineData("drop-shadow(1px 2px 129px #000)")]
    [InlineData("url(#guessed-shadow)")]
    public void UnsupportedFunctionOrTargetStaysLocatedAndDoesNotReplayAnUnrelatedPresetEffect(string value) {
        var source = Directive(new { theme = "base", themeVariables = new { dropShadow = value } }) + Body + Annotations;
        var result = MermaidRenderer.Render(source);
        SequencePresetFixture.Capture("surface-source-unsupported-" + value.Length, result);
        Assert.Contains(result.Diagnostics, item => item.Span.Line == 1 && item.Code == MermaidDiagnosticCodes.UnsupportedConfiguration);
        Assert.Null(Assert.IsType<SequenceArtifact>(result.Artifact!.Model).Paint.NoteShadow);
        Assert.Null(MermaidRenderer.Render(source,
            new MermaidRenderOptions { WarningPolicy = MermaidWarningPolicy.Reject }).Artifact);
    }

    // The pinned init parser consumes JSON after the directive key. Use canonical
    // quoted JSON, not the CFX parser's broader historical object-literal admission.
    private static string Directive(object settings) =>
        "%%{init: " + JsonSerializer.Serialize(settings) + "}%%\n";

}
