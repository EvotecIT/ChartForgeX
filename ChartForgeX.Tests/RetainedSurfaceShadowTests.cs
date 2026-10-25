using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
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
public sealed class RetainedSurfaceShadowTests {
    [Theory]
    [InlineData(390, false)] [InlineData(1100, false)]
    [InlineData(390, true)] [InlineData(1100, true)]
    public void SurfaceEffectsKeepSingleVectorCaptionsLinksAndDetachedPortableFacts(int width, bool dark) {
        var model = RetainedSurfaceFixture.Model(width, dark);
        var context = RetainedSurfaceFixture.Context(model);
        var prepared = model.Prepare(context);
        var baked = prepared.ToSvg(new VisualSvgOptions(idPrefix: "retained-fixed"));
        var native = prepared.ToSvg(new VisualSvgOptions(idPrefix: "retained-fixed",
            effectProfile: VisualSvgEffectProfile.NativeFilters));
        var envelope = prepared.ToArtifact(model.Id, VisualArtifactKind.Sequence).ToInterchangeEnvelope();
        var json = envelope.ToJson();
        RetainedSurfaceFixture.Capture("retained-surfaces-" + width + "-" + dark, prepared);
        Assert.Equal(2, envelope.Version);
        Assert.Equal(.06, envelope.Nodes[1].Sequence!.SurfaceShadow!.Opacity);
        Assert.NotNull(envelope.Sequence!.NoteShadow);
        Assert.NotNull(envelope.Sequence.FragmentTabShadow);
        Assert.NotNull(envelope.Sequence.BackgroundShadow);
        var bakedXml = XDocument.Parse(baked);
        var nativeXml = XDocument.Parse(native);
        Assert.Contains(bakedXml.Descendants(), element => element.Name.LocalName == "image");
        Assert.DoesNotContain(nativeXml.Descendants(), element => element.Name.LocalName == "image");
        Assert.DoesNotContain(nativeXml.Descendants(), element => element.Name.LocalName == "use");
        Assert.Contains(nativeXml.Descendants(), element => element.Name.LocalName == "feMerge");
        Assert.Contains(nativeXml.Descendants(), element => element.Name.LocalName == "feFlood" &&
            (string?)element.Attribute("flood-opacity") == "0.06");
        foreach (var role in new[] { "sequence-participant-label", "sequence-note" }) {
            var first = bakedXml.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == role).ToArray();
            var second = nativeXml.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == role).ToArray();
            Assert.NotEmpty(first);
            Assert.Equal(first.Length, second.Length);
        }
        Assert.Single(nativeXml.Descendants(), element => element.Name.LocalName == "a" &&
            (string?)element.Attribute("href") == "#guide");
        Assert.Equal(bakedXml.Descendants().Count(element => element.Name.LocalName == "text"),
            nativeXml.Descendants().Count(element => element.Name.LocalName == "text"));
        var restored = VisualArtifactInterchangeEnvelope.FromJson(json).ToSequenceArtifact();
        var replay = restored.Prepare(context);
        Assert.Equal(prepared.ToSvg("retained-replay"), replay.ToSvg("retained-replay"));
        Assert.Equal(prepared.ToPng(RetainedSurfaceFixture.Pixels), replay.ToPng(RetainedSurfaceFixture.Pixels));
        model.Participants[1].SurfaceShadow = null;
        model.Paint.NoteShadow = null;
        model.Participants[0].Label = "Changed producer";
        envelope.Nodes[1].Sequence!.SurfaceShadow = null;
        Assert.Equal(baked, prepared.ToSvg(new VisualSvgOptions(idPrefix: "retained-fixed")));
        Assert.Equal(json, prepared.ToArtifact(model.Id, VisualArtifactKind.Sequence).ToInterchangeJson());
    }

    [Theory]
    [InlineData(1)] [InlineData(2)] [InlineData(3)]
    public void ShadowOnlyPixelsPreserveSourceOnceAndFractionalOpacityBeforeFinalByteConversion(int density) {
        var source = RetainedSurfaceFixture.Plate(alpha: 25, shadow: true);
        var options = new VisualRenderOptions(density, 1);
        var image = source.ToRgba(options);
        RetainedSurfaceFixture.Capture("surface-alpha-" + density, source, options);
        Assert.Equal(new byte[] { 255, 0, 0, 25 }, RetainedSurfaceFixture.Pixel(image, 10 * density, 6 * density));
        Assert.Equal(new byte[] { 0, 0, 255, 2 }, RetainedSurfaceFixture.Pixel(image, 18 * density, 8 * density));
        var roundedTint = RetainedSurfaceFixture.Plate(alpha: 25, shadow: true, roundedTint: true).ToRgba(options);
        Assert.Equal(1, RetainedSurfaceFixture.Pixel(roundedTint, 18 * density, 8 * density)[3]);
    }

    [Theory]
    [InlineData(1d)] [InlineData(.5)]
    public void ExistingIsolationClipsAndRigidTransformsApplyToCompletedShadowAndSourceOnce(double opacity) {
        var ordinary = RetainedSurfaceFixture.Plate(128, true);
        var isolated = RetainedSurfaceFixture.Plate(128, true, opacity: opacity, clipped: true);
        var pixels = isolated.ToRgba(RetainedSurfaceFixture.Pixels);
        RetainedSurfaceFixture.Capture("surface-isolation-" + opacity, isolated);
        Assert.Equal((byte)Math.Round(128 * opacity), RetainedSurfaceFixture.Pixel(pixels, 10, 6)[3]);
        Assert.Equal((byte)Math.Round(8 * opacity), RetainedSurfaceFixture.Pixel(pixels, 18, 8)[3]);
        Assert.Equal(new byte[4], RetainedSurfaceFixture.Pixel(pixels, 22, 8));
        if (opacity == 1) Assert.Equal(ordinary.ToRgba(RetainedSurfaceFixture.Pixels).Pixels, pixels.Pixels);
        var rotated = RetainedSurfaceFixture.Plate(128, true, rotated: true).ToRgba(RetainedSurfaceFixture.Pixels);
        // The local +4/+2 shadow offset rotates with the same native surface, not screen axes.
        Assert.Equal(new byte[] { 0, 0, 255, 8 }, RetainedSurfaceFixture.Pixel(rotated, 23, 18));
    }

    [Fact]
    public void EmptyProfilesZeroDescriptorsAndWholeChildCaptureRetainOrdinaryBytes() {
        var source = RetainedSurfaceFixture.Plate(128, false);
        var ordinary = source.ToSvg();
        Assert.Equal(ordinary, source.ToSvg(new VisualSvgOptions()));
        Assert.Equal(ordinary, source.ToSvg(new VisualSvgOptions(effectProfile: VisualSvgEffectProfile.NativeFilters)));
        Assert.Equal(ordinary, RetainedSurfaceFixture.Plate(128, true, zero: true).ToSvg());
        var publicSource = ShadowFilterFixture.Source();
        var old = publicSource.CaptureLayer(new VisualDropShadow(ChartColor.FromRgb(0, 0, 255), 4, 2, 0,
            orderedDither: false), RetainedSurfaceFixture.Pixels);
        var same = publicSource.CaptureLayer(new VisualDropShadow(ChartColor.FromRgb(0, 0, 255), 4, 2, 0,
            orderedDither: false, opacity: 1), RetainedSurfaceFixture.Pixels);
        Assert.Equal(old.Bounds, same.Bounds);
        Assert.Equal(old.ToImage().Pixels, same.ToImage().Pixels);
        var clipped = publicSource.CaptureLayer(new VisualDropShadow(ChartColor.FromRgb(0, 0, 255), 4, 2, 0,
            orderedDither: false, relativeRegion: new ChartRect(10, 10, 1, 1)), RetainedSurfaceFixture.Pixels);
        Assert.Equal(publicSource.ToRgba(RetainedSurfaceFixture.Pixels).Pixels, clipped.ToImage().Pixels);
        Assert.Equal(new ChartRect(0, 0, 64, 48), clipped.Bounds);
    }

    [Fact]
    public void PortableShadowRejectsDowngradeUnknownFieldsAndNonintegralOffsetsBeforeRestoration() {
        var model = RetainedSurfaceFixture.Model(1100, false);
        var prepared = model.Prepare(RetainedSurfaceFixture.Context(model));
        var json = prepared.ToArtifact(model.Id, VisualArtifactKind.Sequence).ToInterchangeJson();
        RetainedSurfaceFixture.Capture("surface-portable-validation", prepared);
        Assert.Contains("\"version\":2", json);
        Assert.Contains("\"offsetX\":4", json);
        var fractionalOpacity = "\"opacity\":" + .06.ToString("G17", System.Globalization.CultureInfo.InvariantCulture);
        Assert.Contains(fractionalOpacity, json);
        Assert.Contains(VisualArtifactInterchangeEnvelope.FromJson(json).Nodes,
            node => node.Sequence?.SurfaceShadow?.Opacity == .06);
        Assert.Throws<NotSupportedException>(() => VisualArtifactInterchangeEnvelope.FromJson(json.Replace("\"version\":2", "\"version\":1")));
        Assert.Throws<ArgumentException>(() => VisualArtifactInterchangeEnvelope.FromJson(json.Replace("\"offsetX\":4", "\"offsetX\":4.5")));
        Assert.Throws<ArgumentOutOfRangeException>(() => VisualArtifactInterchangeEnvelope.FromJson(json.Replace(fractionalOpacity, "\"opacity\":2")));
        Assert.Throws<ArgumentException>(() => VisualArtifactInterchangeEnvelope.FromJson(json.Replace(fractionalOpacity, "\"unknownEffect\":true," + fractionalOpacity)));
        Assert.Equal(json, prepared.ToArtifact(model.Id, VisualArtifactKind.Sequence).ToInterchangeJson());
    }

    [Fact]
    public void ExportCancellationAndDensityCapsAreAtomicAndNativeFiltersDoNotBakePixelBuffers() {
        var prepared = RetainedSurfaceFixture.Model(1100, false).Prepare(
            RetainedSurfaceFixture.Context(RetainedSurfaceFixture.Model(1100, false)));
        var original = prepared.ToSvg();
        RetainedSurfaceFixture.Capture("surface-export-failures", prepared);
        var cancelled = new CancellationToken(true);
        Assert.Throws<OperationCanceledException>(() => prepared.ToSvg(new VisualSvgOptions(), cancelled));
        Assert.Throws<OperationCanceledException>(() => prepared.ToPng(null, cancelled));
        Assert.Throws<ArgumentOutOfRangeException>(() => prepared.ToSvg(new VisualSvgOptions(
            effectRasterOptions: new VisualRenderOptions(pixelBudget: 1)), CancellationToken.None));
        Assert.Contains("<filter", prepared.ToSvg(new VisualSvgOptions(
            effectProfile: VisualSvgEffectProfile.NativeFilters,
            effectRasterOptions: new VisualRenderOptions(pixelBudget: 1)), CancellationToken.None));
        Assert.Equal(original, prepared.ToSvg());
        foreach (var value in new[] { -.1, 1.1, double.NaN, double.PositiveInfinity }) {
            Assert.Throws<ArgumentOutOfRangeException>(() => new VisualDropShadow(ChartColor.Black, 0, 0, 0, opacity: value));
        }
        Assert.Throws<ArgumentOutOfRangeException>(() => new VisualSvgOptions(effectProfile: (VisualSvgEffectProfile)99));
    }
}

