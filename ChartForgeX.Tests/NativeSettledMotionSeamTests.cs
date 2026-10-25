using ChartForgeX.Core;
using ChartForgeX.Effects;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class NativeSettledMotionSeamTests {
    [Theory, InlineData(VisualSvgEffectProfile.Baked), InlineData(VisualSvgEffectProfile.NativeFilters)]
    public void SameEmissionAndInitialFramesRetainTheExactEffectExportPolicyAndNativeEndpoint(VisualSvgEffectProfile profile) {
        var section = NativeSettledMotionFixture.Section(760, false, profile); var prepared = section.PreparedVisual.ForInteractivePngExport();
        var emission = prepared.ExportSvgEmission(section.SvgOptions); var frames = VisualValueCapture.InitialFadeFrames(prepared, 3);
        for (var index = 0; index < frames.Count; index++) NativeSeriesVisibilityDefaultControls.Save("settled-effects-" + profile + "-" + index, frames[index], index == 2 ? section.Html : null,
            new { profile, index, emission.Prefix, emission.CapturePngText });
        Assert.Equal(profile, emission.Options.EffectProfile); Assert.Same(section.SvgOptions.EffectRasterOptions, emission.Options.EffectRasterOptions);
        Assert.Equal(emission.Svg, prepared.ToSvg(emission.Options)); Assert.Equal(section.WideEmission!.Svg, emission.Svg);
        Assert.Equal(prepared.ToSvg(), frames[2].ToSvg()); Assert.Equal(prepared.ToPng(), frames[2].ToPng());
        if (profile == VisualSvgEffectProfile.NativeFilters) {
            Assert.Contains("-surface-shadow-", emission.Svg, StringComparison.Ordinal); Assert.Contains("-retained-mask-0", emission.Svg, StringComparison.Ordinal);
            Assert.Contains("-retained-mask-1", frames[1].ToSvg(), StringComparison.Ordinal);
        } else { Assert.DoesNotContain("<filter", emission.Svg, StringComparison.Ordinal); Assert.DoesNotContain("<mask", emission.Svg, StringComparison.Ordinal); }
    }
    [Fact]
    public void TokenAwareSvgExportStillCancelsThroughTheSingleWriterBody() {
        var visual = NativeSettledMotionFixture.Section(390, true, VisualSvgEffectProfile.NativeFilters).PreparedVisual;
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        NativeSeriesVisibilityDefaultControls.Save("settled-effects-cancellation-control", visual);
        Assert.Throws<OperationCanceledException>(() => VisualSceneSvgRenderer.Render(visual.Scene, "Captured", null, null, false,
            "settled-cancel", new VisualSvgOptions(effectProfile: VisualSvgEffectProfile.NativeFilters), false, cancellation.Token));
    }
    [Fact]
    public void VisibilityCopiesTheSameImmutableShadowAndMaskOnRealNativeSeriesGroups() {
        var source = NativeSettledMotionFixture.Drawing(760, false, VisualSvgEffectProfile.NativeFilters, maskInPlotScope: true);
        var original = source.Scene.Nodes.OfType<VisualSceneGroup>().Single(group => group.Role == "series" && group.Metadata["data-cfx-series-key"] == "observed");
        var from = VisualSeriesVisibilityCapture.Prepare(source, new ChartVisibilityView(), false);
        var to = VisualSeriesVisibilityCapture.Prepare(source, new ChartVisibilityView(new[] { new ChartSeriesId("observed") }), false);
        var middle = VisualSeriesVisibilityCapture.Frames(from, to, 3)[1];
        NativeSeriesVisibilityDefaultControls.Save("settled-visibility-effects-from", from); NativeSeriesVisibilityDefaultControls.Save("settled-visibility-effects-mid", middle);
        NativeSeriesVisibilityDefaultControls.Save("settled-visibility-effects-to", to);
        var copied = middle.Scene.Nodes.OfType<VisualSceneGroup>().Single(group => group.Id == original.Id);
        Assert.Same(original.SurfaceShadow, copied.SurfaceShadow);
        Assert.Same(source.Scene.Nodes.OfType<VisualSceneGroup>().Single(group => group.RetainedEffect != null).RetainedEffect,
            middle.Scene.Nodes.OfType<VisualSceneGroup>().Single(group => group.RetainedEffect != null).RetainedEffect);
        Assert.Contains(middle.Scene.Nodes.OfType<VisualSceneGroup>(), group => group.Opacity == .5); Assert.Equal(original.Opacity, copied.Opacity);
        Assert.Equal(source.ToSvg(), from.ToSvg());
        Assert.NotEqual(from.ToPng(), to.ToPng());
    }
}

