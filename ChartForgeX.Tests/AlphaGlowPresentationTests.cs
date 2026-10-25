using System;
using System.Linq;
using System.Xml.Linq;
using ChartForgeX.Effects;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class AlphaGlowPresentationTests {
    [Fact]
    public void RootGlowAndMaskKeepActualNonlinearOrderAndOneOriginalSource() {
        var source = AlphaGlowFixture.Source(ChartForgeX.Primitives.ChartColor.FromRgba(255, 0, 0, 128));
        var sourceSvg = source.ToSvg();
        var glow = new VisualDropShadow(ChartForgeX.Primitives.ChartColor.FromRgb(0, 0, 255), 0, 0, 1,
            spread: 1, orderedDither: false, opacity: .5);
        var mask = new VisualMask(AppearanceFixture.Solid(16, 16, 128), orderedDither: false);
        var glowThenMask = source.WithDropShadow(glow).WithMask(mask);
        var maskThenGlow = source.WithMask(mask).WithDropShadow(glow);
        var after = glowThenMask.ToRgba(AlphaGlowFixture.Unit);
        var before = maskThenGlow.ToRgba(AlphaGlowFixture.Unit);
        NativeEffectBeforeCapture.Write("alpha-glow-order-glow-mask", glowThenMask.ToSvg(), glowThenMask.ToPng(AlphaGlowFixture.Unit), after);
        NativeEffectBeforeCapture.Write("alpha-glow-order-mask-glow", maskThenGlow.ToSvg(), maskThenGlow.ToPng(AlphaGlowFixture.Unit), before);
        Assert.Equal(80, AppearanceFixture.Pixel(after, 8, 8)[3]);
        Assert.Equal(88, AppearanceFixture.Pixel(before, 8, 8)[3]);
        Assert.NotEqual(after.Pixels, before.Pixels);
        Assert.Equal(source.Regions.Select(item => (item.Id, item.Role)), glowThenMask.Regions.Select(item => (item.Id, item.Role)));
        Assert.Equal(sourceSvg, source.ToSvg());
    }

    [Theory]
    [InlineData("compact", VisualThemeMode.Light)]
    [InlineData("compact", VisualThemeMode.Dark)]
    [InlineData("wide", VisualThemeMode.Light)]
    [InlineData("wide", VisualThemeMode.Dark)]
    public void OrdinaryGlowRecipeReplaysTheExistingTypedShadowAndKeepsNativeVectorsSeparate(string variant, VisualThemeMode mode) {
        var layer = NativeAppearanceExample.Create("glow", variant, mode);
        var policy = NativeAppearanceExample.AlphaGlowPolicy(mode);
        var fresh = NativeAppearanceExample.PrepareSource(variant, mode);
        var direct = fresh.CaptureLayer(policy, AlphaGlowFixture.Unit);
        var retained = fresh.WithDropShadow(policy);
        var nativeSvg = retained.ToSvg(new VisualSvgOptions(effectProfile: VisualSvgEffectProfile.NativeFilters));
        var filterSource = NativeAppearanceExample.AlphaGlowFilterSource(mode);
        var imported = ChartForgeX.SvgRaster.SvgRasterizer.Rasterize(filterSource, strict: true,
            filters: new ChartForgeX.SvgRaster.SvgRasterFilterOptions());
        var filterBuilder = new VisualSceneBuilder(new VisualSize(64, 48), ChartForgeX.Typography.FontSpec.SystemSans());
        filterBuilder.Rect(new ChartForgeX.Primitives.ChartRect(24, 16, 8, 8), ChartForgeX.Primitives.ChartColor.FromRgba(16, 16, 16, 128));
        var filterCapture = new PreparedVisual(filterBuilder.Build()).CaptureLayer(policy, AlphaGlowFixture.Unit);
        NativeEffectBeforeCapture.Write("alpha-glow-filter-" + variant + "-" + mode, filterSource,
            ChartForgeX.Raster.PngWriter.WriteRgba(imported.Image), imported.Image);
        AppearanceFixture.Capture("alpha-glow-filter-native-" + variant + "-" + mode, filterCapture);
        var json = layer.ToArtifact().ToInterchangeJson();
        var envelope = VisualArtifactInterchangeEnvelope.FromJson(json);
        var replayed = envelope.ApplyCapturedLayer(fresh);
        AppearanceFixture.Capture("alpha-glow-recipe-" + variant + "-" + mode, layer);
        AppearanceFixture.Capture("alpha-glow-replayed-" + variant + "-" + mode, replayed);
        NativeEffectBeforeCapture.Json("alpha-glow-recipe-" + variant + "-" + mode + "-facts", new { json, nativeSvg });
        var presentation = Assert.IsType<VisualArtifactInterchangePresentation>(envelope.Presentation);
        var step = Assert.Single(presentation.CapturedEffects);
        Assert.NotNull(step.DropShadow);
        Assert.Null(step.Bloom);
        Assert.Null(step.Outline);
        Assert.Equal(0, policy.OffsetX);
        Assert.Equal(0, policy.OffsetY);
        Assert.Equal(3, policy.SigmaX);
        Assert.Equal(3, policy.SigmaY);
        Assert.Equal(1, policy.Spread);
        Assert.Equal(.6, policy.Opacity);
        Assert.Null(policy.RelativeRegion);
        Assert.Empty(imported.Diagnostics);
        var filterPixels = filterCapture.ToImage();
        var cropped = new byte[64 * 48 * 4];
        for (var y = 0; y < 48; y++) Buffer.BlockCopy(filterPixels.Pixels,
            ((y - (int)filterCapture.Bounds.Y) * filterPixels.Width - (int)filterCapture.Bounds.X) * 4,
            cropped, y * 64 * 4, 64 * 4);
        Assert.Equal(cropped, imported.Image.Pixels);
        Assert.Equal(layer.ToImage().Pixels, direct.ToImage().Pixels);
        Assert.Equal(layer.ToImage().Pixels, replayed.ToImage().Pixels);
        Assert.Equal(layer.Bounds, replayed.Bounds);
        Assert.True(layer.Bounds.X < 0 && layer.Bounds.Width > fresh.Size.Width);
        var document = XDocument.Parse(nativeSvg);
        Assert.Single(document.Descendants(), item => item.Name.LocalName == "filter");
        Assert.DoesNotContain(document.Descendants(), item => item.Name.LocalName == "image");
        Assert.Equal(XDocument.Parse(fresh.ToSvg()).Descendants().Count(item => item.Name.LocalName == "text"),
            document.Descendants().Count(item => item.Name.LocalName == "text"));
        var program = ExampleRecipeSource.EmitNativeAppearance("glow", variant, mode);
        Assert.Contains("VisualDropShadow", program);
        Assert.Contains("ApplyCapturedLayer", program);
        Assert.DoesNotContain("WithGlow", program);
        Assert.DoesNotContain("{{", program);
    }
}
