using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Effects;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class NativeRemainingValueAppearanceCompositionTests {
    [Theory] [InlineData(false)] [InlineData(true)]
    public void JointFramesApplyTheOrderedRootAppearanceOnceToFreshNativeGeometryAndVisibility(bool bar) {
        var renderer = new HtmlInteractiveChartRenderer();
        var from = NativeJointValuesVisibilityBeforeControls.Section(renderer, NativeJointValuesVisibilityBeforeControls.Model(bar, 30), false);
        var to = NativeJointValuesVisibilityBeforeControls.Section(renderer, NativeJointValuesVisibilityBeforeControls.Model(bar, 40), true);
        var bare = VisualSeriesVisibilityCapture.ValueVisibilityFrames(from.PreparedVisual, to.PreparedVisual, 3);
        var first = Wrap(from.PreparedVisual); var last = Wrap(to.PreparedVisual);
        IReadOnlyList<PreparedVisual>? actual = null;
        var error = Record.Exception(() => actual = VisualSeriesVisibilityCapture.ValueVisibilityFrames(first, last, 3));
        Save("remaining-effects-joint-" + bar, first, last, actual, error);
        Assert.Null(error);
        Assert.Same(first, actual![0]); Assert.Same(last, actual[^1]);
        AssertFrame(Wrap(bare[1]), actual[1]);
        Assert.Equal(first.CapturedRootEffectIdentity, actual[1].CapturedRootEffectIdentity);
        Assert.Throws<NotSupportedException>(() => VisualSeriesVisibilityCapture.ValueVisibilityFrames(first,
            to.PreparedVisual.WithOutline(new VisualOutline(ChartColor.Black, 2)), 3));
    }

    [Theory] [InlineData(false)] [InlineData(true)]
    public void RawSourceKeysBindActualAppearanceIndependentlyOfBareOrWrappedFirstAccess(bool donut) {
        var renderer = new HtmlInteractiveChartRenderer();
        var first = NativeRawValueSourceMotionTests.Section(renderer, NativeRawValueSourceMotionTests.Model(donut, "other", 60)).PreparedVisual;
        var second = NativeRawValueSourceMotionTests.Section(renderer, NativeRawValueSourceMotionTests.Model(donut, "other", 60)).PreparedVisual;
        Assert.True(VisualValueCapture.TryRawSource(first, out var a, out _));
        Assert.True(VisualValueCapture.TryRawSource(second, out var b, out _));
        var bareFirst = a!.SourceKey(first);
        var firstWrapped = Wrap(first); var secondWrapped = Wrap(second);
        var wrappedAfterBare = a.SourceKey(firstWrapped); var wrappedFirst = b!.SourceKey(secondWrapped);
        var bareAfterWrapped = b.SourceKey(second);
        var reversed = ReverseWrap(first); var reversedKey = a.SourceKey(reversed);
        Save("remaining-effects-raw-key-" + donut, firstWrapped, secondWrapped, null, null);
        RetainedAppearanceFixture.Save("remaining-effects-raw-key-reversed-" + donut, reversed);
        NativeRadialMotionBeforeControls.Save("remaining-effects-raw-key-" + donut + ".facts.json",
            JsonSerializer.Serialize(new { bareFirst, bareAfterWrapped, wrappedAfterBare, wrappedFirst, reversedKey }));
        Assert.Equal(bareFirst, bareAfterWrapped); Assert.Equal(wrappedAfterBare, wrappedFirst);
        Assert.NotEqual(bareFirst, wrappedAfterBare); Assert.NotEqual(wrappedAfterBare, reversedKey);
        Assert.Equal(first.CapturedRootEffectIdentity, second.CapturedRootEffectIdentity);
        Assert.Equal(firstWrapped.CapturedNativeIdentity, secondWrapped.CapturedNativeIdentity);
    }

    [Fact]
    public void SourceReflowRetainsFullSourceAppearanceAndRejectsChangingEffectPolicy() {
        var renderer = new HtmlInteractiveChartRenderer();
        var from = NativeRawValueSourceMotionTests.Reflow(renderer, NativeRawValueSourceMotionTests.Model(true, "other", 60), false);
        var to = NativeRawValueSourceMotionTests.Reflow(renderer, NativeRawValueSourceMotionTests.Model(true, "other", 5), true);
        var first = Wrap(from.PreparedVisual); var last = Wrap(to.PreparedVisual);
        IReadOnlyList<(PreparedVisual Frame, bool Source, double Opacity)>? actual = null;
        var error = Record.Exception(() => actual = VisualSeriesVisibilityCapture.ReflowFrames(first, last, 5, sourceReflow: true));
        Save("remaining-effects-source-reflow", first, last, actual?.Select(row => row.Frame).ToArray(), error);
        Assert.Null(error);
        Assert.True(VisualValueCapture.TryRawSource(first, out var plan, out var full));
        Assert.Equal(first.CapturedRootEffectIdentity, full.CapturedRootEffectIdentity);
        Assert.NotEqual(plan!.SourceKey(full), plan.SourceKey(from.PreparedVisual.Scene.VisibilityCapture!.CompleteValueBasis));
        AssertFrame(first, actual![0].Frame); AssertFrame(last, actual[^1].Frame);
        Assert.Equal(0, actual[2].Opacity); Assert.Empty(actual[2].Frame.Regions);
        Assert.Throws<NotSupportedException>(() => VisualSeriesVisibilityCapture.ReflowFrames(first,
            to.PreparedVisual.WithDropShadow(new VisualDropShadow(ChartColor.Black, 2, 2, 0)), 5, sourceReflow: true));
    }

    [Fact]
    public void RawOwnedFlashRepaintsSourceInkInsideTheSameRootOperationExactlyOnce() {
        var renderer = new HtmlInteractiveChartRenderer();
        var from = NativeRawValueSourceMotionTests.Section(renderer, NativeRawValueSourceMotionTests.Model(false, "zero", 0)).PreparedVisual;
        var to = NativeRawValueSourceMotionTests.Section(renderer, NativeRawValueSourceMotionTests.Model(false, "zero", 80)).PreparedVisual;
        var bare = VisualValueCapture.NativeFrames(from, to, 3, ChartColor.Black);
        var first = Wrap(from); var last = Wrap(to); IReadOnlyList<PreparedVisual>? actual = null;
        var error = Record.Exception(() => actual = VisualValueCapture.NativeFrames(first, last, 3, ChartColor.Black));
        Save("remaining-effects-owned-flash", first, last, actual, error);
        Assert.Null(error); AssertFrame(Wrap(bare[1]), actual![1]);
        Assert.Same(last, actual[0]); Assert.Same(last, actual[^1]);
        Assert.Equal(last.CapturedRootEffectIdentity, actual[1].CapturedRootEffectIdentity);
    }

    private static PreparedVisual Wrap(PreparedVisual source) => source
        .WithOutline(new VisualOutline(ChartColor.FromRgba(20, 40, 80, 128), 1, false))
        .WithDropShadow(new VisualDropShadow(ChartColor.FromRgba(0, 0, 0, 96), 2, 2, 0, orderedDither: false));
    private static PreparedVisual ReverseWrap(PreparedVisual source) => source
        .WithDropShadow(new VisualDropShadow(ChartColor.FromRgba(0, 0, 0, 96), 2, 2, 0, orderedDither: false))
        .WithOutline(new VisualOutline(ChartColor.FromRgba(20, 40, 80, 128), 1, false));
    private static void AssertFrame(PreparedVisual expected, PreparedVisual actual) {
        Assert.Equal(expected.ToSvg(), actual.ToSvg());
        Assert.Equal(expected.ToSvg("remaining-effects-check"), actual.ToSvg("remaining-effects-check"));
        Assert.Equal(expected.ToPng(), actual.ToPng());
        Assert.Equal(expected.Regions.Select(row => (row.Id, row.Role, row.Label, row.Bounds)),
            actual.Regions.Select(row => (row.Id, row.Role, row.Label, row.Bounds)));
    }
    private static void Save(string name, PreparedVisual from, PreparedVisual to, IReadOnlyList<PreparedVisual>? frames, Exception? error) {
        NativeRadialMotionBeforeControls.Save(name + ".facts.json", JsonSerializer.Serialize(new { error = error?.ToString(), count = frames?.Count }));
        RetainedAppearanceFixture.Save(name + "-from", from); RetainedAppearanceFixture.Save(name + "-to", to);
        if (frames != null) for (var index = 0; index < frames.Count; index++) RetainedAppearanceFixture.Save(name + "-frame-" + index, frames[index]);
    }
}