/// <summary>Real native Chart scenes/effects in the existing test host; the internal placement is a compositor/adapter seam fixture, not a public Chart shadow policy.</summary>
internal static class NativeSettledMotionFixture {
    internal static PreparedVisual Drawing(int width, bool dark, VisualSvgEffectProfile profile, bool maskInPlotScope = false) {
        var chart = NativeSeriesVisibilityDefaultControls.Model("bar", width, dark).WithDataLabels(false);
        var basis = chart.Prepare(VisualExportRequest.ForChart(chart).Context); var scene = basis.Scene;
        var plot = basis.Regions.Single(region => region.Role == "cartesian-plot").Bounds;
        var shadow = new VisualSceneSurfaceShadow(new VisualDropShadow(ChartColor.Black, 4, 4, 0, opacity: .06), plot, plot);
        var widthPixels = checked((int)scene.Size.Width); var heightPixels = checked((int)scene.Size.Height);
        var pixels = new byte[checked(widthPixels * heightPixels * 4)]; for (var index = 3; index < pixels.Length; index += 4) pixels[index] = 255;
        var mask = new VisualMask(new RgbaImage(widthPixels, heightPixels, pixels), orderedDither: false);
        var retained = maskInPlotScope ? new VisualSceneRetainedEffect(VisualCapturedEffect.From(mask), scene.Size) : null;
        var seriesIndex = Enumerable.Range(0, scene.Nodes.Count).Single(index => scene.Nodes[index] is VisualSceneGroup group && group.Role == "series" && group.Metadata["data-cfx-series-key"] == "observed");
        var maskIndex = new VisualSeriesVisibilityCapture.SpanTree(scene.Nodes).Parent[seriesIndex];
        if (maskInPlotScope && maskIndex < 0) throw new InvalidOperationException("The native series has no retained plot scope for this nested-mask seam fixture.");
        var nodes = scene.Nodes.Select((node, index) => node is VisualSceneGroup group && (index == seriesIndex || maskInPlotScope && index == maskIndex)
            ? (VisualSceneNode)new VisualSceneGroup(group.Role, group.Id, group.Clip, group.Metadata, group.PathClip, group.Rotation, group.Href, group.Tooltip,
                group.Translation, group.ImageResource, group.ColorScope, group.Opacity, index == seriesIndex ? shadow : group.SurfaceShadow,
                maskInPlotScope && index == maskIndex ? retained : group.RetainedEffect) : node).ToArray();
        var options = new VisualSvgOptions(effectProfile: profile, effectRasterOptions: new VisualRenderOptions(supersampling: 1));
        var drawing = basis.WithScene(new VisualScene(scene.Size, nodes, scene.Diagnostics, scene.Regions, scene.DataProjection, scene.ValueCapture), options);
        return maskInPlotScope ? drawing : drawing.WithMask(mask);
    }
    internal static HtmlChartSection Section(int width, bool dark, VisualSvgEffectProfile profile) {
        var chart = NativeSeriesVisibilityDefaultControls.Model("bar", width, dark).WithDataLabels(false);
        var ordinary = new HtmlInteractiveChartRenderer().PrepareSection(chart, configure: options => {
            options.Interaction.ChartId = "settled-native-effects"; options.Interaction.Enable(ChartInteractionFeatures.Export);
        });
        var drawing = Drawing(width, dark, profile);
        var options = new VisualSvgOptions(ordinary.WideEmission!.Prefix, effectProfile: profile, effectRasterOptions: new VisualRenderOptions(supersampling: 1));
        var emission = drawing.ForInteractivePngExport().ExportSvgEmission(options);
        var html = ordinary.Html.Replace(ordinary.WideEmission.Svg, emission.Svg, StringComparison.Ordinal);
        return new HtmlChartSection(html, drawing, null, options, emission, null);
    }
}
