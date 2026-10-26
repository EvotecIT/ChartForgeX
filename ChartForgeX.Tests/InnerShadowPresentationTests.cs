using System;
using System.Linq;
using System.Xml.Linq;
using ChartForgeX.Effects;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.SvgRaster;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class InnerShadowPresentationTests {
    [Theory]
    [InlineData("compact", VisualThemeMode.Light)]
    [InlineData("compact", VisualThemeMode.Dark)]
    [InlineData("wide", VisualThemeMode.Light)]
    [InlineData("wide", VisualThemeMode.Dark)]
    public void CoreRecipeNativeProfilesAndOrderedPortableReplayKeepOneSource(string variant, VisualThemeMode mode) {
        var source = NativeAppearanceExample.PrepareSource(variant, mode);
        var request = NativeAppearanceExample.InnerShadowPolicy(mode);
        var layer = NativeAppearanceExample.Create("inner", variant, mode);
        var direct = source.CaptureLayer(request, InnerShadowFixture.Unit);
        var retained = source.WithInnerShadow(request);
        var json = retained.ToArtifact().ToInterchangeJson();
        var envelope = VisualArtifactInterchangeEnvelope.FromJson(json);
        var fresh = NativeAppearanceExample.PrepareSource(variant, mode);
        var restored = envelope.ApplyCapturedEffects(fresh);
        var nativeSvg = retained.ToSvg(new VisualSvgOptions(effectProfile: VisualSvgEffectProfile.NativeFilters));
        var terminalJson = layer.ToArtifact().ToInterchangeJson();
        var terminal = VisualArtifactInterchangeEnvelope.FromJson(terminalJson).ApplyCapturedLayer(fresh);
        AppearanceFixture.Capture("inner-recipe-" + variant + "-" + mode, layer);
        AppearanceFixture.Capture("inner-recipe-restored-" + variant + "-" + mode, terminal);
        NativeEffectBeforeCapture.Write("inner-retained-" + variant + "-" + mode, nativeSvg,
            restored.ToPng(InnerShadowFixture.Unit), restored.ToRgba(InnerShadowFixture.Unit));
        NativeEffectBeforeCapture.Json("inner-recipe-" + variant + "-" + mode + "-facts", new { json, terminalJson });
        Assert.Equal(direct.ToImage().Pixels, layer.ToImage().Pixels);
        Assert.Equal(layer.ToImage().Pixels, terminal.ToImage().Pixels);
        Assert.Equal(retained.ToSvg(), restored.ToSvg());
        Assert.Equal(retained.ToPng(InnerShadowFixture.Unit), restored.ToPng(InnerShadowFixture.Unit));
        Assert.Equal(new ChartRect(0, 0, source.Size.Width, source.Size.Height), layer.Bounds);
        var presentation = Assert.IsType<VisualArtifactInterchangePresentation>(envelope.Presentation);
        var step = Assert.Single(presentation.CapturedEffects);
        Assert.NotNull(step.InnerShadow);
        Assert.Null(step.DropShadow);
        Assert.Null(step.CaptureFrame);
        var document = XDocument.Parse(nativeSvg);
        Assert.Single(document.Descendants(), item => item.Name.LocalName == "filter");
        Assert.DoesNotContain(document.Descendants(), item => item.Name.LocalName == "image");
        Assert.Equal(XDocument.Parse(source.ToSvg()).Descendants().Count(item => item.Name.LocalName == "text"),
            document.Descendants().Count(item => item.Name.LocalName == "text"));
        Assert.Equal("atop", document.Descendants().Last(item => item.Name.LocalName == "feComposite").Attribute("operator")?.Value);
        presentation.CapturedEffects.Clear();
        Assert.Equal(json, retained.ToArtifact().ToInterchangeJson());
        var program = ExampleRecipeSource.EmitNativeAppearance("inner", variant, mode);
        Assert.Contains("VisualInnerShadow", program);
        Assert.Contains("ApplyCapturedLayer", program);
        Assert.DoesNotContain("{{", program);
    }

    [Fact]
    public void KnownEditableFamilyCannotSilentlyDropInnerAppearanceOnModelImport() {
        var model = SequenceArtifact.Create("inner-editable").WithSize(480, 480)
            .AddParticipant("review", "Review").AddParticipant("archive", "Archive")
            .AddMessage("review", "archive", "Save");
        var prepared = model.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(480, 480), 20)));
        var plain = VisualArtifactInterchangeEnvelope.FromJson(prepared.ToArtifact().ToInterchangeJson());
        var styled = prepared.WithInnerShadow(new VisualInnerShadow(ChartColor.Black, 1, 1, 1));
        var saved = styled.ToArtifact().ToInterchangeJson();
        NativeEffectBeforeCapture.Json("inner-model-import-loss-guard", new { saved });
        Assert.NotNull(plain.ToSequenceArtifact());
        var envelope = VisualArtifactInterchangeEnvelope.FromJson(saved);
        Assert.Throws<NotSupportedException>(() => envelope.ToSequenceArtifact());
    }

    [Fact]
    public void MaskAndInsetOrderIsRealAndMultiplePayloadsCannotSilentlyDropOneEffect() {
        var source = InnerShadowFixture.Source(128);
        var request = new VisualInnerShadow(ChartColor.FromRgb(0, 0, 255), 0, 0, 0, orderedDither: false);
        var mask = new VisualMask(AppearanceFixture.Solid(16, 16, 128), orderedDither: false);
        var first = source.WithInnerShadow(request).WithMask(mask).ToRgba(InnerShadowFixture.Unit);
        var second = source.WithMask(mask).WithInnerShadow(request).ToRgba(InnerShadowFixture.Unit);
        NativeEffectBeforeCapture.Write("inner-mask-after", null, first.ToPng(), first);
        NativeEffectBeforeCapture.Write("inner-mask-before", null, second.ToPng(), second);
        Assert.NotEqual(first.Pixels, second.Pixels);
        Assert.Equal(64, AppearanceFixture.Pixel(first, 8, 8)[3]);
        Assert.Equal(64, AppearanceFixture.Pixel(second, 8, 8)[3]);
        var envelope = VisualArtifactInterchangeEnvelope.FromJson(NativeAppearanceExample.PrepareSource()
            .WithInnerShadow(request).ToArtifact().ToInterchangeJson());
        var presentation = Assert.IsType<VisualArtifactInterchangePresentation>(envelope.Presentation);
        Assert.Single(presentation.CapturedEffects).DropShadow = new VisualDropShadow(ChartColor.Black, 1, 1, 1);
        Assert.Throws<ArgumentException>(() => envelope.ToJson());
    }

    [Fact]
    public void NewPortablePayloadRejectsUnknownFieldsAndInvalidScalarsThroughItsCanonicalReader() {
        var source = NativeAppearanceExample.PrepareSource();
        var saved = source.WithInnerShadow(new VisualInnerShadow(ChartColor.Black, 1, 1, 2))
            .ToArtifact().ToInterchangeJson();
        NativeEffectBeforeCapture.Json("inner-portable-scalar-boundary", new { saved });
        Assert.Contains("\"innerShadow\":{", saved);
        Assert.Throws<ArgumentException>(() => VisualArtifactInterchangeEnvelope.FromJson(
            saved.Replace("\"innerShadow\":{", "\"innerShadow\":{\"relativeRegion\":{},")));
        Assert.Throws<ArgumentOutOfRangeException>(() => VisualArtifactInterchangeEnvelope.FromJson(
            saved.Replace("\"sigmaX\":2", "\"sigmaX\":-1")));
    }

}