internal static class RetainedSurfaceFixture {
    internal static readonly VisualRenderOptions Pixels = new(supersampling: 1);

    internal static SequenceArtifact Model(int width, bool dark) {
        var sequence = SequenceArtifact.Create("retained-surfaces").WithSize(width, 900)
            .AddParticipant("A", "Review", SequenceArtifactParticipantKind.Actor)
            .AddParticipant("B", "Archive", SequenceArtifactParticipantKind.Control)
            .AddMessage("A", "B", "Save").AddMessage("B", "A", "Stored");
        sequence.ThemeMode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light;
        sequence.Layout = new SequenceLayoutPolicy(participantBands: new SequenceParticipantBands(),
            participantMinimumWidth: width == 390 ? 64 : 150, participantSpacing: width == 390 ? 8 : 50,
            fragmentTabMinimumWidth: 50, fragmentTabMinimumHeight: 35);
        sequence.Participants[0].Href = "#guide";
        sequence.Participants[1].SurfaceShadow = new VisualDropShadow(dark ? ChartColor.White : ChartColor.Black,
            4, 4, 0, orderedDither: false, opacity: .06, relativeRegion: new ChartRect(-.1, -.1, 1.3, 1.3));
        var style = new VisualDropShadow(ChartColor.Black, 1, 2, 2, opacity: .25);
        sequence.Paint.NoteShadow = style;
        sequence.Paint.FragmentTabShadow = style;
        sequence.Paint.BackgroundShadow = style;
        sequence.AddNote(SequenceArtifactNotePlacement.Over, new[] { "A", "B" }, "Retain facts");
        sequence.Notes[0].StepIndex = 1;
        sequence.AddBlock(SequenceArtifactBlockKind.Loop, "Audit", 0, 1);
        sequence.AddBlock(SequenceArtifactBlockKind.Rect, "", 0, 1);
        sequence.Blocks[1].BackgroundColor = ChartColor.FromRgba(150, 160, 170, 30);
        return sequence;
    }

