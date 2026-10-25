using System;
using ChartForgeX.Effects;
using ChartForgeX.Rendering;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class NativeEffectWireBudgetPostTests {
    [Fact]
    public void RepresentedLargeMaskKeepsNativeFramesAndImplicitHandoffButExplicitWireRejects() {
        var native = NativeEffectWireFixture.Source().WithMask(NativeEffectWireFixture.Mask(1024,1537));
        var artifact = native.ToArtifact();
        var frames = VisualValueCapture.InitialFadeFrames(native,3);
        NativeEffectWireFixture.Save("native-wire-large-root",artifact.ToSvg(),artifact.ToPng());
        NativeEffectWireFixture.Save("native-wire-large-middle",frames[1].ToSvg(),frames[1].ToPng());
        Assert.Same(native,frames[2]);Assert.Equal(3,frames.Count);
        Assert.True(artifact.ExportFormats.HasFlag(VisualArtifactExportFormat.Json));
        Assert.Throws<ArgumentOutOfRangeException>(()=>artifact.ToInterchangeJson());
        Assert.Equal(native.ToPng(),artifact.ToPng());
    }

    [Fact]
    public void CompletedLayerNativeArtifactDoesNotEagerlyEncodeTerminalPostMask() {
        var source = NativeEffectWireFixture.Source();
        var layer = source.CaptureLayer(new VisualLayerBlur(0),new VisualRenderOptions(supersampling:1))
            .WithMask(NativeEffectWireFixture.Mask(1024,1537));
        var artifact = layer.ToArtifact();
        var svg = artifact.ToSvg();var png = artifact.ToPng();
        NativeEffectWireFixture.Save("native-wire-large-terminal",svg,png);
        Assert.Contains("<image",svg);Assert.Equal(layer.Visual.ToPng(),png);
        Assert.True(artifact.ExportFormats.HasFlag(VisualArtifactExportFormat.Json));
        Assert.Throws<ArgumentOutOfRangeException>(()=>artifact.ToInterchangeJson());
        Assert.Equal(layer.Bounds.Width,artifact.NaturalSize!.Value.Width);
        Assert.Equal(layer.Bounds.Height,artifact.NaturalSize.Value.Height);
    }

    [Fact]
    public void CompleteEnvelopeBudgetIncludesSourceFactsAndMasksBeforeAnyPortableSnapshot() {
        var source = NativeEffectWireFixture.Source(largeFacts:true);
        // Each part fits separately:45 retained65536-character facts and one4MiB mask.
        var sourceJson = source.ToArtifact().ToInterchangeJson();
        var mask = NativeEffectWireFixture.Mask(1024,1024);
        var smallFacts = NativeEffectWireFixture.Source().WithMask(mask).ToArtifact().ToInterchangeJson();
        var native = source.WithMask(mask);
        var artifact = native.ToArtifact();
        NativeEffectWireFixture.Save("native-wire-complete-envelope",artifact.ToSvg(),artifact.ToPng());
        Assert.True(sourceJson.Length<VisualArtifactInterchangeEnvelope.MaximumJsonCharacters);
        Assert.True(smallFacts.Length<VisualArtifactInterchangeEnvelope.MaximumJsonCharacters);
        Assert.Throws<InvalidOperationException>(()=>artifact.ToInterchangeJson());
        Assert.Equal(native.ToPng(),artifact.ToPng());
    }
    [Fact]
    public void CompleteEnvelopeUtf8BudgetIncludesNonAsciiSourceAndMaskBeforeSnapshotCopies() {
        var source=NativeEffectWireFixture.Source(true,new string('\u03A9',65536),31);
        var mask=NativeEffectWireFixture.Mask(1024,800);
        var sourceJson=source.ToArtifact().ToInterchangeJson();
        var maskJson=NativeEffectWireFixture.Source().WithMask(mask).ToArtifact().ToInterchangeJson();
        var native=source.WithMask(mask);var artifact=native.ToArtifact();
        NativeEffectWireFixture.Save("native-wire-unicode-envelope",artifact.ToSvg(),artifact.ToPng());
        Assert.True(System.Text.Encoding.UTF8.GetByteCount(sourceJson)<VisualArtifactInterchangeEnvelope.MaximumJsonUtf8Bytes);
        Assert.True(System.Text.Encoding.UTF8.GetByteCount(maskJson)<VisualArtifactInterchangeEnvelope.MaximumJsonUtf8Bytes);
        Assert.True(sourceJson.Length+maskJson.Length<VisualArtifactInterchangeEnvelope.MaximumJsonCharacters);
        Assert.Throws<ArgumentOutOfRangeException>(()=>artifact.ToInterchangeJson());
        Assert.Throws<ArgumentOutOfRangeException>(()=>artifact.ToInterchangeUtf8Json());
        Assert.Equal(native.ToPng(),artifact.ToPng());
    }

}
