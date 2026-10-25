using System;
using System.IO;
using ChartForgeX.Effects;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Current-API controls for the existing zero-offset alpha-shadow recipe; no future Glow API is referenced.</summary>
public sealed class AlphaGlowBeforeControls {
    [Fact]
    public void SoftAlphaHaloExistsForDarkSourceWithoutBrightPassOrHardOutlineSubstitution() {
        var source = AlphaGlowFixture.Source(ChartColor.FromRgb(16, 16, 16));
        var glow = source.CaptureLayer(new VisualDropShadow(ChartColor.FromRgb(0, 0, 255), 0, 0, 1,
            spread: 1, orderedDither: false), AlphaGlowFixture.Unit);
        var outline = source.CaptureLayer(new VisualOutline(ChartColor.FromRgb(0, 0, 255), 1,
            orderedDither: false), AlphaGlowFixture.Unit);
        var bloom = source.CaptureLayer(new VisualBloom(1, threshold: .9, strength: 1,
            orderedDither: false), AlphaGlowFixture.Unit);
        AppearanceFixture.Capture("alpha-glow-before-dark", glow);
        AppearanceFixture.Capture("alpha-glow-before-outline-control", outline);
        AppearanceFixture.Capture("alpha-glow-before-bloom-control", bloom);
        Assert.Equal(new byte[] { 0, 0, 255, 85 }, AppearanceFixture.WorldPixel(glow, 2, 8));
        Assert.Equal(new byte[4], AppearanceFixture.WorldPixel(outline, 2, 8));
        Assert.Equal(new byte[4], AppearanceFixture.WorldPixel(bloom, 2, 8));
        Assert.Equal(new byte[] { 16, 16, 16, 255 }, AppearanceFixture.WorldPixel(glow, 8, 8));
        Assert.Same(source, glow.Source);
        Assert.Equal(new ChartRect(-2, -2, 20, 20), glow.Bounds);
    }

    [Fact]
    public void SemitransparentSourceEntersOnceOverItsBlurredAlphaTint() {
        var source = AlphaGlowFixture.Source(ChartColor.FromRgba(255, 0, 0, 128));
        var before = source.ToSvg();
        var glow = source.CaptureLayer(new VisualDropShadow(ChartColor.FromRgb(0, 0, 255), 0, 0, 1,
            spread: 1, orderedDither: false, opacity: .5), AlphaGlowFixture.Unit);
        AppearanceFixture.Capture("alpha-glow-before-half-alpha", glow);
        Assert.Equal(new byte[] { 204, 0, 51, 160 }, AppearanceFixture.WorldPixel(glow, 8, 8));
        Assert.Equal(new byte[] { 255, 0, 0, 128 }, AppearanceFixture.Pixel(source.ToRgba(AlphaGlowFixture.Unit), 8, 8));
        Assert.Equal(before, source.ToSvg());
        Assert.Equal("glow-source", Assert.Single(glow.Visual.Regions).Id);
    }

    [Fact]
    public void FractionalTintOpacityIsKeptUntilFinalCoverageConversion() {
        var source = AlphaGlowFixture.Source(ChartColor.FromRgba(255, 0, 0, 25));
        var tint = ChartColor.FromRgb(11, 29, 248);
        var raw = source.CaptureLayer(new VisualDropShadow(tint, 0, 0, 0,
            spread: 1, orderedDither: false, opacity: .06), AlphaGlowFixture.Unit);
        var quantizedTint = source.CaptureLayer(new VisualDropShadow(tint.WithAlpha(15), 0, 0, 0,
            spread: 1, orderedDither: false), AlphaGlowFixture.Unit);
        AppearanceFixture.Capture("alpha-glow-before-raw-opacity", raw);
        AppearanceFixture.Capture("alpha-glow-before-byte-tint-control", quantizedTint);
        Assert.Equal(new byte[] { 11, 29, 248, 2 }, AppearanceFixture.WorldPixel(raw, 3, 8));
        Assert.Equal(1, AppearanceFixture.WorldPixel(quantizedTint, 3, 8)[3]);
        Assert.NotEqual(raw.ToImage().Pixels, quantizedTint.ToImage().Pixels);
    }

    [Fact]
    public void ZeroTintAndOpacityPreserveOrdinaryBytesButZeroGeometryIsNotAGeneralIdentity() {
        var source = AlphaGlowFixture.Source(ChartColor.FromRgba(255, 0, 0, 128));
        var ordinary = source.CaptureLayer(new VisualLayerBlur(0), AlphaGlowFixture.Unit);
        var noOpacity = source.CaptureLayer(new VisualDropShadow(ChartColor.FromRgb(0, 0, 255), 0, 0, 1,
            spread: 1, orderedDither: false, opacity: 0), AlphaGlowFixture.Unit);
        var noTint = source.CaptureLayer(new VisualDropShadow(ChartColor.Transparent, 0, 0, 1,
            spread: 1, orderedDither: false), AlphaGlowFixture.Unit);
        var zeroGeometry = source.CaptureLayer(new VisualDropShadow(ChartColor.FromRgb(0, 0, 255), 0, 0, 0,
            orderedDither: false), AlphaGlowFixture.Unit);
        AppearanceFixture.Capture("alpha-glow-before-ordinary", ordinary);
        AppearanceFixture.Capture("alpha-glow-before-zero-opacity", noOpacity);
        AppearanceFixture.Capture("alpha-glow-before-zero-tint", noTint);
        AppearanceFixture.Capture("alpha-glow-before-zero-geometry", zeroGeometry);
        Assert.Equal(ordinary.ToImage().Pixels, noOpacity.ToImage().Pixels);
        Assert.Equal(ordinary.ToImage().Pixels, noTint.ToImage().Pixels);
        Assert.Equal(ordinary.Bounds, noOpacity.Bounds);
        Assert.Equal(ordinary.Bounds, noTint.Bounds);
        Assert.Equal(new byte[] { 170, 0, 85, 192 }, AppearanceFixture.WorldPixel(zeroGeometry, 8, 8));
    }
}

internal static class AlphaGlowFixture {
    internal static readonly VisualRenderOptions Unit = new(supersampling: 1);
    internal static PreparedVisual Source(ChartColor color) {
        var builder = new VisualSceneBuilder(new VisualSize(16, 16), FontSpec.SystemSans());
        builder.Rect(new ChartRect(4, 4, 8, 8), color);
        builder.AddRegion(new VisualSemanticRegion("glow-source", "sample", new ChartRect(4, 4, 8, 8), "Original source"));
        return new PreparedVisual(builder.Build());
    }
}
