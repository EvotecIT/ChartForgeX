using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Checks shortened labels against the actual prepared font and fallback glyph metrics.</summary>
[Collection(nameof(FontRegistryCollection))]
public sealed class V2LabelPlacementTests {
    [Fact]
    public void ShortenedFallbackText_UsesBackendMeasurements_AndResolvesCasingOnce() {
        try {
            Register("V2 Placement Primary", "primary", 900);
            Register("V2 Placement Fallback", "regular", 400);
            var font = FontSpec.FromFamily("V2 Placement Primary, V2 Placement Fallback");
            font.Weight = 900;
            var style = new TextStyle { Font = font, FontSize = 12, TextCase = TextCaseTransform.ToggleCase };
            var resolved = TypographyFontResolver.ResolveFace(font);
            var fallback = TextShaper.Shape(resolved.Font!, "B", 12);
            Assert.NotSame(resolved.Font!.Root, Assert.Single(fallback).Face.Root);
            var builder = new VisualSceneBuilder(new VisualSize(160, 80), font);
            const string original = "bBbBbBbBbBbBbBbBbB";
            var displayed = TextCaseTransformer.Apply(original, style.TextCase, System.Globalization.CultureInfo.InvariantCulture);
            var shownStyle = style.Clone(); shownStyle.TextCase = TextCaseTransform.None;
            var shortMetrics = builder.MeasureText(displayed.Substring(0, 3) + "…", shownStyle);
            var bounds = new ChartRect(0, 0, shortMetrics.Width + .001, shortMetrics.Height + .001);
            var request = new LabelPlacementRequest(original, new ChartPoint(0, 0), style, new[] { new LabelCandidate(0, 0) }) {
                MeasuredSize = builder.MeasureText(original, style)
            };
            var measuredShortenedText = false;
            TextMetrics Measure(string text, TextStyle measuringStyle) {
                Assert.Equal(TextCaseTransform.None, measuringStyle.TextCase);
                measuredShortenedText |= text.EndsWith("…", StringComparison.Ordinal);
                return builder.MeasureText(text, measuringStyle);
            }
            var result = Assert.Single(new LabelPlacementService().Place(new[] { request }, bounds, null, 2, Measure));
            Assert.False(result.IsDropped);
            Assert.True(result.IsEllipsized);
            Assert.True(measuredShortenedText);
            Assert.StartsWith(displayed.Substring(0, 1), result.Text);
            var actual = builder.MeasureText(result.Text, shownStyle);
            Assert.Equal(actual.Width, result.Bounds.Width, 8);
            Assert.Equal(actual.Height, result.Bounds.Height, 8);
            Assert.True(actual.Width <= bounds.Width && actual.Height <= bounds.Height);
        } finally {
            FontRegistry.Clear();
        }
    }

    private static void Register(string family, string fixture, int weight) {
        using var stream = typeof(V2LabelPlacementTests).Assembly.GetManifestResourceStream("ChartForgeX.Tests.Fixtures.OpenType.fallback-weight-" + fixture + ".ttf")!;
        FontRegistry.Register(family, stream, weight);
    }
}
