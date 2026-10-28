using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class NativeFallbackClusterMotionTests : IDisposable {
    [Theory] [InlineData(false, false)] [InlineData(false, true)] [InlineData(true, false)] [InlineData(true, true)]
    public void CompleteMixedFaceFramesEqualIndependentNativePreparationAfterRegistryReplacement(bool donut, bool outside) {
        NativeFallbackClusterBeforeControls.Register(); var renderer = new HtmlInteractiveChartRenderer();
        Chart Model(double value) => NativeFallbackClusterBeforeControls.Model(donut, true, value).WithDataLabelPlacement(outside ? ChartDataLabelPlacement.Outside : ChartDataLabelPlacement.Inside);
        var from = renderer.PrepareSection(Model(60)); var to = renderer.PrepareSection(Model(80)); var middle = renderer.PrepareSection(Model(70));
        var options = new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.Values, 3); var payload = renderer.PrepareNativeTransitionJson(from, to, options);
        var frames = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 3, null); var name = "captured-fallback-" + donut + "-" + outside;
        NativeRadialValueMotionTests.Capture(from, to, frames, payload, name); NativeRadialMotionBeforeControls.Capture(middle, name + "-independent");
        var captured = from.PreparedVisual.NativeTextCapture!; var count = captured.Requests.Sum(request => request.Clusters.Decisions.Count);
        NativeRadialMotionBeforeControls.Save(name + ".facts.json", JsonSerializer.Serialize(new { fallbackDecisions = count, fonts = captured.CanonicalFacts(false), captured.SupportsStrictPrimary, captured.SupportsCapturedReplay }));
        var svg = frames.Select(frame => frame.ToSvg()).ToArray(); var png = frames.Select(frame => frame.ToPng()).ToArray();
        FontRegistry.Register(NativeFallbackClusterBeforeControls.Fallback, OpenTypeTestFonts.NameKeyed(includePrivateUse: false)); FontRegistry.Clear();
        var repeated = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 3, null);
        Assert.True(count > 0); Assert.False(captured.SupportsStrictPrimary); Assert.True(captured.SupportsCapturedReplay);
        Assert.Equal(middle.PreparedVisual.ToSvg(), frames[1].ToSvg()); Assert.Equal(middle.PreparedVisual.ToSvg("native-captured-fallback"), frames[1].ToSvg("native-captured-fallback"));
        Assert.Equal(middle.PreparedVisual.ToPng(), frames[1].ToPng()); Assert.Equal(svg, repeated.Select(frame => frame.ToSvg()));
        for (var index = 0; index < repeated.Count; index++) Assert.Equal(png[index], repeated[index].ToPng());
        Assert.Equal(payload, renderer.PrepareNativeTransitionJson(from, to, options)); Assert.Throws<NotSupportedException>(() => captured.Replay());
    }
    [Theory] [InlineData(false)] [InlineData(true)]
    public void ExplicitInitialValuesRetainCapturedFallbackFacesWithoutChangingTheNativeFinalEndpoint(bool donut) {
        NativeFallbackClusterBeforeControls.Register(); var renderer = new HtmlInteractiveChartRenderer();
        var from = renderer.PrepareSection(NativeFallbackClusterBeforeControls.Model(donut, true, 60)); var to = renderer.PrepareSection(NativeFallbackClusterBeforeControls.Model(donut, true, 80));
        var middle = renderer.PrepareSection(NativeFallbackClusterBeforeControls.Model(donut, true, 70));
        var payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.InitialValues, 3));
        var frames = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 3, null);
        NativeRadialValueMotionTests.Capture(from, to, frames, payload, "fallback-initial-" + donut); NativeRadialMotionBeforeControls.Capture(middle, "fallback-initial-independent-" + donut);
        Assert.Equal(middle.PreparedVisual.ToSvg(), frames[1].ToSvg()); Assert.Equal(middle.PreparedVisual.ToPng(), frames[1].ToPng());
        Assert.Equal(to.PreparedVisual.ToSvg(), frames[2].ToSvg()); Assert.Equal(to.PreparedVisual.ToPng(), frames[2].ToPng());
    }
    [Fact]
    public void CapturedDecisionDoesNotGuessAnUnseenClusterEvenWhenItsOwnedFaceCoversIt() {
        NativeFallbackClusterBeforeControls.Register(); FontRegistry.Register(NativeFallbackClusterBeforeControls.Fallback,
            OpenTypeTestFonts.NameKeyed(extraGlyphs: new Dictionary<int, int> { [0xEFFE] = OpenTypeTestFonts.Box }));
        var style = Style(); var scope = new VisualSceneTextCapture(); var builder = new VisualSceneBuilder(new VisualSize(240, 100), style.Font, scope);
        builder.Text("H" + char.ConvertFromUtf32(OpenTypeTestFonts.PrivateUseCharacter), 10, 50, style); var snapshot = scope.Freeze();
        var decision = snapshot.Requests.Single().Clusters.Decisions.Single(); FontRegistry.Clear(); var replay = snapshot.ReplayCapturedClusters();
        var target = new VisualSceneBuilder(new VisualSize(240, 100), style.Font, replay); target.Text("H" + char.ConvertFromUtf32(OpenTypeTestFonts.PrivateUseCharacter), 10, 50, style);
        Exception? missing = null; try { target.MeasureText(char.ConvertFromUtf32(0xEFFE), style); } catch (Exception exception) { missing = exception; }
        var changedLanguage = style.Clone(); changedLanguage.OpenTypeLanguageTag = "TRK"; Exception? language = null;
        try { target.MeasureText("H", changedLanguage); } catch (Exception exception) { language = exception; }
        Save(new PreparedVisual(target.Build()), "unknown", new { knownRaw = decision.Raw, coveredUnseen = decision.Face.HasGlyph(0xEFFE), missing = missing?.Message, language = language?.Message });
        Assert.True(decision.Face.HasGlyph(0xEFFE)); Assert.IsType<NotSupportedException>(missing); Assert.IsType<NotSupportedException>(language);
        Assert.Throws<InvalidOperationException>(() => snapshot.Bind(style));
    }
    [Fact]
    public void ConflictingActualFallbackFacesDisableReplayWithoutChangingOrdinaryRendering() {
        NativeFallbackClusterBeforeControls.Register(); var style = Style(); var scope = new VisualSceneTextCapture(); var builder = new VisualSceneBuilder(new VisualSize(240, 100), style.Font, scope);
        var text = "H" + char.ConvertFromUtf32(OpenTypeTestFonts.PrivateUseCharacter); _ = builder.MeasureText(text, style);
        FontRegistry.Register(NativeFallbackClusterBeforeControls.Fallback, OpenTypeTestFonts.NameKeyed(weight: 700)); builder.Text(text, 10, 50, style);
        var snapshot = scope.Freeze(); Exception? failure = null; try { _ = snapshot.ReplayCapturedClusters(); } catch (Exception exception) { failure = exception; }
        Save(new PreparedVisual(builder.Build()), "conflict", new { snapshot.SupportsCapturedReplay, error = failure?.Message });
        Assert.False(snapshot.SupportsCapturedReplay); Assert.IsType<NotSupportedException>(failure);
    }
    [Theory] [InlineData(false)] [InlineData(true)]
    public void ActualNativeFallbackClusterAndScalarLimitsRejectBeforeReplayAllocation(bool scalarLimit) {
        NativeFallbackClusterBeforeControls.Register(); var style = Style(); var scope = new VisualSceneTextCapture(); var builder = new VisualSceneBuilder(new VisualSize(240, 100), style.Font, scope);
        var character = char.ConvertFromUtf32(OpenTypeTestFonts.PrivateUseCharacter); var observed = 0;
        if (scalarLimit) { _ = builder.MeasureText(character + new string((char)0x034F, TextShaper.CapturedClusters.MaximumScalars - 1), style); observed = 1; }
        else for (var index = 0; index <= TextShaper.CapturedClusters.MaximumClusters; index++) {
            var text = new System.Text.StringBuilder(character); for (var bit = 0; bit < 13; bit++) text.Append((char)((index & (1 << bit)) == 0 ? 0xFE00 : 0xFE01));
            _ = builder.MeasureText(text.ToString(), style); observed++;
        }
        builder.Text(character, 10, 50, style); var snapshot = scope.Freeze(); Exception? failure = null;
        try { _ = snapshot.ReplayCapturedClusters(); } catch (Exception exception) { failure = exception; }
        Save(new PreparedVisual(builder.Build()), "limit-" + scalarLimit, new { observed, retained = snapshot.Requests.Single().Clusters.Decisions.Count, snapshot.SupportsCapturedReplay, error = failure?.Message });
        Assert.False(snapshot.SupportsCapturedReplay); Assert.IsType<NotSupportedException>(failure);
        Assert.Equal(scalarLimit ? 0 : TextShaper.CapturedClusters.MaximumClusters, snapshot.Requests.Single().Clusters.Decisions.Count);
    }
    internal static TextStyle Style() => new() { Font = FontSpec.FromFamily(NativeFallbackClusterBeforeControls.Primary + ", " + NativeFallbackClusterBeforeControls.Fallback), FontSize = 20 };
    internal static void Save(PreparedVisual prepared, string name, object facts) {
        NativeRadialMotionBeforeControls.Save("native-fallback-" + name + ".facts.json", JsonSerializer.Serialize(facts)); NativeRadialMotionBeforeControls.Save("native-fallback-" + name + ".svg", prepared.ToSvg());
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) { Directory.CreateDirectory(output); File.WriteAllBytes(Path.Combine(output, "native-fallback-" + name + ".native.png"), prepared.ToPng()); }
    }
    public void Dispose() { FontRegistry.Clear(); _ = BrowserProofFonts.Family; }
}
