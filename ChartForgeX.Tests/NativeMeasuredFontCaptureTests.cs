using System.Text.Json;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class NativeMeasuredFontCaptureTests : IDisposable {
    private const string Primary = "CFX Captured Native Primary";
    private const string Fallback = "CFX Captured Native Fallback";
    [Fact]
    public void OmittedMeasurementAndAscentBindOwnedFontBytesAndActualLanguageInputs() {
        FontRegistry.Register(Primary, OpenTypeTestFonts.NameKeyed());
        var a = Capture("TRK"); var same = Capture("TRK");
        FontRegistry.Register(Primary, OpenTypeTestFonts.NameKeyed(weight: 700)); var b = Capture("TRK"); var language = Capture("SRB");
        Save(a.Prepared, a.Capture, "first"); Save(b.Prepared, b.Capture, "replaced"); Save(language.Prepared, language.Capture, "language");
        Assert.Empty(a.Prepared.Scene.Nodes.OfType<VisualSceneText>()); Assert.Equal(a.Prepared.ToSvg(), b.Prepared.ToSvg()); Assert.Equal(a.Prepared.ToPng(), b.Prepared.ToPng());
        Assert.Equal(a.Capture.CanonicalFacts(true), same.Capture.CanonicalFacts(true)); Assert.NotEqual(a.Capture.CanonicalFacts(true), b.Capture.CanonicalFacts(true));
        Assert.NotEqual(b.Capture.CanonicalFacts(false), language.Capture.CanonicalFacts(false)); Assert.Single(a.Capture.Requests); Assert.Single(a.Capture.Runs);
        Assert.DoesNotContain("/Volumes/", a.Capture.CanonicalFacts(true), StringComparison.Ordinal); Assert.DoesNotContain("/Users/", a.Capture.CanonicalFacts(false), StringComparison.Ordinal);
    }
    [Fact]
    public void ReplayBindsFreshRequestedColorAndAlignmentWithoutTouchingCapturedFaceOrConsultingRegistry() {
        FontRegistry.Register(Primary, OpenTypeTestFonts.NameKeyed()); var captured = Capture("TRK").Capture;
        var style = Style("TRK"); style.Color = ChartColor.FromRgb(170, 20, 40); style.Alignment = TextAlignment.Right;
        var before = captured.CanonicalFacts(true); FontRegistry.Clear(); var replay = captured.Replay();
        var builder = new VisualSceneBuilder(new VisualSize(200, 90), FontSpec.FromFamily(Primary), replay);
        builder.Text("HH", 180, 55, style); var prepared = new PreparedVisual(builder.Build()); Save(prepared, replay, "replayed");
        var row = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneText>()); Assert.Equal(style.Color, row.Text.Style.Color); Assert.Equal(style.Alignment, row.Text.Style.Alignment);
        Assert.Equal(style.Color, row.Color); Assert.Equal(style.Alignment, row.Alignment); Assert.Equal(before, captured.CanonicalFacts(true));
        var unrecorded = Style("SRB"); Assert.Throws<NotSupportedException>(() => builder.MeasureText("H", unrecorded));
        var missing = char.ConvertFromUtf32(OpenTypeTestFonts.PrivateUseCharacter); // The primary fixture owns this glyph; a genuinely missing scalar still rejects.
        Assert.True(builder.MeasureText(missing, style).Width > 0); Assert.Throws<NotSupportedException>(() => builder.MeasureText(char.ConvertFromUtf32(0x10FFFD), style));
    }
    [Fact]
    public void RecordedFallbackAndMissingGlyphRunsRejectStrictReplayWhileOrdinaryShapeKeepsItsExistingPolicy() {
        FontRegistry.Register(Primary, OpenTypeTestFonts.NameKeyed(includePrivateUse: false)); FontRegistry.Register(Fallback, OpenTypeTestFonts.NameKeyed());
        var style = Style(null); style.Font.Family = Primary + ", " + Fallback; var capture = new VisualSceneTextCapture();
        var builder = new VisualSceneBuilder(new VisualSize(200, 90), style.Font, capture); var text = "H" + char.ConvertFromUtf32(OpenTypeTestFonts.PrivateUseCharacter);
        _ = builder.MeasureText(text, style); builder.Text(text, 10, 45, style); var prepared = new PreparedVisual(builder.Build()); Save(prepared, capture, "fallback");
        Assert.False(capture.SupportsStrictPrimary); Assert.Throws<NotSupportedException>(() => capture.Replay());
        Assert.Equal(2, prepared.Scene.Nodes.OfType<VisualSceneText>().Single().Text.Lines.Single().Glyphs.Select(row => row.Face).Distinct().Count());
        Assert.NotEmpty(prepared.ToPng());
    }
    private static (PreparedVisual Prepared, VisualSceneTextCapture Capture) Capture(string language) {
        var capture = new VisualSceneTextCapture(); var style = Style(language); var builder = new VisualSceneBuilder(new VisualSize(200, 90), style.Font, capture);
        _ = builder.MeasureText("HH", style); _ = builder.TextAscent(style); _ = builder.TextAscentEnvelope(style);
        return (new PreparedVisual(builder.Build()), capture.Freeze());
    }
    private static TextStyle Style(string? language) => new() { Font = FontSpec.FromFamily(Primary), FontSize = 24, OpenTypeLanguageTag = language };
    private static void Save(PreparedVisual prepared, VisualSceneTextCapture capture, string name) {
        NativeRadialMotionBeforeControls.Save("native-measured-font-" + name + ".facts.json", JsonSerializer.Serialize(new { faces = capture.CanonicalFacts(false), runs = capture.CanonicalFacts(true), capture.SupportsStrictPrimary }));
        NativeRadialMotionBeforeControls.Save("native-measured-font-" + name + ".svg", prepared.ToSvg());
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) { Directory.CreateDirectory(output); File.WriteAllBytes(Path.Combine(output, "native-measured-font-" + name + ".native.png"), prepared.ToPng()); }
    }
    public void Dispose() { FontRegistry.Clear(); _ = BrowserProofFonts.Family; }
}
