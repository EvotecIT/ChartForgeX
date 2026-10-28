using System.Globalization;
using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class NativeNumberCultureCaseMotionTests : IDisposable {
    [Theory]
    [InlineData("pie", "fr-number")] [InlineData("donut", "sv-currency")] [InlineData("pie", "ar-percent")] [InlineData("donut", "fr-compact")]
    [InlineData("pie", "title")] [InlineData("donut", "sentence")] [InlineData("treemap", "sentence")] [InlineData("treemap", "toggle")]
    [InlineData("pie", "literal-ms")] [InlineData("donut", "sections")] [InlineData("treemap", "scaled")] [InlineData("pie", "permille")]
    [InlineData("donut", "scientific")] [InlineData("treemap", "escaped")] [InlineData("donut", "literal-ms")] [InlineData("pie", "sections")]
    public void ActualNamedFormatterAndNativeCompositeCaseReplayOwnedFallbacksAfterCultureAndRegistryChange(string kind, string policy) {
        Register(); var (start, finish) = Readings(policy);
        var renderer = new HtmlInteractiveChartRenderer(); var from = renderer.PrepareSection(Model(kind, policy, start)); var to = renderer.PrepareSection(Model(kind, policy, finish));
        var middle = renderer.PrepareSection(Model(kind, policy, ChartMath.InterpolateRange(start, finish, .5))); var culture = CultureInfo.CurrentCulture; Exception? failure = null; IReadOnlyList<PreparedVisual>? observed = null; string? payload = null;
        try { CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR"); FontRegistry.Clear(); observed = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 3, null); payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.Values, 3)); }
        catch (Exception exception) { failure = exception; } finally { CultureInfo.CurrentCulture = culture; _ = BrowserProofFonts.Family; }
        var name = "number-culture-case-" + kind + "-" + policy; NativeRadialMotionBeforeControls.Capture(from, name + "-from"); NativeRadialMotionBeforeControls.Capture(to, name + "-to"); NativeRadialMotionBeforeControls.Capture(middle, name + "-independent");
        NativeRadialMotionBeforeControls.Save(name + ".facts.json", JsonSerializer.Serialize(new { start, finish, arithmeticMean = (start + finish) / 2d, nativeMiddle = ChartMath.InterpolateRange(start, finish, .5), payload, error = failure?.GetType().Name, message = failure?.Message, requests = from.PreparedVisual.NativeTextCapture?.CanonicalFacts(includeRuns: false) }));
        Assert.Null(failure); var frames = Assert.IsAssignableFrom<IReadOnlyList<PreparedVisual>>(observed); Assert.NotNull(payload); NativeRadialValueMotionTests.Capture(from, to, frames, payload!, name);
        Assert.Equal(middle.PreparedVisual.ToSvg(), frames[1].ToSvg()); Assert.Equal(middle.PreparedVisual.ToPng(), frames[1].ToPng()); Assert.Equal(to.PreparedVisual.ToSvg(), frames[^1].ToSvg());
        var probes = from.PreparedVisual.NativeTextCapture!.Requests.SelectMany(row => row.Probes).ToArray(); Assert.Contains(probes, row => row.Owner.StartsWith("value-format-", StringComparison.Ordinal));
        if (policy is "title" or "sentence" or "toggle") {
            var expectedOwner = kind == "treemap" ? "native-treemap-header-context" : "native-radial-label-value-context";
            Assert.Contains(probes, row => row.Owner == expectedOwner && row.CaseCulture == string.Empty);
            Assert.All(probes.Where(row => row.CaseCulture != null), row => Assert.Equal(TextCaseTransformer.Apply(row.Raw, row.Case, CultureInfo.InvariantCulture), row.Displayed));
        } else Assert.Contains(probes, row => row.Owner.StartsWith("value-format-native-", StringComparison.Ordinal) && row.CaseCulture == string.Empty);
    }
    [Theory] [InlineData("N2", "fr-FR")] [InlineData("C2", "sv-SE")] [InlineData("P1", "ar-SA")] [InlineData("E3", "tr-TR")] [InlineData("0.##", "pl-PL")]
    public void NativeRepertoireContainsActualNumberOutputsAndRejectsAProviderModifiedUnderTheSameCultureName(string format, string cultureName) {
        var named = CultureInfo.GetCultureInfo(cultureName); var policy = ChartValueFormat.Number(format, named); var accepted = policy.TryGetNativeTextRepertoire(out var owner, out var values);
        var changed = (CultureInfo)named.Clone(); changed.NumberFormat.NumberDecimalSeparator = "|"; var altered = ChartValueFormat.Number(format, changed); var rejected = altered.TryGetNativeTextRepertoire(out _, out _);
        NativeRadialMotionBeforeControls.Save("number-policy-" + cultureName + "-" + format + ".facts.json", JsonSerializer.Serialize(new { format, cultureName, accepted, owner, values, sameName = named.Name == changed.Name, equalCulture = named.Equals(changed), rejected, ordinary = altered.Format(123.45) }));
        Assert.True(accepted); Assert.Contains(policy.Format(1234.5), values); Assert.False(rejected); Assert.Throws<NotSupportedException>(() => altered.PortablePolicy()); Assert.NotEmpty(altered.Format(123.45));
    }
    [Theory] [InlineData("000000000000000000")] [InlineData("0.0E+0000")] [InlineData("\'" + "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx" + "\'0")] [InlineData("N18")]
    public void FormatsOutsideNativeWorkBoundsKeepOrdinaryFormattingWithoutAReplayRepertoire(string format) {
        var policy = ChartValueFormat.Number(format); var observed = policy.TryGetNativeTextRepertoire(out _, out _);
        NativeRadialMotionBeforeControls.Save("number-grammar-" + format.Length + "-" + ((int)format[0]) + ".facts.json", JsonSerializer.Serialize(new { format, observed, ordinary = policy.Format(123.45) })); Assert.False(observed); Assert.NotEmpty(policy.Format(123.45));
    }
    [Fact]
    public void ExistingInvariantCompactCatalogAndProbeMetadataRetainTheirExactNativeFacts() {
        NativeValueRepertoireBeforeControls.Register(); var chart = NativeValueRepertoireBeforeControls.Model(false, "compact", 900); var renderer = new HtmlInteractiveChartRenderer(); var section = renderer.PrepareSection(chart);
        var policy = ChartValueFormat.Compact(); Assert.True(policy.TryGetInvariantTextRepertoire(out var oldOwner, out var old)); Assert.True(policy.TryGetNativeTextRepertoire(out var newOwner, out var current));
        var probes = section.PreparedVisual.NativeTextCapture!.Requests.SelectMany(row => row.Probes).ToArray(); NativeRadialMotionBeforeControls.Capture(section, "number-invariant-control"); NativeRadialMotionBeforeControls.Save("number-invariant-control.facts.json", section.PreparedVisual.NativeTextCapture.CanonicalFacts(false));
        Assert.Equal(oldOwner, newOwner); Assert.Equal(old, current); Assert.All(probes, row => Assert.Null(row.CaseCulture)); Assert.DoesNotContain("caseCulture", section.PreparedVisual.NativeTextCapture.CanonicalFacts(false), StringComparison.Ordinal);
    }
    [Fact]
    public void ProbeBudgetCountsSupplementaryScalarsAndRejectsTheFirstExcessObservation() {
        Register(); var style = new TextStyle { Font = FontSpec.FromFamily(NativeFallbackClusterBeforeControls.Primary), FontSize = 14 };
        var scope = new VisualSceneTextCapture(); var builder = new VisualSceneBuilder(new VisualSize(200, 100), style.Font, scope); _ = builder.MeasureText("H", style);
        var request = scope.Requests.Single(); var raw = string.Concat(Enumerable.Repeat(char.ConvertFromUtf32(0x1F600), TextShaper.CapturedClusters.MaximumScalars));
        Exception? first = null; try { request.ObserveProbe("native-scalar-budget", raw, TextCaseTransform.None, string.Empty); } catch (Exception exception) { first = exception; }
        Exception? excess = null; try { request.ObserveProbe("native-scalar-budget", "H", TextCaseTransform.None, string.Empty); } catch (Exception exception) { excess = exception; }
        NativeRadialMotionBeforeControls.Save("number-unicode-probe-budget.facts.json", JsonSerializer.Serialize(new { utf16 = raw.Length, scalarBound = TextShaper.CapturedClusters.MaximumScalars, admitted = first?.Message, excess = excess?.Message, retained = request.Probes.Count }));
        Assert.Null(first); Assert.IsType<NotSupportedException>(excess); Assert.Single(request.Probes);
    }
    [Fact]
    public void SameNameGetFormatOverrideCannotMasqueradeAsAnUnmodifiedNamedNumericProvider() {
        var supplied = new ProviderCulture(); var format = ChartValueFormat.Number("N2", supplied); var ordinary = format.Format(1234.5);
        var admitted = format.TryGetNativeTextRepertoire(out _, out _); Exception? error = null; try { _ = format.PortablePolicy(); } catch (Exception exception) { error = exception; }
        NativeRadialMotionBeforeControls.Save("number-overridden-provider.facts.json", JsonSerializer.Serialize(new { name = format.Culture.Name, capturedType = format.Culture.GetType().FullName,
            namedDecimal = format.Culture.NumberFormat.NumberDecimalSeparator, ordinary, admitted, error = error?.Message }));
        Assert.Equal("en-US", format.Culture.Name); Assert.Equal(".", format.Culture.NumberFormat.NumberDecimalSeparator); Assert.Contains("|", ordinary, StringComparison.Ordinal);
        Assert.False(admitted); Assert.IsType<NotSupportedException>(error);
    }
    private sealed class ProviderCulture : CultureInfo {
        private readonly NumberFormatInfo _numeric;
        internal ProviderCulture() : base("en-US") { _numeric = (NumberFormatInfo)base.NumberFormat.Clone(); _numeric.NumberDecimalSeparator = "|"; }
        public override object Clone() => new ProviderCulture();
        public override object? GetFormat(Type? formatType) => formatType == typeof(NumberFormatInfo) ? _numeric : base.GetFormat(formatType);
    }
    internal static (double First, double Last) Readings(string policy) => policy == "ar-percent" ? (9,11)
        : policy == "scaled" ? (900000,1100000) : policy == "permille" ? (.009,.011) : (900,1100);
    internal static Chart Model(string kind, string policy, double reading, int width = 760, bool dark = false) {
        Chart chart;
        if (kind == "treemap") {
            chart = NativeHierarchyMotionBeforeControls.Model(false, false, reading);
            chart.Series[0].ReplaceHierarchyItems(new[] { new ChartHierarchyItem("root", "HH"), new ChartHierarchyItem("first", "HH", "root", reading), new ChartHierarchyItem("second", "HH", "root", .1), new ChartHierarchyItem("third", "HH", "root", .2) });
        } else { chart = NativeValueRepertoireBeforeControls.Model(kind == "donut", "compact", reading).WithPieSliceLabelContent(ChartPieSliceLabelContent.LabelAndValue);
            chart.Series[0].Points[1] = new ChartForgeX.Primitives.ChartPoint(2, .1); chart.Series[0].Points[2] = new ChartForgeX.Primitives.ChartPoint(3, .2); chart.WithXLabels("HH", "HH", "HH"); }
        chart.WithFontFamily(NativeFallbackClusterBeforeControls.Primary + ", " + NativeFallbackClusterBeforeControls.Fallback).WithSize(width, 420).WithThemeMode(dark ? ChartForgeX.Themes.VisualThemeMode.Dark : ChartForgeX.Themes.VisualThemeMode.Light);
        var (format, textCase) = Policy(policy); return chart.WithValueFormat(format).ConfigureDataLabelStyle(style => style.TextCase = textCase);
    }
    internal static (ChartValueFormat Format, TextCaseTransform Case) Policy(string policy) => policy switch {
        "fr-number" => (ChartValueFormat.Number("N2", CultureInfo.GetCultureInfo("fr-FR")), TextCaseTransform.None),
        "sv-currency" => (ChartValueFormat.Number("C2", CultureInfo.GetCultureInfo("sv-SE")), TextCaseTransform.SentenceCase),
        "ar-percent" => (ChartValueFormat.Number("P1", CultureInfo.GetCultureInfo("ar-SA")), TextCaseTransform.None),
        "fr-compact" => (ChartValueFormat.Compact(CultureInfo.GetCultureInfo("fr-FR")), TextCaseTransform.None),
        "title" => (ChartValueFormat.Compact(), TextCaseTransform.TitleCase), "sentence" => (ChartValueFormat.Compact(), TextCaseTransform.SentenceCase), "toggle" => (ChartValueFormat.Compact(), TextCaseTransform.ToggleCase),
        "literal-ms" => (ChartValueFormat.Number("0.00 'ms'"), TextCaseTransform.ToggleCase),
        "sections" => (ChartValueFormat.Number("0.0;[debit](0.0);'zero'"), TextCaseTransform.SentenceCase),
        "scaled" => (ChartValueFormat.Number("#,##0,, 'M'"), TextCaseTransform.None),
        "permille" => (ChartValueFormat.Number("0.0‰",CultureInfo.GetCultureInfo("fr-FR")), TextCaseTransform.None),
        "scientific" => (ChartValueFormat.Number("0.00E+00"), TextCaseTransform.TitleCase),
        "escaped" => (ChartValueFormat.Number(@"0.00\m\s"), TextCaseTransform.ToggleCase),
        _ => throw new ArgumentOutOfRangeException(nameof(policy))
    };
    internal static byte[] Register() {
        FontRegistry.Register(NativeFallbackClusterBeforeControls.Primary, NativeFallbackClusterBeforeControls.PrimaryBytes()); var glyphs = ".,kKmMbBEe+- >%…".Select(value => (int)value).Distinct().ToDictionary(value => value, _ => OpenTypeTestFonts.Box);
        foreach (var name in new[] { "fr-number", "sv-currency", "ar-percent", "fr-compact", "title", "sentence", "toggle", "literal-ms", "sections", "scaled", "permille", "scientific", "escaped" }) {
            var policy = Policy(name).Format;
            foreach (var value in new[] { 0d, 1, 2, 3, 4, 5, 6, 7, 8, 9, .1, .2, 9, 11, 900, 1100, 1234.5, -123.4, 900000, 1100000, double.Epsilon, double.MaxValue }) {
                var text = policy.Format(value);
                foreach (TextCaseTransform transform in Enum.GetValues(typeof(TextCaseTransform))) {
                    foreach (var measured in new[] { "M", "Mg" }) { var sample = TextCaseTransformer.Apply(measured, transform, CultureInfo.InvariantCulture); for (var index = 0; index < sample.Length;) glyphs[TrueTypeFont.ReadCodePoint(sample, ref index)] = OpenTypeTestFonts.Box; }
                    var display = TextCaseTransformer.Apply("HH " + text, transform, CultureInfo.InvariantCulture);
                    for (var index = 0; index < display.Length;) glyphs[TrueTypeFont.ReadCodePoint(display, ref index)] = OpenTypeTestFonts.Box;
                }
            }
        }
        var bytes = OpenTypeTestFonts.NameKeyed(extraGlyphs: glyphs); FontRegistry.Register(NativeFallbackClusterBeforeControls.Fallback, bytes); return bytes;
    }
    public void Dispose() { FontRegistry.Clear(); _ = BrowserProofFonts.Family; }
}
