using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using ChartForgeX.Effects;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class RetainedAppearanceReplayTests {
    [Theory]
    [InlineData("outline")] [InlineData("bloom")] [InlineData("shadow")]
    public void FixedRootUsesTheSameNativeOperationWithoutChangingSourceViewportOrFacts(string kind) {
        var source = RetainedAppearanceFixture.Source();
        var before = source.ToSvg();
        var root = RetainedAppearanceFixture.Wrap(source, kind);
        var expected = RetainedAppearanceFixture.Operation(kind).Capture(source, RetainedAppearanceFixture.Options, CancellationToken.None);
        var image = expected.ToImage();
        var cropped = RasterEffects.Crop(new RasterEffectResult(image, new RasterEffectRegion(
            (int)expected.Bounds.X, (int)expected.Bounds.Y, image.Width, image.Height)),
            new RasterEffectRegion(0, 0, 8, 6), CancellationToken.None);
        RetainedAppearanceFixture.Save("retained-root-" + kind, root);
        Assert.Equal(source.Size.Width, root.Size.Width);
        Assert.Equal(source.Size.Height, root.Size.Height);
        Assert.Equal(source.Regions.Select(row => row.Id), root.Regions.Select(row => row.Id));
        Assert.Equal(cropped.Pixels, root.ToRgba(RetainedAppearanceFixture.Options).Pixels);
        Assert.Equal(before, source.ToSvg());
        var envelope = root.ToArtifact().ToInterchangeEnvelope();
        var step = Assert.Single(envelope.Presentation!.CapturedEffects);
        Assert.Null(step.CaptureFrame);
        Assert.Equal(2, envelope.Version);
        Assert.Equal(root.ToRgba(RetainedAppearanceFixture.Options).Pixels,
            envelope.ApplyCapturedEffects(source).ToRgba(RetainedAppearanceFixture.Options).Pixels);
    }

    [Fact]
    public void FractionalShadowOpacityRemainsRawThroughPortableReplayAndNativeFilterPolicy() {
        var builder = new VisualSceneBuilder(new VisualSize(32, 20), FontSpec.SystemSans());
        builder.Rect(new ChartRect(4, 4, 4, 4), ChartColor.FromRgba(255, 0, 0, 25));
        var source = new PreparedVisual(builder.Build());
        var root = source.WithDropShadow(new VisualDropShadow(ChartColor.FromRgba(0, 0, 255, 255),
            8, 0, 0, orderedDither: false, opacity: .06));
        var envelope = root.ToArtifact().ToInterchangeEnvelope();
        var loaded = VisualArtifactInterchangeEnvelope.FromJson(envelope.ToJson());
        var replay = loaded.ApplyCapturedEffects(source);
        RetainedAppearanceFixture.Save("retained-shadow-raw-opacity", root);
        RetainedAppearanceFixture.Save("retained-shadow-raw-opacity-replayed", replay);
        var native = XDocument.Parse(root.ToSvg(new VisualSvgOptions(effectProfile: VisualSvgEffectProfile.NativeFilters)));
        var flood = Assert.Single(native.Descendants(), node => node.Name.LocalName == "feFlood");
        Assert.Equal(.06, double.Parse(flood.Attribute("flood-opacity")!.Value, System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal(.06, Assert.Single(loaded.Presentation!.CapturedEffects).DropShadow!.Opacity);
        Assert.Equal(new byte[] { 255, 0, 0, 25 }, AppearanceFixture.Pixel(root.ToRgba(RetainedAppearanceFixture.Options), 5, 5));
        Assert.Equal(new byte[] { 0, 0, 255, 2 }, AppearanceFixture.Pixel(root.ToRgba(RetainedAppearanceFixture.Options), 14, 5));
        Assert.Equal(root.ToPng(RetainedAppearanceFixture.Options), replay.ToPng(RetainedAppearanceFixture.Options));
        Assert.Equal(root.ToSvg(), replay.ToSvg());
    }

    [Fact]
    public void ZeroOffsetAndSigmaShadowStillPaintsUnderSemitransparentSource() {
        var source = RetainedAppearanceFixture.Source();
        var root = source.WithDropShadow(new VisualDropShadow(ChartColor.FromRgba(0, 0, 255, 128), 0, 0, 0, orderedDither: false));
        RetainedAppearanceFixture.Save("retained-shadow-zero-is-not-identity", root);
        Assert.NotSame(source, root);
        Assert.Equal(new byte[] { 204, 0, 51, 160 }, AppearanceFixture.Pixel(root.ToRgba(RetainedAppearanceFixture.Options), 3, 3));
        Assert.Equal(128, AppearanceFixture.Pixel(source.ToRgba(RetainedAppearanceFixture.Options), 3, 3)[3]);
    }

    [Theory]
    [InlineData("outline")] [InlineData("bloom-strength")] [InlineData("bloom-threshold")] [InlineData("shadow")]
    public void UnconditionalNoopsReturnTheSamePreparedOwnerAndExactOrdinaryBytes(string kind) {
        var source = RetainedAppearanceFixture.Source(); var svg = source.ToSvg(); var png = source.ToPng();
        var json = source.ToArtifact().ToInterchangeJson();
        var result = kind == "outline" ? source.WithOutline(new VisualOutline(ChartColor.Black, 0))
            : kind == "shadow" ? source.WithDropShadow(new VisualDropShadow(ChartColor.Transparent, 2, 2, 1))
            : source.WithBloom(new VisualBloom(1, kind == "bloom-threshold" ? 1 : .5, kind == "bloom-strength" ? 0 : 1));
        RetainedAppearanceFixture.Save("retained-noop-" + kind, result);
        Assert.Same(source, result);
        Assert.Equal(svg, result.ToSvg()); Assert.Equal(png, result.ToPng());
        Assert.Equal(json, result.ToArtifact().ToInterchangeJson());
    }

    [Fact]
    public void OrderedRootMasksAndOutlineDoNotCommuteAndPortableReplayKeepsThatOrder() {
        var source = RetainedAppearanceFixture.Source();
        var mask = new VisualMask(AppearanceFixture.Solid(3, 6, 255), orderedDither: false);
        var outline = new VisualOutline(ChartColor.FromRgba(0, 0, 255, 255), 1, false);
        var first = source.WithOutline(outline).WithMask(mask);
        var second = source.WithMask(mask).WithOutline(outline);
        RetainedAppearanceFixture.Save("retained-order-outline-mask", first);
        RetainedAppearanceFixture.Save("retained-order-mask-outline", second);
        Assert.Equal(new byte[4], AppearanceFixture.Pixel(first.ToRgba(RetainedAppearanceFixture.Options), 3, 3));
        Assert.Equal(new byte[] { 0, 0, 255, 128 }, AppearanceFixture.Pixel(second.ToRgba(RetainedAppearanceFixture.Options), 3, 3));
        foreach (var root in new[] { first, second }) {
            var saved = root.ToArtifact().ToInterchangeJson(); var loaded = VisualArtifactInterchangeEnvelope.FromJson(saved);
            Assert.Equal(2, loaded.Presentation!.CapturedEffects.Count);
            Assert.Equal(root.ToRgba(RetainedAppearanceFixture.Options).Pixels,
                loaded.ApplyCapturedEffects(source).ToRgba(RetainedAppearanceFixture.Options).Pixels);
        }
    }

    [Fact]
    public void TerminalCaptureKeepsSourceDimensionsOutputBoundsAndFreshSourceFactsSeparate() {
        var source = RetainedAppearanceFixture.Source();
        var layer = source.WithOutline(new VisualOutline(ChartColor.Black, 1, false))
            .CaptureLayer(new VisualLayerBlur(1, orderedDither: false), RetainedAppearanceFixture.Options)
            .WithMask(new VisualMask(AppearanceFixture.Solid(8, 6, 128), orderedDither: false));
        var artifact = layer.ToArtifact(); var saved = artifact.ToInterchangeJson();
        var envelope = VisualArtifactInterchangeEnvelope.FromJson(saved);
        var replayed = envelope.ApplyCapturedLayer(source);
        var fresh = RetainedAppearanceFixture.Source("fresh-source");
        var edited = envelope.ApplyCapturedLayer(fresh);
        RetainedAppearanceFixture.Save("terminal-source-frame", layer, saved);
        RetainedAppearanceFixture.Save("terminal-replayed", replayed);
        Assert.Equal(8d, envelope.Width); Assert.Equal(6d, envelope.Height);
        Assert.True(artifact.NaturalSize.HasValue);
        var natural = artifact.NaturalSize.GetValueOrDefault();
        Assert.Equal(layer.Bounds.Width, natural.Width);
        Assert.Equal(layer.Bounds.Height, natural.Height);
        Assert.Equal(layer.Bounds, envelope.Presentation!.CapturedEffects[1].CaptureFrame!.Bounds);
        Assert.Equal(3, envelope.Presentation.CapturedEffects.Count);
        Assert.Equal(layer.ToImage().Pixels, replayed.ToImage().Pixels);
        Assert.Equal("fresh-source", Assert.Single(edited.Source.Regions).Id);
        Assert.DoesNotContain(edited.Source.Regions, row => row.Id == "source-mark");
        Assert.Null(layer.Visual.Scene.ValueCapture); Assert.Null(layer.Visual.Scene.VisibilityCapture);
        Assert.Throws<NotSupportedException>(() => envelope.ApplyCapturedEffects(source));
        var sourceFacts = source.ToArtifact().ToInterchangeEnvelope();
        Assert.Throws<ArgumentException>(() => layer.Visual.ToArtifact(sourceFacts.Id, sourceFacts.Kind, sourceFacts));
        artifact.Model = RetainedAppearanceFixture.Source(size: new VisualSize(layer.Bounds.Width, layer.Bounds.Height));
        Assert.Throws<InvalidOperationException>(() => artifact.ToInterchangeEnvelope());
    }

    [Fact]
    public void NoBrightTerminalReplayRejectsEditedExpansionRatherThanAttachingAnOldFootprint() {
        var source = RetainedAppearanceFixture.Source();
        var layer = source.CaptureLayer(new VisualBloom(1, .9, 1, orderedDither: false), RetainedAppearanceFixture.Options);
        var saved = layer.ToArtifact().ToInterchangeJson(); var envelope = VisualArtifactInterchangeEnvelope.FromJson(saved);
        var bright = RetainedAppearanceFixture.Source(white: true);
        var actualChanged = bright.CaptureLayer(new VisualBloom(1, .9, 1, orderedDither: false), RetainedAppearanceFixture.Options);
        RetainedAppearanceFixture.Save("terminal-no-bright-before-edit", layer, saved);
        RetainedAppearanceFixture.Save("terminal-bright-fresh-footprint", actualChanged);
        Assert.Equal(new ChartRect(0, 0, 8, 6), layer.Bounds);
        Assert.True(actualChanged.Bounds.Width > layer.Bounds.Width);
        Assert.Equal(layer.Bounds, envelope.ApplyCapturedLayer(source).Bounds);
        Assert.Throws<InvalidOperationException>(() => envelope.ApplyCapturedLayer(bright));
        Assert.Equal(bright.ToRgba(RetainedAppearanceFixture.Options).Pixels, bright.CaptureLayer(new VisualLayerBlur(0), RetainedAppearanceFixture.Options).ToImage().Pixels);
    }

    [Fact]
    public void ReplayClonesMutableDtoPixelsAndFieldsBeforeTheyCanChangeTheRetainedOwner() {
        var source = RetainedAppearanceFixture.Source();
        var root = source.WithOutline(new VisualOutline(ChartColor.Black, 1)).WithMask(new VisualMask(AppearanceFixture.Solid(8, 6, 128)));
        var envelope = root.ToArtifact().ToInterchangeEnvelope(); var replayed = envelope.ApplyCapturedEffects(source);
        var svg = replayed.ToSvg(new VisualSvgOptions(effectProfile: VisualSvgEffectProfile.NativeFilters)); var pixels = replayed.ToPng();
        var mask = envelope.Presentation!.CapturedEffects[1].Mask!;
        Array.Clear(mask.Pixels, 0, mask.Pixels.Length); mask.OriginX = 500;
        envelope.Presentation.CapturedEffects[0].Outline = new VisualOutline(ChartColor.White, 2);
        envelope.Presentation.CapturedEffects.Clear();
        RetainedAppearanceFixture.Save("retained-replay-dto-detachment", replayed);
        Assert.Equal(svg, replayed.ToSvg(new VisualSvgOptions(effectProfile: VisualSvgEffectProfile.NativeFilters)));
        Assert.Equal(pixels, replayed.ToPng());
        Assert.Equal(2, root.ToArtifact().ToInterchangeEnvelope().Presentation!.CapturedEffects.Count);
    }

    [Fact]
    public void CapturedMaskInputsAreRetainedAndDebitedOnceAtTheActualSmallWorkflowBoundary() {
        var builder = new VisualSceneBuilder(new VisualSize(1, 1), FontSpec.SystemSans());
        builder.Rect(new ChartRect(0, 0, 1, 1), ChartColor.FromRgba(255, 0, 0, 255));
        var source = new PreparedVisual(builder.Build());
        var first = new VisualMask(AppearanceFixture.Solid(1, 1, 128), orderedDither: false);
        var other = new VisualMask(AppearanceFixture.Solid(1, 1, 128), orderedDither: false);
        var distinct = source.CaptureLayer(first, RetainedAppearanceFixture.Options).WithMask(other);
        Assert.Throws<ArgumentOutOfRangeException>(() => distinct.PrepareMask(first, maximumLiveBytes: 19));
        var exact = distinct.PrepareMask(first, maximumLiveBytes: 20);
        var output = RasterEffects.Execute(exact, 0, false, CancellationToken.None).Image;
        NativeEffectBeforeCapture.Write("retained-mask-input-budget", null, output.ToPng(), output);
        Assert.Equal(new byte[] { 255, 0, 0, 32 }, output.Pixels);
        Assert.Equal(20, exact.ConservativeLiveBytes);
        var shared = source.CaptureLayer(first, RetainedAppearanceFixture.Options).WithMask(first);
        Assert.Equal(16, shared.PrepareMask(first, maximumLiveBytes: 16).ConservativeLiveBytes);
        Assert.Equal(output.Pixels, distinct.WithMask(first).ToImage().Pixels);
    }
}

internal static class RetainedAppearanceFixture {
    internal static readonly VisualRenderOptions Options = new(supersampling: 1);
    internal static PreparedVisual Source(string id = "source-mark", bool white = false, VisualSize? size = null) {
        var builder = new VisualSceneBuilder(size ?? new VisualSize(8, 6), FontSpec.SystemSans());
        builder.Rect(new ChartRect(2, 2, 3, 2), white ? ChartColor.White : ChartColor.FromRgba(255, 0, 0, 128));
        builder.AddRegion(new VisualSemanticRegion(id, "sample", new ChartRect(2, 2, 3, 2), "Native source"));
        return new PreparedVisual(builder.Build());
    }
    internal static VisualCapturedEffect Operation(string kind) => kind == "outline"
        ? VisualCapturedEffect.From(new VisualOutline(ChartColor.FromRgba(0, 0, 255, 255), 1, false))
        : kind == "bloom" ? VisualCapturedEffect.From(new VisualBloom(1, .1, .7, orderedDither: false))
        : VisualCapturedEffect.From(new VisualDropShadow(ChartColor.FromRgba(0, 0, 255, 128), 1, 1, 1, orderedDither: false));
    internal static PreparedVisual Wrap(PreparedVisual source, string kind) => source.WithRootEffect(Operation(kind));
    internal static void Save(string name, PreparedVisual source) =>
        NativeEffectBeforeCapture.Write(name, source.ToSvg(new VisualSvgOptions(effectProfile: VisualSvgEffectProfile.NativeFilters)), source.ToPng(Options), source.ToRgba(Options));
    internal static void Save(string name, CapturedVisualLayer layer, string? json = null) {
        NativeEffectBeforeCapture.Write(name, layer.Visual.ToSvg(), layer.Visual.ToPng(), layer.ToImage());
        if (json != null) NativeEffectBeforeCapture.Json(name + "-interchange", new { json });
    }
}