    internal static VisualRenderContext Context(SequenceArtifact model) =>
        new(new VisualLayoutOptions(new VisualSize(model.Width, model.Height), 24), model.Theme, model.ThemeMode);

    // This tiny canonical scene exercises byte alpha and isolation without font/background
    // ambiguity. The public Sequence workflow above establishes actual authored surface adoption.
    internal static PreparedVisual Plate(byte alpha, bool shadow, bool roundedTint = false,
        double opacity = 1, bool clipped = false, bool rotated = false, bool zero = false) {
        var builder = new VisualSceneBuilder(new VisualSize(32, 32), FontSpec.SystemSans());
        using (builder.PushGroup("plate", "surface-plate", opacity: opacity)) {
            using var clip = clipped ? builder.PushClip(new ChartRect(0, 0, 21, 32)) : null;
            using var rotation = rotated ? builder.PushRotation(90, 16, 16) : null;
            var bounds = new ChartRect(8, 4, 8, 8);
            var effect = !shadow ? null : new VisualDropShadow(roundedTint ? ChartColor.FromRgba(0, 0, 255, 15) : ChartColor.FromRgb(0, 0, 255),
                4, 2, 0, orderedDither: false, opacity: zero ? 0 : roundedTint ? 1 : .06);
            using (builder.PushSurfaceShadow(effect, bounds, bounds)) builder.Rect(bounds, ChartColor.FromRgba(255, 0, 0, alpha));
        }
        builder.AddRegion(new VisualSemanticRegion("plate", "surface-plate", new ChartRect(8, 4, 8, 8)));
        return new PreparedVisual(builder.Build());
    }

    internal static byte[] Pixel(RgbaImage image, int x, int y) =>
        image.Pixels.Skip((y * image.Width + x) * 4).Take(4).ToArray();

    internal static void Capture(string name, PreparedVisual prepared, VisualRenderOptions? raster = null) {
        var output = Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(output)) return;
        Directory.CreateDirectory(output);
        var path = Path.Combine(output, name);
        File.WriteAllText(path + ".baked.svg", prepared.ToSvg(new VisualSvgOptions(idPrefix: "retained-proof")));
        File.WriteAllText(path + ".native-filter.svg", prepared.ToSvg(new VisualSvgOptions(idPrefix: "retained-proof",
            effectProfile: VisualSvgEffectProfile.NativeFilters)));
        File.WriteAllBytes(path + ".png", prepared.ToPng(raster ?? Pixels));
        if (prepared.Regions.Any(region => region.Role == "sequence-participant")) {
            File.WriteAllText(path + ".json", prepared.ToArtifact("retained-surfaces", VisualArtifactKind.Sequence).ToInterchangeJson());
        }
    }
}
