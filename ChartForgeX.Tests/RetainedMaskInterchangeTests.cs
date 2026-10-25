using System;
using System.IO;
using System.Linq;
using System.Threading;
using ChartForgeX.Effects;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class RetainedMaskInterchangeTests {
    [Theory]
    [InlineData(390, false)] [InlineData(1100, false)]
    [InlineData(390, true)] [InlineData(1100, true)]
    public void PublicSequenceSaveLoadEditPrepareReplayKeepsPolicyAndActualSourceIdentities(int width, bool dark) {
        var model = RetainedMaskFixture.Sequence(dark).WithSize(width, 620);
        model.Metadata["raw-sequence"] = "001";
        model.Participants[0].Metadata["raw-participant"] = "review@01";
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(width, 620), 24), model.Theme, model.ThemeMode);
        var source = model.Prepare(context);
        var masked = source.WithMask(RetainedMaskFixture.Mask(width, 620, 1, VisualMaskMode.Luminance));
        var saved = masked.ToArtifact().ToInterchangeJson();
        var loaded = VisualArtifactInterchangeEnvelope.FromJson(saved);
        var replayed = loaded.ApplyCapturedEffects(source);
        var sourceFacts = VisualArtifactInterchangeEnvelope.FromJson(saved);
        sourceFacts.Presentation!.CapturedEffects.Clear();
        var editable = sourceFacts.ToSequenceArtifact();
        var restoredSource = editable.Prepare(context);
        var restored = loaded.ApplyCapturedEffects(restoredSource);
        var savedPixels = loaded.Presentation!.CapturedEffects.SelectMany(item => item.Mask!.Pixels).ToArray();
        editable.Messages[0].Text = "Approved";
        var editedSource = editable.Prepare(context);
        var edited = loaded.ApplyCapturedEffects(editedSource);
        Capture("retained-mask-roundtrip-" + width + "-" + dark, saved, source, masked, replayed, restored, edited);
        Assert.Equal(2, loaded.Version);
        Assert.Equal(saved, loaded.ToJson());
        Assert.Equal(masked.ToRgba(RetainedMaskFixture.Pixels).Pixels, replayed.ToRgba(RetainedMaskFixture.Pixels).Pixels);
        Assert.Equal(masked.ToRgba(RetainedMaskFixture.Pixels).Pixels, restored.ToRgba(RetainedMaskFixture.Pixels).Pixels);
        Assert.Equal(source.Regions.Select(region => region.Id), restored.Regions.Select(region => region.Id));
        Assert.Equal("001", editable.Metadata["raw-sequence"]);
        Assert.Equal("review@01", editable.Participants[0].Metadata["raw-participant"]);
        Assert.Equal("#guide", editable.Participants[0].Href);
        Assert.Equal("Save", model.Messages[0].Text);
        Assert.Contains("Approved", edited.ToSvg(new VisualSvgOptions(effectProfile: VisualSvgEffectProfile.NativeFilters)));
        Assert.Equal(savedPixels, Assert.Single(loaded.Presentation.CapturedEffects).Mask!.Pixels);
        Assert.Throws<NotSupportedException>(() => loaded.ToSequenceArtifact());
    }

    [Theory]
    [InlineData(VisualMaskMode.Alpha, 1)] [InlineData(VisualMaskMode.Alpha, 2)]
    [InlineData(VisualMaskMode.Luminance, 1)] [InlineData(VisualMaskMode.Luminance, 2)]
    public void OrderedOffOriginMaskReplayPreservesEveryRawByteAndTheExistingNativeCoverage(VisualMaskMode mode, int density) {
        var source = RetainedMaskFixture.Plate();
        var input = RetainedMaskFixture.Mask(64, 48, density, mode).ToImage();
        input.Pixels[0] = 17; input.Pixels[1] = 29; input.Pixels[2] = 43; input.Pixels[3] = 0;
        var first = new VisualMask(input, new ChartPoint(-1d / density, 0), density, mode, false);
        var second = RetainedMaskFixture.Mask(64, 48, density, VisualMaskMode.Alpha);
        var masked = source.WithMask(first).WithMask(second);
        var options = new VisualRenderOptions(density, 1);
        var json = masked.ToArtifact().ToInterchangeJson();
        var loaded = VisualArtifactInterchangeEnvelope.FromJson(json);
        var replayed = loaded.ApplyCapturedEffects(source);
        RetainedMaskFixture.Capture("retained-mask-order-roundtrip-" + mode + "-" + density, source, replayed, options);
        Assert.Equal(2, loaded.Presentation!.CapturedEffects.Count);
        var item = loaded.Presentation.CapturedEffects[0].Mask!;
        Assert.Equal(input.Pixels, item.Pixels);
        Assert.Equal(-1d / density, item.OriginX);
        Assert.Equal(mode, item.Mode);
        Assert.False(item.OrderedDither);
        Assert.Equal(masked.ToRgba(options).Pixels, replayed.ToRgba(options).Pixels);
        Assert.Equal(json, replayed.ToArtifact().ToInterchangeJson());
    }

    [Fact]
    public void MutablePortableArraysCannotChangeCapturedOrReplayedPresentation() {
        var source = RetainedMaskFixture.Plate();
        var masked = source.WithMask(RetainedMaskFixture.Mask(64, 48, 1, VisualMaskMode.Alpha));
        var envelope = masked.ToArtifact().ToInterchangeEnvelope();
        var bytes = masked.ToPng(RetainedMaskFixture.Pixels);
        var replayed = envelope.ApplyCapturedEffects(source);
        var replayBytes = replayed.ToPng(RetainedMaskFixture.Pixels);
        RetainedMaskFixture.Capture("retained-mask-portable-detachment", source, replayed, RetainedMaskFixture.Pixels);
        var item = Assert.Single(envelope.Presentation!.CapturedEffects).Mask!;
        Array.Clear(item.Pixels, 0, item.Pixels.Length);
        item.OriginX = 1000;
        envelope.Presentation.CapturedEffects.Clear();
        Assert.Equal(bytes, masked.ToPng(RetainedMaskFixture.Pixels));
        Assert.Equal(replayBytes, replayed.ToPng(RetainedMaskFixture.Pixels));
        var independent = masked.ToArtifact().ToInterchangeEnvelope();
        Assert.Contains(Assert.Single(independent.Presentation!.CapturedEffects).Mask!.Pixels, value => value > 0);
    }

    [Fact]
    public void EveryExistingEditableImporterRejectsCapturedPresentationLoss() {
        var source = RetainedMaskFixture.Plate();
        var masked = source.WithMask(RetainedMaskFixture.Mask(64, 48, 1, VisualMaskMode.Alpha));
        var envelope = masked.ToArtifact().ToInterchangeEnvelope();
        RetainedMaskFixture.Capture("retained-mask-import-loss-guards", source, masked, RetainedMaskFixture.Pixels);
        Action[] imports = {
            () => envelope.ToTopologyChart(), () => envelope.ToTopologyHierarchySource(),
            () => envelope.ToSequenceArtifact(), () => envelope.ToWaterfallChart(),
            () => envelope.ToTreeViewBlock(), () => envelope.ToRailroadDiagramBlock(),
            () => envelope.ToJourneyDiagramBlock(), () => envelope.ToTimelineDiagramBlock()
        };
        foreach (var import in imports) {
            var error = Assert.Throws<NotSupportedException>(import);
            Assert.Contains("captured effects", error.Message);
        }
    }

    [Fact]
    public void EmptyPolicyKeepsActualUnmaskedBytesVersionAndReplayIdentity() {
        var model = RetainedMaskFixture.Sequence(false);
        var source = model.Prepare(RetainedMaskFixture.Context(model));
        var envelope = source.ToArtifact().ToInterchangeEnvelope();
        var before = envelope.ToJson();
        var version = envelope.Version;
        var svg = source.ToSvg();
        var png = source.ToPng(RetainedMaskFixture.Pixels);
        RetainedMaskFixture.Capture("retained-mask-empty-policy", source, source, RetainedMaskFixture.Pixels);
        Assert.Empty(envelope.Presentation!.CapturedEffects);
        Assert.Equal(before, envelope.ToJson());
        Assert.DoesNotContain("capturedEffects", before);
        Assert.Equal(version, envelope.Version);
        Assert.Same(source, envelope.ApplyCapturedEffects(source));
        Assert.Equal(svg, source.ToSvg());
        Assert.Equal(png, source.ToPng(RetainedMaskFixture.Pixels));
        Assert.NotNull(envelope.ToSequenceArtifact());
    }

    private static void Capture(string name, string json, PreparedVisual source, params PreparedVisual[] stages) {
        var output = Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(output)) return;
        Directory.CreateDirectory(output);
        File.WriteAllText(Path.Combine(output, name + ".json"), json);
        for (var index = 0; index < stages.Length; index++)
            RetainedMaskFixture.Capture(name + "-stage-" + index, source, stages[index], RetainedMaskFixture.Pixels);
    }
}
