using System.Text.Json;
using System.Text.Json.Nodes;
using ChartForgeX.Effects;
using ChartForgeX.Rendering;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class PreparedJsonCapabilityTests {
    [Theory] [InlineData(false)] [InlineData(true)]
    public void ExplicitJsonQueryUsesCurrentCompleteMetadataWhileNativeWrappingAndOtherFormatsStayLazy(bool terminal) {
        var source = RetainedMaskFixture.Plate(); var mask = NativeEffectWireFixture.Mask(1024, 1024);
        var artifact = terminal ? source.CaptureLayer(new VisualLayerBlur(0), RetainedMaskFixture.Pixels).WithMask(mask).ToArtifact("query")
            : source.WithMask(mask).ToArtifact("query", VisualArtifactKind.Unknown, new VisualArtifactInterchangeEnvelope { Id = "query", Width = source.Size.Width, Height = source.Size.Height });
        var snapshot = Assert.IsType<Lazy<string>>(artifact.PreparedInterchangeSnapshot); var nativeSvg = artifact.ToSvg(); var nativePng = artifact.ToPng();
        var initiallyLazy = !snapshot.IsValueCreated; var declared = artifact.ExportFormats.HasFlag(VisualArtifactExportFormat.Json);
        var nonJson = artifact.SupportsExport(VisualArtifactExportFormat.Svg | VisualArtifactExportFormat.Png); var afterNonJsonLazy = !snapshot.IsValueCreated;
        var first = artifact.SupportsExport(VisualArtifactExportFormat.Json | VisualArtifactExportFormat.Svg);
        var text = new string('x', 65536); for (var index = 0; index < 45; index++) artifact.Metadata.Add("query-extra-" + index, text);
        var overBudget = artifact.SupportsExport(VisualArtifactExportFormat.Json); var mixedOverBudget = artifact.SupportsExport(VisualArtifactExportFormat.Json | VisualArtifactExportFormat.Png);
        var terminalStillLazy = !snapshot.IsValueCreated;
        for (var index = 0; index < 45; index++) artifact.Metadata.Remove("query-extra-" + index);
        var restored = artifact.SupportsExport(VisualArtifactExportFormat.Json); var noFormat = artifact.SupportsExport(VisualArtifactExportFormat.None);
        Save("prepared-json-query-" + terminal, new { initiallyLazy, declared, nonJson, afterNonJsonLazy, first, overBudget, mixedOverBudget, terminalStillLazy, restored, noFormat });
        NativeEffectWireFixture.Save("prepared-json-query-" + terminal, nativeSvg, nativePng);
        Assert.True(initiallyLazy); Assert.True(afterNonJsonLazy); Assert.True(declared); Assert.True(nonJson); Assert.True(first); Assert.False(overBudget); Assert.False(mixedOverBudget); Assert.True(restored); Assert.False(noFormat);
        if (terminal) Assert.True(terminalStillLazy);
        Assert.Equal(nativeSvg, artifact.ToSvg()); Assert.Equal(nativePng, artifact.ToPng());
    }
    [Fact]
    public void BorrowedQualificationAndExplicitExportShareTheSameCounterBeforeAnyMaskSnapshot() {
        var native = RetainedMaskFixture.Plate().WithMask(RetainedMaskFixture.Mask(64, 48, 1, VisualMaskMode.Alpha));
        Assert.True(native.TryGetRootEffects(out var effects)); var original = Assert.Single(effects).ToPortableBorrowed().Mask!.Pixels;
        var envelope = new VisualArtifactInterchangeEnvelope { Id = "borrowed", Width = native.Size.Width, Height = native.Size.Height };
        native.CapturePortableEffects(envelope, cloneMaskPixels: false); var borrowed = Assert.Single(envelope.Presentation!.CapturedEffects).Mask!.Pixels;
        var exported = native.ToArtifact().ToInterchangeEnvelope(); var copied = Assert.Single(exported.Presentation!.CapturedEffects).Mask!.Pixels;
        Save("prepared-json-borrowed", new { borrowedLength = borrowed.Length, copiedLength = copied.Length, sameBorrowed = ReferenceEquals(original, borrowed), separateExport = !ReferenceEquals(original, copied) });
        Assert.Same(original, borrowed); Assert.NotSame(original, copied); Assert.Equal(original, copied);
    }
    [Fact]
    public void CapturedClonePreservesItsImmutableSourceOutputFrameThroughSaveLoadAndExport() {
        var source = RetainedMaskFixture.Plate(); var layer = source.CaptureLayer(new VisualLayerBlur(1), RetainedMaskFixture.Pixels); var artifact = layer.ToArtifact("clone-source"); var clone = artifact.Clone();
        var supported = clone.SupportsExport(VisualArtifactExportFormat.Json); string? json = null; Exception? failure = null; CapturedVisualLayer? restored = null;
        try { json = clone.ToInterchangeJson(); restored = VisualArtifactInterchangeEnvelope.FromJson(json).ApplyCapturedLayer(source); } catch (Exception exception) { failure = exception; }
        Save("prepared-json-clone", new { supported, json, error = failure?.ToString(), sourceWidth = source.Size.Width, outputWidth = layer.Bounds.Width });
        NativeEffectWireFixture.Save("prepared-json-clone", artifact.ToSvg(), artifact.ToPng()); if (restored != null) NativeEffectWireFixture.Save("prepared-json-clone-restored", restored.Visual.ToSvg(), restored.Visual.ToPng());
        Assert.True(supported); Assert.Null(failure); Assert.Same(layer, clone.CapturedLayer); Assert.Same(artifact.Model, clone.Model); Assert.NotEqual(source.Size.Width, layer.Bounds.Width);
        Assert.Equal(artifact.ToSvg(), clone.ToSvg()); Assert.Equal(artifact.ToPng(), clone.ToPng());
        var replay = Assert.IsType<CapturedVisualLayer>(restored); Assert.Equal(layer.Bounds, replay.Bounds); Assert.Equal(layer.Visual.ToPng(RetainedMaskFixture.Pixels), replay.Visual.ToPng(RetainedMaskFixture.Pixels));
        var envelope = VisualArtifactInterchangeEnvelope.FromJson(json!); Assert.Equal(source.Size.Width, envelope.Width!.Value); Assert.Equal(layer.Bounds, Assert.Single(envelope.Presentation!.CapturedEffects).CaptureFrame!.Bounds);
    }
    [Theory] [InlineData(false)] [InlineData(true)]
    public void CapturedModelReplacementCannotAdvertiseOrExportLostTerminalSourceFacts(bool nonprepared) {
        var layer = RetainedMaskFixture.Plate().CaptureLayer(new VisualLayerBlur(1), RetainedMaskFixture.Pixels); var artifact = layer.ToArtifact();
        artifact.Model = nonprepared ? new object() : RetainedMaskFixture.White((int)layer.Bounds.Width, (int)layer.Bounds.Height);
        var supported = artifact.SupportsExport(VisualArtifactExportFormat.Json); var error = Record.Exception(() => artifact.ToInterchangeEnvelope()); Save("prepared-json-replaced-" + nonprepared, new { supported, error = error?.ToString() });
        Assert.False(supported); Assert.IsType<InvalidOperationException>(error);
    }
    [Fact]
    public void OrdinaryNonpreparedDeclarationsKeepTheirExistingFlagContract() {
        var artifact = VisualArtifact.Create("ordinary", VisualArtifactKind.Unknown, new object()); artifact.ExportFormats = VisualArtifactExportFormat.Json | VisualArtifactExportFormat.Svg;
        Assert.True(artifact.SupportsExport(VisualArtifactExportFormat.Json)); Assert.True(artifact.SupportsExport(VisualArtifactExportFormat.Json | VisualArtifactExportFormat.Svg)); Assert.False(artifact.SupportsExport(VisualArtifactExportFormat.Json | VisualArtifactExportFormat.Png)); Assert.False(artifact.SupportsExport(VisualArtifactExportFormat.None));
    }
    [Fact]
    public void ActualCapturedEffectsArrayLimitRejectsBeforeParsingTheFirstExcessEffect() {
        var source = RetainedMaskFixture.Plate().WithMask(NativeEffectWireFixture.Mask(1, 1)); var root = JsonNode.Parse(source.ToArtifact().ToInterchangeJson())!.AsObject(); var presentation = root["presentation"]!.AsObject(); var one = presentation["capturedEffects"]![0]!.DeepClone();
        var maximum = VisualSceneRasterRenderer.MaximumIsolatedDepth; var array = new JsonArray(); for (var index = 0; index < maximum; index++) array.Add(one.DeepClone()); presentation["capturedEffects"] = array;
        var valid = root.ToJsonString(); var literal = array.ToJsonString(); var over = valid.Replace(literal, literal.Substring(0, literal.Length - 1) + ",not-json]");
        var accepted = VisualArtifactInterchangeEnvelope.FromJson(valid); var error = Record.Exception(() => VisualArtifactInterchangeEnvelope.FromJson(over)); Save("prepared-json-effect-array", new { maximum, valid, over, error = error?.ToString() });
        Assert.Equal(maximum, accepted.Presentation!.CapturedEffects.Count); var rejected = Assert.IsType<ArgumentException>(error); Assert.Contains("array contains too many items", rejected.Message, StringComparison.Ordinal);
        Assert.Throws<NotSupportedException>(() => VisualArtifactInterchangeEnvelope.FromJson(source.ToArtifact().ToInterchangeJson().Replace("capturedEffects", "capturedMasks")));
    }
    private static void Save(string name, object facts) {
        if (Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY") is not { Length: > 0 } output) return; Directory.CreateDirectory(output); File.WriteAllText(Path.Combine(output, name + ".facts.json"), JsonSerializer.Serialize(facts));
    }
}
