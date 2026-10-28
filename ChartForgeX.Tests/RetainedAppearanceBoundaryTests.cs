using System;
using System.Threading;
using System.Text.Json.Nodes;
using ChartForgeX.Effects;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class RetainedAppearanceBoundaryTests {
    [Fact]
    public void EveryStepHasOnePayloadAndTheCaptureBoundaryHasOnlyItsRealSupportedFollowers() {
        var source = RetainedAppearanceFixture.Source();
        var envelope = source.WithOutline(new VisualOutline(ChartColor.Black, 1)).ToArtifact().ToInterchangeEnvelope();
        var saved = envelope.ToJson();
        RetainedAppearanceFixture.Save("retained-boundary-source", source);
        var empty = VisualArtifactInterchangeEnvelope.FromJson(saved);
        empty.Presentation!.CapturedEffects[0] = new VisualArtifactInterchangeCapturedEffect();
        Assert.Throws<ArgumentException>(empty.Validate);
        var two = VisualArtifactInterchangeEnvelope.FromJson(saved);
        two.Presentation!.CapturedEffects[0].Bloom = new VisualBloom(1, .5, 1);
        Assert.Throws<ArgumentException>(two.Validate);
        var blur = VisualArtifactInterchangeEnvelope.FromJson(saved);
        blur.Presentation!.CapturedEffects[0] = new VisualArtifactInterchangeCapturedEffect { Blur = new VisualLayerBlur(1) };
        Assert.Throws<NotSupportedException>(blur.Validate);
        var capture = source.CaptureLayer(new VisualLayerBlur(1), RetainedAppearanceFixture.Options).ToArtifact().ToInterchangeEnvelope();
        capture.Presentation!.CapturedEffects.Add(new VisualArtifactInterchangeCapturedEffect { Bloom = new VisualBloom(1, .5, 1) });
        Assert.Throws<NotSupportedException>(capture.Validate);
        capture.Presentation.CapturedEffects.RemoveAt(1);
        capture.Presentation.CapturedEffects.Add(capture.Presentation.CapturedEffects[0]);
        Assert.Throws<NotSupportedException>(capture.Validate);
        Assert.Equal(saved, envelope.ToJson());
    }

    [Fact]
    public void OldUnpublishedMaskKeyCannotBeSilentlyReadAsAnEmptyCurrentPresentation() {
        var source = RetainedAppearanceFixture.Source();
        var saved = source.WithMask(new VisualMask(AppearanceFixture.Solid(8, 6, 128))).ToArtifact().ToInterchangeJson();
        NativeEffectBeforeCapture.Json("retained-wire-boundary", new { saved });
        var node = JsonNode.Parse(saved) ?? throw new InvalidOperationException("Current serializer returned no JSON object.");
        var presentation = node["presentation"] ?? throw new InvalidOperationException("Nonempty mask presentation is missing.");
        var steps = presentation["capturedEffects"]?.AsArray() ?? throw new InvalidOperationException("Ordered effect array is missing.");
        var mask = steps[0]?["mask"]?.DeepClone() ?? throw new InvalidOperationException("Mask payload is missing.");
        presentation.AsObject().Remove("capturedEffects");
        presentation["capturedMasks"] = new JsonArray(mask);
        var legacy = node.ToJsonString();
        NativeEffectBeforeCapture.Json("retained-wire-legacy-input", new { legacy });
        Assert.Throws<NotSupportedException>(() => VisualArtifactInterchangeEnvelope.FromJson(legacy));
        Assert.Throws<NotSupportedException>(() => VisualArtifactInterchangeEnvelope.FromJson(saved.Replace("\"version\":2", "\"version\":1")));
    }

    [Fact]
    public void CanceledAndWrongViewportReplayDoNotPublishOrMutateOriginalSource() {
        var source = RetainedAppearanceFixture.Source();
        var root = source.WithOutline(new VisualOutline(ChartColor.Black, 1));
        var envelope = root.ToArtifact().ToInterchangeEnvelope();
        var captured = root.CaptureLayer(new VisualLayerBlur(1), RetainedAppearanceFixture.Options).ToArtifact().ToInterchangeEnvelope();
        var before = source.ToSvg();
        RetainedAppearanceFixture.Save("retained-cancellation-before", source);
        Assert.Throws<OperationCanceledException>(() => envelope.ApplyCapturedEffects(source, new CancellationToken(true)));
        Assert.Throws<OperationCanceledException>(() => captured.ApplyCapturedLayer(source, new CancellationToken(true)));
        Assert.Throws<ArgumentException>(() => envelope.ApplyCapturedEffects(root));
        Assert.Throws<ArgumentException>(() => captured.ApplyCapturedLayer(RetainedAppearanceFixture.Source(size: new VisualSize(9, 6))));
        Assert.Equal(before, source.ToSvg());
        Assert.Equal(root.ToRgba(RetainedAppearanceFixture.Options).Pixels, envelope.ApplyCapturedEffects(source).ToRgba(RetainedAppearanceFixture.Options).Pixels);
    }
}
