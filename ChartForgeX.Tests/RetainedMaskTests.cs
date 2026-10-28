using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Effects;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class RetainedMaskTests {
    [Theory]
    [InlineData(VisualMaskMode.Alpha, 1)] [InlineData(VisualMaskMode.Luminance, 1)]
    [InlineData(VisualMaskMode.Alpha, 2)] [InlineData(VisualMaskMode.Luminance, 2)]
    public void RetainedNativeMaskMatchesExistingCaptureAndKeepsNonzeroSourceRgb(VisualMaskMode mode, int density) {
        var source = RetainedMaskFixture.Plate();
        var input = RetainedMaskFixture.Mask(64, 48, density, mode);
        var options = new VisualRenderOptions(density, 1);
        var before = source.ToRgba(options);
        var expected = source.CaptureLayer(input, options).ToImage();
        var masked = source.WithMask(input);
        var actual = masked.ToRgba(options);
        RetainedMaskFixture.Capture("retained-mask-pixels-" + mode + "-" + density, source, masked, options);
        Assert.Equal(expected.Width, actual.Width);
        Assert.Equal(expected.Height, actual.Height);
        Assert.Equal(expected.Pixels, actual.Pixels);
        Assert.Contains(actual.Pixels, value => value > 0);
        for (var at = 0; at < actual.Pixels.Length; at += 4) {
            if (actual.Pixels[at + 3] == 0) continue;
            Assert.Equal(before.Pixels[at], actual.Pixels[at]);
            Assert.Equal(before.Pixels[at + 1], actual.Pixels[at + 1]);
            Assert.Equal(before.Pixels[at + 2], actual.Pixels[at + 2]);
        }
        Assert.Equal(source.Size, masked.Size);
        Assert.Equal(source.Regions, masked.Regions);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void NativeFilterProfileRetainsVectorCaptionsLinksAndNativeLumaCoverageWhileBakedIsExplicit(bool dark) {
        var model = RetainedMaskFixture.Sequence(dark);
        var source = model.Prepare(RetainedMaskFixture.Context(model));
        var input = RetainedMaskFixture.Mask(800, 900, 1, VisualMaskMode.Luminance);
        var masked = source.WithMask(input);
        var svg = masked.ToSvg(new VisualSvgOptions(idPrefix: "mask-native", effectProfile: VisualSvgEffectProfile.NativeFilters));
        var baked = masked.ToSvg(new VisualSvgOptions(idPrefix: "mask-baked", effectRasterOptions: RetainedMaskFixture.Pixels));
        RetainedMaskFixture.Capture("retained-mask-sequence-" + dark, source, masked, RetainedMaskFixture.Pixels);
        var nativeXml = XDocument.Parse(svg);
        var sourceXml = XDocument.Parse(source.ToSvg());
        var definition = Assert.Single(nativeXml.Descendants(), element => element.Name.LocalName == "mask");
        Assert.Equal("alpha", definition.Attribute("mask-type")!.Value);
        Assert.Equal(sourceXml.Descendants().Count(element => element.Name.LocalName == "text"),
            nativeXml.Descendants().Count(element => element.Name.LocalName == "text"));
        Assert.Single(nativeXml.Descendants(), element => element.Name.LocalName == "a" &&
            (string?)element.Attribute("href") == "#guide");
        var coverageElement = Assert.Single(definition.Descendants(), element => element.Name.LocalName == "image");
        var coverage = RetainedMaskFixture.DecodeImage(coverageElement);
        var white = RetainedMaskFixture.White(800, 900);
        var expected = white.CaptureLayer(input, RetainedMaskFixture.Pixels).ToImage();
        Assert.Equal(expected.Pixels, coverage.Pixels);
        var bakedXml = XDocument.Parse(baked);
        Assert.DoesNotContain(bakedXml.Descendants(), element => element.Name.LocalName == "text");
        var bitmap = Assert.Single(bakedXml.Descendants(), element => element.Name.LocalName == "image");
        Assert.Equal(masked.ToRgba(RetainedMaskFixture.Pixels).Pixels, RetainedMaskFixture.DecodeImage(bitmap).Pixels);
        Assert.Equal(source.Regions.Select(region => region.Id), masked.Regions.Select(region => region.Id));
        Assert.Equal(source.SemanticInterchange!.Nodes.Select(node => node.Label), masked.SemanticInterchange!.Nodes.Select(node => node.Label));
    }

    [Fact]
    public void MaskInputAndGetterMutationCannotChangeRetainedPresentationOrOriginalSource() {
        var source = RetainedMaskFixture.Plate();
        var pixels = Enumerable.Repeat((byte)255, 64 * 48 * 4).ToArray();
        var input = new VisualMask(new RgbaImage(64, 48, pixels), orderedDither: false);
        var masked = source.WithMask(input);
        var svg = masked.ToSvg("mask-detached");
        var png = masked.ToPng(RetainedMaskFixture.Pixels);
        RetainedMaskFixture.Capture("retained-mask-detachment", source, masked, RetainedMaskFixture.Pixels);
        Array.Clear(pixels, 0, pixels.Length);
        var copy = input.ToImage();
        Array.Clear(copy.Pixels, 0, copy.Pixels.Length);
        Assert.Equal(svg, masked.ToSvg("mask-detached"));
        Assert.Equal(png, masked.ToPng(RetainedMaskFixture.Pixels));
        Assert.Equal(source.ToRgba(RetainedMaskFixture.Pixels).Pixels, masked.ToRgba(RetainedMaskFixture.Pixels).Pixels);
    }

    [Fact]
    public void CompleteRootMaskCapturesPortablePolicyWithoutChangingOriginalSourceFacts() {
        var model = RetainedMaskFixture.Sequence(false);
        var source = model.Prepare(RetainedMaskFixture.Context(model));
        var originalJson = source.ToArtifact().ToInterchangeJson();
        var mask = RetainedMaskFixture.Mask(800, 900, 1, VisualMaskMode.Alpha);
        var masked = source.WithMask(mask);
        var artifact = masked.ToArtifact();
        var envelope = artifact.ToInterchangeEnvelope();
        RetainedMaskFixture.Capture("retained-mask-portable-boundary", source, masked, RetainedMaskFixture.Pixels);
        Assert.True(artifact.SupportsExport(VisualArtifactExportFormat.Json));
        Assert.True(artifact.SupportsExport(VisualArtifactExportFormat.Svg));
        Assert.True(artifact.SupportsExport(VisualArtifactExportFormat.Png));
        Assert.Equal(source.Regions.Select(region => region.Label), artifact.Regions.Select(region => region.Label));
        var portable = Assert.Single(envelope.Presentation!.CapturedEffects).Mask!;
        Assert.Equal(mask.ToImage().Pixels, portable.Pixels);
        Assert.Equal(masked.ToRgba(RetainedMaskFixture.Pixels).Pixels,
            envelope.ApplyCapturedEffects(source).ToRgba(RetainedMaskFixture.Pixels).Pixels);
        Assert.Equal(originalJson, source.ToArtifact().ToInterchangeJson());
        Assert.Throws<NotSupportedException>(() => envelope.ToSequenceArtifact());
    }

    [Fact]
    public void DensityBudgetAndCancellationFailuresAreAtomicWhileNativeProfileAvoidsSourceRasterBuffers() {
        var source = RetainedMaskFixture.Plate();
        var masked = source.WithMask(RetainedMaskFixture.Mask(64, 48, 1, VisualMaskMode.Luminance));
        var original = source.ToSvg();
        var prepared = masked.ToSvg();
        RetainedMaskFixture.Capture("retained-mask-failures", source, masked, RetainedMaskFixture.Pixels);
        Assert.Throws<ArgumentNullException>(() => source.WithMask(null!));
        Assert.Throws<ArgumentException>(() => masked.WithMask(RetainedMaskFixture.Mask(64, 48, 2, VisualMaskMode.Alpha)));
        Assert.Throws<ArgumentOutOfRangeException>(() => source.WithMask(new VisualMask(
            new RgbaImage(1, 1, new byte[] { 255, 255, 255, 255 }), pixelsPerUnit: 17)));
        Assert.Throws<ArgumentException>(() => masked.ToPng(new VisualRenderOptions(scale: 2)));
        Assert.Throws<ArgumentOutOfRangeException>(() => masked.ToSvg(new VisualSvgOptions(
            effectRasterOptions: new VisualRenderOptions(pixelBudget: 1))));
        Assert.Contains("<mask", masked.ToSvg(new VisualSvgOptions(effectProfile: VisualSvgEffectProfile.NativeFilters,
            effectRasterOptions: new VisualRenderOptions(pixelBudget: 1))));
        var cancelled = new CancellationToken(true);
        Assert.Throws<OperationCanceledException>(() => masked.ToPng(null, cancelled));
        Assert.Throws<OperationCanceledException>(() => masked.ToSvg(new VisualSvgOptions(effectProfile: VisualSvgEffectProfile.NativeFilters), cancelled));
        Assert.Equal(original, source.ToSvg());
        Assert.Equal(prepared, masked.ToSvg());
    }

    [Fact]
    public void OrderedMasksOwnTheirCompletedSourceAndRetainTheSharedNativeDepthBoundary() {
        var source = RetainedMaskFixture.Plate();
        var mask = RetainedMaskFixture.Mask(64, 48, 1, VisualMaskMode.Alpha);
        var original = source.ToSvg();
        var first = source.WithMask(mask);
        var second = first.WithMask(mask);
        var expected = source.CaptureLayer(mask, RetainedMaskFixture.Pixels).WithMask(mask).ToImage();
        RetainedMaskFixture.Capture("retained-mask-ordered", source, second, RetainedMaskFixture.Pixels);
        Assert.Equal(expected.Pixels, second.ToRgba(RetainedMaskFixture.Pixels).Pixels);
        var nested = source;
        for (var index = 0; index < 65; index++) nested = nested.WithMask(mask);
        var output = Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(output)) File.WriteAllText(Path.Combine(output, "retained-mask-depth-source.svg"), source.ToSvg());
        Assert.Throws<NotSupportedException>(() => nested.ToRgba(RetainedMaskFixture.Pixels));
        Assert.Equal(original, source.ToSvg());
    }

    [Fact]
    public void NativeProjectionFactsAndUnsnapshottedCommonExportCannotSilentlyLoseMaskPresentation() {
        var chart = Chart.Create().WithHeader(false).WithLegend(false)
            .WithXAxisBounds(0, 3).WithYAxisBounds(0, 4)
            .AddSeries(ChartSeries.CreateLine(new ChartSeriesId("series"), "Values", new[] {
                new ChartPoint(0, 1), new ChartPoint(1, 3), new ChartPoint(3, 2)
            }));
        var source = chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(320, 240), 24)));
        var masked = source.WithMask(RetainedMaskFixture.Mask(320, 240, 1, VisualMaskMode.Alpha));
        RetainedMaskFixture.Capture("retained-mask-projection", source, masked, RetainedMaskFixture.Pixels);
        Assert.NotNull(source.XProjection);
        Assert.NotNull(source.YProjection);
        Assert.Same(source.XProjection, masked.XProjection);
        Assert.Same(source.YProjection, masked.YProjection);
        Assert.Equal(source.Regions.Select(region => region.Bounds), masked.Regions.Select(region => region.Bounds));
        var artifact = masked.ToArtifact("masked-numeric", VisualArtifactKind.Chart);
        Assert.True(artifact.SupportsExport(VisualArtifactExportFormat.Json));
        var envelope = artifact.ToInterchangeEnvelope();
        Assert.Equal(VisualArtifactInterchangeFamily.None, envelope.Family);
        Assert.Single(envelope.Presentation!.CapturedEffects);
        Assert.Equal(masked.ToRgba(RetainedMaskFixture.Pixels).Pixels,
            envelope.ApplyCapturedEffects(source).ToRgba(RetainedMaskFixture.Pixels).Pixels);
    }

    [Fact]
    public void ParentClipTranslationAndObjectOpacityApplyAfterTheSameLocalMaskResult() {
        var source = RetainedMaskFixture.Plate();
        var pixels = Enumerable.Repeat((byte)255, 64 * 48 * 4).ToArray();
        for (var index = 3; index < pixels.Length; index += 4) pixels[index] = 128;
        var masked = source.WithMask(new VisualMask(new RgbaImage(64, 48, pixels), orderedDither: false));
        var group = new VisualSceneGroup("parent", "parent", new ChartRect(20, 12, 12, 12), null,
            null, null, null, null, new ChartPoint(4, 6), null, null, .5);
        var nodes = new[] { group }.Cast<VisualSceneNode>().Concat(masked.Scene.Nodes)
            .Concat(new[] { new VisualSceneEndGroup() }).ToArray();
        var parent = new PreparedVisual(new VisualScene(new VisualSize(80, 64), nodes,
            masked.Diagnostics, Array.Empty<VisualSemanticRegion>()));
        var image = parent.ToRgba(RetainedMaskFixture.Pixels);
        var local = masked.ToRgba(RetainedMaskFixture.Pixels);
        var expectedAlpha = (byte)Math.Round(local.Pixels[(17 * local.Width + 25) * 4 + 3] * .5);
        var output = Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(output)) {
            Directory.CreateDirectory(output);
            File.WriteAllText(Path.Combine(output, "retained-mask-parent.svg"), parent.ToSvg());
            File.WriteAllBytes(Path.Combine(output, "retained-mask-parent.png"), parent.ToPng(RetainedMaskFixture.Pixels));
        }
        Assert.Equal(expectedAlpha, image.Pixels[(23 * image.Width + 29) * 4 + 3]);
        Assert.Equal(0, image.Pixels[(23 * image.Width + 37) * 4 + 3]);
        Assert.Equal(0, image.Pixels[(1 * image.Width + 1) * 4 + 3]);
    }

    [Fact]
    public void SharedMaskAndShadowPlanRetainsOriginalWholeCaptureSemantics() {
        var model = RetainedSurfaceFixture.Model(1100, false);
        var source = model.Prepare(RetainedSurfaceFixture.Context(model));
        var mask = RetainedMaskFixture.Mask(1100, 900, 1, VisualMaskMode.Alpha);
        var masked = source.WithMask(mask);
        RetainedMaskFixture.Capture("retained-mask-surface-shadow", source, masked, RetainedMaskFixture.Pixels);
        Assert.Equal(source.CaptureLayer(mask, RetainedMaskFixture.Pixels).ToImage().Pixels,
            masked.ToRgba(RetainedMaskFixture.Pixels).Pixels);
        var retainedCapture = masked.CaptureLayer(mask, RetainedMaskFixture.Pixels);
        Assert.Equal(source.CaptureLayer(mask, RetainedMaskFixture.Pixels).WithMask(mask).ToImage().Pixels,
            retainedCapture.ToImage().Pixels);
        Assert.Same(masked, retainedCapture.Source);
    }
}

