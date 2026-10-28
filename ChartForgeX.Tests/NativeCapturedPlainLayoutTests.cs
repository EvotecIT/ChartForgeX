using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class NativeCapturedPlainLayoutTests {
    [Theory]
    [InlineData(false, 400, false, TextCaseTransform.None)] [InlineData(false, 700, true, TextCaseTransform.None)]
    [InlineData(false, 400, false, TextCaseTransform.TitleCase)] [InlineData(false, 400, false, TextCaseTransform.SentenceCase)]
    [InlineData(false, 400, false, TextCaseTransform.ToggleCase)] [InlineData(false, 400, true, TextCaseTransform.Uppercase)]
    [InlineData(true, 400, false, TextCaseTransform.None)] [InlineData(true, 700, true, TextCaseTransform.None)]
    public void CapturedPlainLayoutKeepsExactIncumbentWidthsWrappingCaseAndTrimmingAfterRegistryClear(bool fallback, int weight, bool italic, TextCaseTransform transform) {
        var family = BrowserProofFonts.Family; var text = "regional RECONCILIATION of native glyphs and captions";
        if (fallback) {
            FontRegistry.Register(NativeFallbackClusterBeforeControls.Primary, NativeFallbackClusterBeforeControls.PrimaryBytes());
            FontRegistry.Register(NativeFallbackClusterBeforeControls.Fallback, OpenTypeTestFonts.NameKeyed(extraGlyphs: new Dictionary<int, int> { ['…'] = OpenTypeTestFonts.Box }));
            family = NativeFallbackClusterBeforeControls.Primary + ", " + NativeFallbackClusterBeforeControls.Fallback;
            text = "HH Oe " + char.ConvertFromUtf32(OpenTypeTestFonts.PrivateUseCharacter) + " HHe\u0301 HH OO HH OO";
        }
        var style = new TextStyle { Font = FontSpec.FromFamily(family), FontSize = 13.7, LineHeight = 1.2, TextCase = transform, UnderlineStyle = TextDecorationStyle.Single };
        style.Font.Weight = weight; style.Font.Italic = italic;
        var expected = TextLayoutEngine.Layout(text, 72, style, maximumLines: 2, trimming: TextTrimming.Ellipsis);
        var scope = new VisualSceneTextCapture(); var builder = new VisualSceneBuilder(new VisualSize(240, 120), style.Font, scope);
        var recorded = builder.LayoutText(text, 72, style, maximumLines: 2, trimming: TextTrimming.Ellipsis); var captured = scope.Freeze();
        var name = "plain-layout-" + fallback + "-" + weight + "-" + italic + "-" + transform;
        NativeRadialMotionBeforeControls.Save(name + ".before-replay.json", JsonSerializer.Serialize(new { expected = Facts(expected), recorded = Facts(recorded), supported = captured.SupportsCapturedReplay, requests = captured.Requests.Select(row => row.PublicKey) }));
        try { FontRegistry.Clear(); var replay = new VisualSceneBuilder(new VisualSize(240, 120), style.Font, captured.ReplayCapturedClusters());
            var repeated = replay.LayoutText(text, 72, style, maximumLines: 2, trimming: TextTrimming.Ellipsis);
            NativeRadialMotionBeforeControls.Save(name + ".after-replay.json", JsonSerializer.Serialize(Facts(repeated)));
            Equal(expected, recorded); Equal(expected, repeated);
        } finally { _ = BrowserProofFonts.Family; }
    }
    [Fact]
    public void PlainReplayRejectsUncapturedFaceRequestRatherThanConsultingAReplacementRegistry() {
        var style = new TextStyle { Font = FontSpec.FromFamily(BrowserProofFonts.Family), FontSize = 14 }; var scope = new VisualSceneTextCapture();
        var builder = new VisualSceneBuilder(new VisualSize(200, 100), style.Font, scope); _ = builder.LayoutText("Native source caption", 70, style, maximumLines: 1);
        var snapshot = scope.Freeze(); var other = style.Clone(); other.OpenTypeLanguageTag = "TRK"; Exception? failure = null;
        try { var replay = new VisualSceneBuilder(new VisualSize(200, 100), style.Font, snapshot.ReplayCapturedClusters()); _ = replay.LayoutText("Native source caption", 70, other, maximumLines: 1); } catch (Exception exception) { failure = exception; }
        NativeRadialMotionBeforeControls.Save("plain-layout-missing-request.json", JsonSerializer.Serialize(new { error = failure?.Message })); Assert.IsType<NotSupportedException>(failure);
    }
    private static object Facts(TextLayout layout) => new { layout.Trimmed, layout.Metrics.Width, layout.Metrics.Height, layout.Metrics.LineHeight, lines = layout.Lines.Select(line => new { line.Text, line.Width }) };
    private static void Equal(TextLayout expected, TextLayout actual) {
        Assert.Equal(expected.Trimmed, actual.Trimmed); Assert.Equal(expected.Metrics.Width, actual.Metrics.Width); Assert.Equal(expected.Metrics.Height, actual.Metrics.Height); Assert.Equal(expected.Metrics.LineHeight, actual.Metrics.LineHeight);
        Assert.Equal(expected.Lines.Select(line => (line.Text, line.Width)), actual.Lines.Select(line => (line.Text, line.Width)));
    }
}
