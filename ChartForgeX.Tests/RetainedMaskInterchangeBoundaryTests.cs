using System;
using System.Linq;
using System.Threading;
using ChartForgeX.Effects;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class RetainedMaskInterchangeBoundaryTests {
    [Fact]
    public void InvalidPortableInputsAndNoncanonicalBase64RejectBeforeReplay() {
        var source = RetainedMaskFixture.Plate();
        var mask = new VisualMask(new RgbaImage(1, 1, new byte[] { 31, 17, 43, 255 }), orderedDither: false);
        var masked = source.WithMask(mask);
        var json = masked.ToArtifact().ToInterchangeJson();
        RetainedMaskFixture.Capture("retained-mask-portable-validation", source, masked, RetainedMaskFixture.Pixels);
        var encoded = Convert.ToBase64String(mask.ToImage().Pixels);
        Assert.Throws<NotSupportedException>(() => VisualArtifactInterchangeEnvelope.FromJson(json.Replace("\"version\":2", "\"version\":1")));
        Assert.Throws<ArgumentException>(() => VisualArtifactInterchangeEnvelope.FromJson(json.Replace(encoded, encoded.Insert(1, " "))));
        var noncanonical = encoded.Substring(0, encoded.Length - 3) + "x==";
        Assert.Throws<ArgumentException>(() => VisualArtifactInterchangeEnvelope.FromJson(json.Replace(encoded, noncanonical)));
        Action<VisualArtifactInterchangeMask>[] edits = {
            item => item.ImageWidth = 0,
            item => item.Mode = (VisualMaskMode)99,
            item => item.PixelsPerUnit = 17,
            item => item.OriginX = .5
        };
        foreach (var edit in edits) {
            var copy = VisualArtifactInterchangeEnvelope.FromJson(json);
            edit(Assert.Single(copy.Presentation!.CapturedEffects).Mask!);
            Assert.Throws<ArgumentOutOfRangeException>(copy.Validate);
        }
        foreach (var length in new[] { 3, 5 }) {
            var copy = VisualArtifactInterchangeEnvelope.FromJson(json);
            Assert.Single(copy.Presentation!.CapturedEffects).Mask!.Pixels = new byte[length];
            Assert.Throws<ArgumentException>(copy.Validate);
        }
        var mixed = VisualArtifactInterchangeEnvelope.FromJson(json);
        mixed.Presentation!.CapturedEffects.Add(new VisualArtifactInterchangeCapturedEffect {
            Mask = new VisualArtifactInterchangeMask {
                ImageWidth = 1, ImageHeight = 1, PixelsPerUnit = 2, Pixels = new byte[4]
            }
        });
        Assert.Throws<ArgumentException>(mixed.Validate);
        var deep = VisualArtifactInterchangeEnvelope.FromJson(json);
        var record = Assert.Single(deep.Presentation!.CapturedEffects);
        for (var index = 1; index <= VisualSceneRasterRenderer.MaximumIsolatedDepth; index++) deep.Presentation.CapturedEffects.Add(record);
        Assert.Throws<NotSupportedException>(deep.Validate);
        Assert.Equal(json, masked.ToArtifact().ToInterchangeJson());
    }

    [Fact]
    public void ReplayRequiresUnmaskedExactViewportAndCancellationIsAtomic() {
        var source = RetainedMaskFixture.Plate();
        var masked = source.WithMask(RetainedMaskFixture.Mask(64, 48, 1, VisualMaskMode.Alpha));
        var envelope = masked.ToArtifact().ToInterchangeEnvelope();
        var original = source.ToSvg();
        RetainedMaskFixture.Capture("retained-mask-portable-replay-boundary", source, masked, RetainedMaskFixture.Pixels);
        Assert.Throws<ArgumentException>(() => envelope.ApplyCapturedEffects(masked));
        Assert.Throws<ArgumentException>(() => envelope.ApplyCapturedEffects(RetainedMaskFixture.White(65, 48)));
        Assert.Throws<OperationCanceledException>(() => envelope.ApplyCapturedEffects(source, new CancellationToken(true)));
        Assert.Equal(original, source.ToSvg());
        Assert.Equal(masked.ToPng(RetainedMaskFixture.Pixels), envelope.ApplyCapturedEffects(source).ToPng(RetainedMaskFixture.Pixels));
    }

    [Fact]
    public void EmbeddedMaskedScopesRemainExplicitlyNonportableWithoutChangingNativePixels() {
        var source = RetainedMaskFixture.Plate();
        var masked = source.WithMask(RetainedMaskFixture.Mask(64, 48, 1, VisualMaskMode.Alpha));
        var group = new VisualSceneGroup("parent", "parent", null, null, translation: new ChartPoint(4, 6));
        var nodes = new[] { group }.Cast<VisualSceneNode>().Concat(masked.Scene.Nodes)
            .Concat(new[] { new VisualSceneEndGroup() }).ToArray();
        var composed = new PreparedVisual(new VisualScene(new VisualSize(80, 64), nodes,
            masked.Diagnostics, Array.Empty<VisualSemanticRegion>()));
        var artifact = composed.ToArtifact("embedded-mask", VisualArtifactKind.Unknown);
        RetainedMaskFixture.Capture("retained-mask-portable-embedded-rejection", source, composed, RetainedMaskFixture.Pixels);
        Assert.False(artifact.SupportsExport(VisualArtifactExportFormat.Json));
        Assert.Throws<NotSupportedException>(() => artifact.ToInterchangeEnvelope());
        artifact.ExportFormats |= VisualArtifactExportFormat.Json;
        Assert.Throws<NotSupportedException>(() => artifact.ToInterchangeJson());
        Assert.Contains("mask", composed.ToSvg(new VisualSvgOptions(effectProfile: VisualSvgEffectProfile.NativeFilters)));
        Assert.Contains(composed.ToRgba(RetainedMaskFixture.Pixels).Pixels, value => value > 0);
    }

    [Fact]
    public void SuppliedSemanticMasksCannotAdvertiseUnpaintedPresentationOnAnUnmaskedScene() {
        var source = RetainedMaskFixture.Plate();
        var masked = source.WithMask(RetainedMaskFixture.Mask(64, 48, 1, VisualMaskMode.Alpha));
        var envelope = masked.ToArtifact().ToInterchangeEnvelope();
        var original = source.ToSvg();
        RetainedMaskFixture.Capture("retained-mask-portable-semantic-mismatch", source, masked, RetainedMaskFixture.Pixels);
        Assert.Throws<ArgumentException>(() => source.ToArtifact(envelope.Id, envelope.Kind, envelope));
        Assert.Equal(original, source.ToSvg());
    }

    [Fact]
    public void ExistingJsonBudgetBoundsLargeMasksWhileNativeAndSourceInspectionRemainAvailable() {
        var source = RetainedMaskFixture.Plate();
        var pixels = new byte[1536 * 1024 * 4];
        for (var index = 3; index < pixels.Length; index += 4) pixels[index] = 255;
        var mask = new VisualMask(new RgbaImage(1536, 1024, pixels), orderedDither: false);
        var masked = source.WithMask(mask);
        var artifact = masked.ToArtifact();
        RetainedMaskFixture.Capture("retained-mask-portable-character-budget", source, masked, RetainedMaskFixture.Pixels);
        // The raw pixel payload alone is exactly8MiB base64; required JSON syntax/facts exceed the incumbent total.
        var supportsJson = artifact.SupportsExport(VisualArtifactExportFormat.Json);
        var wireFailure = Record.Exception(() => artifact.ToInterchangeEnvelope());
        var output = Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(output)) System.IO.File.WriteAllText(System.IO.Path.Combine(output, "retained-mask-portable-character-budget.facts.json"),
            System.Text.Json.JsonSerializer.Serialize(new { rawBytes = pixels.Length, base64Characters = VisualArtifactInterchangeValidation.Base64Length(pixels.LongLength),
                limit = VisualArtifactInterchangeEnvelope.MaximumJsonCharacters, supportsJson, error = wireFailure?.ToString() }));
        Assert.False(supportsJson);
        Assert.IsType<InvalidOperationException>(wireFailure);
        Assert.Equal(source.ToRgba(RetainedMaskFixture.Pixels).Pixels, masked.ToRgba(RetainedMaskFixture.Pixels).Pixels);
        Assert.Equal(source.Regions.Select(region => region.Id), masked.Regions.Select(region => region.Id));
    }
}