internal static class RetainedMaskFixture {
    internal static readonly VisualRenderOptions Pixels = new(supersampling: 1);

    internal static PreparedVisual Plate() {
        var builder = new VisualSceneBuilder(new VisualSize(64, 48), FontSpec.SystemSans());
        builder.Rect(new ChartRect(24, 16, 8, 8), ChartColor.FromRgba(101, 53, 217, 123));
        builder.AddRegion(new VisualSemanticRegion("plate", "composition-part", new ChartRect(24, 16, 8, 8), "Native source"));
        return new PreparedVisual(builder.Build());
    }

    internal static PreparedVisual White(int width, int height) {
        var builder = new VisualSceneBuilder(new VisualSize(width, height), FontSpec.SystemSans());
        builder.Rect(new ChartRect(0, 0, width, height), ChartColor.White);
        return new PreparedVisual(builder.Build());
    }

    internal static SequenceArtifact Sequence(bool dark) {
        var model = SequenceArtifact.Create("mask-sequence").WithSize(800, 600)
            .AddParticipant("A", "Review").AddParticipant("B", "Archive").AddMessage("A", "B", "Save");
        model.Participants[0].Href = "#guide";
        model.ThemeMode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light;
        return model;
    }

    internal static VisualRenderContext Context(SequenceArtifact model) =>
        new(new VisualLayoutOptions(new VisualSize(800, 900), 24), model.Theme, model.ThemeMode);

