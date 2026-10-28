using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class NativeValueRepertoireMotionTests : IDisposable {
    [Theory]
    [InlineData(false, TextCaseTransform.None, false)] [InlineData(true, TextCaseTransform.None, true)]
    [InlineData(false, TextCaseTransform.Uppercase, true)] [InlineData(true, TextCaseTransform.Lowercase, false)]
    public void NativeSuffixCrossingsReplayOwnedObservedFacesAfterRegistryClearAndEqualTheIndependentMidpoint(bool donut, TextCaseTransform textCase, bool initial) {
        Register(); var renderer = new HtmlInteractiveChartRenderer();
        Chart Model(double value) => NativeValueRepertoireBeforeControls.Model(donut, "compact", value).ConfigureDataLabelStyle(style => style.TextCase = textCase);
        var from = renderer.PrepareSection(Model(900)); var to = renderer.PrepareSection(Model(1100)); var middle = renderer.PrepareSection(Model(1000));
        NativeRadialMotionBeforeControls.Capture(from, "value-repertoire-post-" + donut + "-" + textCase + "-from");
        NativeRadialMotionBeforeControls.Capture(to, "value-repertoire-post-" + donut + "-" + textCase + "-to");
        NativeRadialMotionBeforeControls.Capture(middle, "value-repertoire-post-" + donut + "-" + textCase + "-independent");
        string? payload = null; Exception? failure = null; IReadOnlyList<PreparedVisual>? observed = null;
        try { FontRegistry.Clear(); observed = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 3, null);
            payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(initial ? HtmlNativeTransitionEffect.InitialValues : HtmlNativeTransitionEffect.Values, 3));
        } catch (Exception exception) { failure = exception; }
        NativeRadialMotionBeforeControls.Save("value-repertoire-post-" + donut + "-" + textCase + ".facts.json", JsonSerializer.Serialize(new { payload, error = failure?.GetType().Name, message = failure?.Message }));
        var format = ChartValueFormat.Compact(); var hasCatalog = format.TryGetInvariantTextRepertoire(out var owner, out var catalog);
        var nativeText = from.PreparedVisual.NativeTextCapture!; var probes = nativeText.Requests.SelectMany(request => request.Probes).ToArray();
        var witness = format.Format(1234.5); var actualMidpointText = format.Format(1000);
        NativeRadialMotionBeforeControls.Save("value-repertoire-post-" + donut + "-" + textCase + ".probes.json", JsonSerializer.Serialize(new {
            hasCatalog, owner, catalog, witness, actualMidpointText, probes = probes.Select(probe => new { probe.Owner, probe.Raw, probe.Case, probe.Displayed }),
            capturedFaces = nativeText.CanonicalFacts(includeRuns: false) }));
        Assert.Null(failure); Assert.NotNull(payload); var frames = Assert.IsAssignableFrom<IReadOnlyList<PreparedVisual>>(observed);
        NativeRadialValueMotionTests.Capture(from, to, frames, payload!, "repertoire-owned-" + donut + "-" + textCase);
        Assert.Equal(middle.PreparedVisual.ToSvg(), frames[1].ToSvg()); Assert.Equal(middle.PreparedVisual.ToPng(), frames[1].ToPng());
        Assert.Equal(to.PreparedVisual.ToSvg(), frames[2].ToSvg()); Assert.Equal(to.PreparedVisual.ToPng(), frames[2].ToPng());
        Assert.True(hasCatalog); Assert.Contains(witness, catalog);
        Assert.Contains(probes, probe => probe.Owner == owner && probe.Raw == witness && probe.Case == textCase
            && probe.Displayed == TextCaseTransformer.Apply(witness, textCase, System.Globalization.CultureInfo.InvariantCulture));
    }
    [Fact]
    public void UnpaintedSuffixFaceChangesBindTheReflowSourceKeyEvenWhenOrdinaryNativeSvgIsIdentical() {
        Register(); var renderer = new HtmlInteractiveChartRenderer();
        Chart Model() => NativeValueRepertoireBeforeControls.Model(false, "compact", 900);
        var ordinaryBefore = renderer.PrepareSection(Model());
        var before = renderer.PrepareVisibilitySection(Model(), new ChartVisibilityView(Array.Empty<ChartVisibilityTarget>(), ChartVisibilityLayout.Reflow));
        Register(kGlyph: OpenTypeTestFonts.H);
        var ordinaryAfter = renderer.PrepareSection(Model());
        var after = renderer.PrepareVisibilitySection(Model(), new ChartVisibilityView(hiddenTargets: new[] {
            ChartVisibilityTarget.Observation(new ChartSeriesId("parts"), new ChartObservationId("third")) }, layout: ChartVisibilityLayout.Reflow));
        Exception? failure = null; try { _ = VisualSeriesVisibilityCapture.ReflowFrames(before.PreparedVisual, after.PreparedVisual, 3); } catch (Exception exception) { failure = exception; }
        NativeRadialMotionBeforeControls.Capture(ordinaryBefore, "value-repertoire-unpainted-before"); NativeRadialMotionBeforeControls.Capture(ordinaryAfter, "value-repertoire-unpainted-after");
        NativeRadialMotionBeforeControls.Capture(before, "value-repertoire-reflow-before"); NativeRadialMotionBeforeControls.Capture(after, "value-repertoire-reflow-after");
        NativeRadialMotionBeforeControls.Save("value-repertoire-unpainted-key.facts.json", JsonSerializer.Serialize(new {
            first = before.PreparedVisual.Scene.VisibilityCapture!.SourceKey, second = after.PreparedVisual.Scene.VisibilityCapture!.SourceKey, error = failure?.Message }));
        Assert.Equal(ordinaryBefore.PreparedVisual.ToSvg(), ordinaryAfter.PreparedVisual.ToSvg());
        Assert.NotEqual(before.PreparedVisual.Scene.VisibilityCapture!.SourceKey, after.PreparedVisual.Scene.VisibilityCapture!.SourceKey);
        Assert.IsType<NotSupportedException>(failure);
    }
    internal static byte[] FallbackBytes(int kGlyph = OpenTypeTestFonts.Box) {
        var glyphs = ".,kKmMbBE+- >%".Distinct().ToDictionary(character => (int)character, _ => OpenTypeTestFonts.Box); glyphs['k'] = kGlyph;
        return OpenTypeTestFonts.NameKeyed(extraGlyphs: glyphs);
    }
    private static void Register(int kGlyph = OpenTypeTestFonts.Box) {
        FontRegistry.Register(NativeFallbackClusterBeforeControls.Primary, NativeFallbackClusterBeforeControls.PrimaryBytes());
        FontRegistry.Register(NativeFallbackClusterBeforeControls.Fallback, FallbackBytes(kGlyph));
    }
    public void Dispose() { FontRegistry.Clear(); _ = BrowserProofFonts.Family; }
}
