using System;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Effects;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class RetainedAppearanceFrameContracts {
    [Theory]
    [InlineData("outline")] [InlineData("bloom")] [InlineData("shadow")]
    public void ValueFramesRepaintBareCapturedIndicesThenApplyTheSameRootPolicyOnce(string kind) {
        PreparedVisual Source(double value) {
            var chart = NativeCartesianValueBeforeControls.Model(false, "linear", value);
            return RetainedAppearanceFixture.Wrap(chart.Prepare(VisualExportRequest.ForChart(chart).Context), kind);
        }
        var from = Source(20); var to = Source(80); var expected = Source(50);
        var frames = VisualValueCapture.Frames(from, to, 3);
        RetainedAppearanceFixture.Save("appearance-value-from-" + kind, from);
        RetainedAppearanceFixture.Save("appearance-value-middle-" + kind, frames[1]);
        RetainedAppearanceFixture.Save("appearance-value-independent-" + kind, expected);
        Assert.Same(from, frames[0]); Assert.Same(to, frames[2]);
        Assert.Equal(expected.ToSvg("same-native-frame"), frames[1].ToSvg("same-native-frame"));
        Assert.Equal(expected.ToPng(), frames[1].ToPng());
        Assert.Equal(from.CapturedRootEffectIdentity, to.CapturedRootEffectIdentity);
        Assert.Equal(from.NativeTextCapture, frames[1].NativeTextCapture);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void VisibilityBindsActualSourceViewAndPolicyThroughTheCanonicalPreserveAndReflowOwners(bool reflow) {
        var chart = reflow ? NativeReflowVisibilityBeforeControls.Model("line") : NativeSeriesVisibilityDefaultControls.Model("line");
        chart.WithDataLabels(false);
        var context = VisualExportRequest.ForChart(chart).Context;
        ChartVisibilityView View(bool hide) => reflow
            ? new ChartVisibilityView(hide ? new[] { ChartVisibilityTarget.Observation(chart.Series[0].Id, new ChartObservationId("current")) }
                : Array.Empty<ChartVisibilityTarget>(), ChartVisibilityLayout.Reflow)
            : new ChartVisibilityView(hide ? new[] { chart.Series[0].Id } : null);
        var rawFrom = chart.PrepareVisibility(context, View(false));
        var rawTo = chart.PrepareVisibility(context, View(true));
        var from = RetainedAppearanceFixture.Wrap(rawFrom, "outline");
        var to = RetainedAppearanceFixture.Wrap(rawTo, "outline");
        RetainedAppearanceFixture.Save("appearance-visibility-from-" + reflow, from);
        RetainedAppearanceFixture.Save("appearance-visibility-to-" + reflow, to);
        Assert.Equal(rawFrom.VisibilityView, from.VisibilityView);
        Assert.Equal(rawTo.VisibilityView, to.VisibilityView);
        Assert.NotSame(rawFrom.Scene.VisibilityCapture, from.Scene.VisibilityCapture);
        if (reflow) {
            var frames = VisualSeriesVisibilityCapture.ReflowFrames(from, to, 3);
            RetainedAppearanceFixture.Save("appearance-reflow-middle", frames[1].Frame);
            Assert.Equal(from.Scene.VisibilityCapture!.SourceKey, to.Scene.VisibilityCapture!.SourceKey);
            Assert.NotEqual(rawFrom.Scene.VisibilityCapture!.SourceKey, from.Scene.VisibilityCapture.SourceKey);
            Assert.Same(from, frames[0].Frame); Assert.Same(to, frames[2].Frame);
        } else {
            var frames = VisualSeriesVisibilityCapture.Frames(from, to, 3);
            RetainedAppearanceFixture.Save("appearance-preserve-middle", frames[1]);
            Assert.Same(from, frames[0]); Assert.Same(to, frames[2]);
            Assert.Equal(from.CapturedRootEffectIdentity, frames[1].CapturedRootEffectIdentity);
        }
    }

    [Fact]
    public void SamePaintAlphaMasksStillRequireTheSameRawRetainedInputsForValueCompatibility() {
        PreparedVisual Source(double value, byte ignoredRed) {
            var chart = NativeCartesianValueBeforeControls.Model(false, "linear", value);
            var native = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
            var width = (int)native.Size.Width; var height = (int)native.Size.Height;
            var pixels = new byte[width * height * 4];
            for (var at = 0; at < pixels.Length; at += 4) { pixels[at] = ignoredRed; pixels[at + 3] = 255; }
            return native.WithMask(new VisualMask(new ChartForgeX.Raster.RgbaImage(width, height, pixels), orderedDither: false));
        }
        var from = Source(20, 0); var changedRaw = Source(80, 255); var ordinaryTarget = Source(80, 0);
        RetainedAppearanceFixture.Save("appearance-unpainted-mask-facts-from", from);
        RetainedAppearanceFixture.Save("appearance-unpainted-mask-facts-to", changedRaw);
        Assert.Equal(ordinaryTarget.ToPng(), changedRaw.ToPng());
        Assert.NotEqual(from.CapturedRootEffectIdentity, changedRaw.CapturedRootEffectIdentity);
        Assert.Throws<NotSupportedException>(() => VisualValueCapture.Frames(from, changedRaw, 3));
        Assert.Equal(3, VisualValueCapture.Frames(from, ordinaryTarget, 3).Count);
    }
}