    internal static VisualMask Mask(int width, int height, int density, VisualMaskMode mode) {
        var pixels = new byte[checked(width * density * height * density * 4)];
        for (var y = 0; y < height * density; y++) {
            for (var x = 0; x < width * density; x++) {
                var at = (y * width * density + x) * 4;
                pixels[at + x % 3] = 255;
                pixels[at + 3] = (byte)(64 + y % 192);
            }
        }
        return new VisualMask(new RgbaImage(width * density, height * density, pixels),
            pixelsPerUnit: density, mode: mode, orderedDither: false);
    }

    internal static RgbaImage DecodeImage(XElement image) {
        var href = image.Attribute("href")!.Value;
        return RasterImageDecoder.Decode(Convert.FromBase64String(href.Substring(href.IndexOf(',') + 1)));
    }

    internal static void Capture(string name, PreparedVisual source, PreparedVisual masked, VisualRenderOptions options) {
        var output = Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(output)) return;
        Directory.CreateDirectory(output);
        var stem = Path.Combine(output, name);
        File.WriteAllText(stem + ".source.svg", source.ToSvg());
        File.WriteAllBytes(stem + ".source.png", source.ToPng(options));
        File.WriteAllText(stem + ".baked.svg", masked.ToSvg(new VisualSvgOptions(effectRasterOptions: options)));
        File.WriteAllText(stem + ".native.svg", masked.ToSvg(new VisualSvgOptions(effectProfile: VisualSvgEffectProfile.NativeFilters)));
        File.WriteAllBytes(stem + ".native.png", masked.ToPng(options));
        File.WriteAllText(stem + ".regions.txt", string.Join("\n", masked.Regions.Select(region => region.Id + ":" + region.Role + ":" + region.Label)));
        if (source.SemanticInterchange != null) File.WriteAllText(stem + ".source.json", source.ToArtifact().ToInterchangeJson());
    }
}
