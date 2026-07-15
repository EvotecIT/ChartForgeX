using System;
using System.Linq;
using ChartForgeX.Composition;
using ChartForgeX.Primitives;
using ChartForgeX.Typography;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void SharedTypographyMeasuresAndWrapsDeterministically() {
        var style = TextStyle.Create(18, ChartColor.Black);
        style.Font = FontSpec.FromFamily("Segoe UI, Arial, sans-serif");
        style.LineHeight = 1.25;

        var first = TextLayoutEngine.Layout("Premium deterministic typography for every renderer", 150, style);
        var second = TextLayoutEngine.Layout("Premium deterministic typography for every renderer", 150, style);

        Assert(first.Lines.Count > 1, "Shared typography should wrap text at word boundaries.");
        Assert(first.Lines.Select(line => line.Text).SequenceEqual(second.Lines.Select(line => line.Text)), "Text layout should be deterministic for identical inputs.");
        Assert(Math.Abs(first.Metrics.Width - second.Metrics.Width) < 0.0001, "Text measurement should be deterministic for identical inputs.");
        Assert(first.Metrics.Width <= 150.001, "Wrapped lines should fit the requested width.");
        Assert(first.Metrics.LineHeight > style.FontSize, "Configured line height should increase vertical line spacing.");
    }

    private static void SharedTypographyTrimsAtConfiguredLineLimit() {
        var style = TextStyle.Create(16, ChartColor.Black);
        var layout = TextLayoutEngine.Layout("one two three four five six seven eight", 72, style, maximumLines: 2, trimming: TextTrimming.Ellipsis);

        Assert(layout.Trimmed, "Text layout should report content removed by the line limit.");
        Assert(layout.Lines.Count == 2, "Text layout should honor the requested maximum line count.");
        Assert(layout.Lines[1].Text.EndsWith("…", StringComparison.Ordinal), "Trimmed text should end with an ellipsis.");
        Assert(layout.Lines[1].Width <= 72.001, "Ellipsized text should fit the requested width.");
    }

    private static void TextStyleOverridesResolveWithoutASecondStyleBrain() {
        var fallback = TextStyle.Create(16, ChartColor.Black);
        fallback.Font = FontSpec.FromFamily("system-ui, sans-serif");
        var overrides = new TextStyleOverride()
            .WithColor("#7C3AED")
            .WithFontFamily("Aptos, sans-serif")
            .WithFontSize(22)
            .WithWeight("bold")
            .WithItalic()
            .WithUnderline();

        var resolved = overrides.Resolve(fallback);
        Assert(resolved.Color.ToHex() == "#7C3AED" && resolved.Font.Family == "Aptos, sans-serif" && resolved.FontSize == 22, "Text overrides should resolve color, family, and size over the shared complete style.");
        Assert(resolved.Font.Weight == 700 && resolved.Font.Italic && resolved.Underline, "Text overrides should resolve weight and decoration into the shared typography model.");
        Assert(fallback.Font.Family == "system-ui, sans-serif" && fallback.FontSize == 16, "Resolving text overrides should not mutate the fallback style.");
    }

    private static void ImageCompositionUsesSharedTypographyContract() {
        var style = TextStyle.Create(20, ChartColor.FromHex("#F8FAFC"));
        style.Font = FontSpec.FromFamily("Consolas, monospace");
        style.Font.Weight = 700;
        style.Alignment = TextAlignment.Center;

        var png = ImageComposition.Create(260, 100, ChartColor.FromHex("#0F172A"))
            .DrawText(20, 16, 220, "ChartForgeX typography wraps without System.Drawing", style, maximumLines: 3)
            .ToPng();

        Assert(png.Length > 200, "Shared typography should produce a non-empty composed PNG.");
        Assert(png[0] == 137 && png[1] == 80 && png[2] == 78 && png[3] == 71, "Shared typography composition should produce PNG output.");
    }
}
