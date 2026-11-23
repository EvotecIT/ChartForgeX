using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
internal static partial class ExpressiveExamples {
    private static Chart CreateFallbackFontShowcase() => Chart.Create()
        .Title("Mixed-script report labels")
        .Subtitle("Fallback glyphs retain their shaped advances and the requested bold text policy")
        .Theme(VisualTheme.Graphite())
        .Size(800, 383)
        .XAxis(x => x.Title("Font stack · Segoe UI Black · Segoe UI · Noto Sans Arabic"))
        .YAxis(y => y.Title("Samples"))
        .TickLabelStyle(style => style.FontFamily("'Segoe UI Black', 'Segoe UI', 'Noto Sans Arabic', sans-serif").FontWeight(900).FontSize(28))
        .XLabels("Report", "مرحبا", "بِبّ", "Value 123")
        .AddBar("Observed", Points(32, 48, 43, 66), ChartColor.FromHex("#2563eb"));
    private static Chart CreateEmbeddedBitmapShowcase() {
        var chart = Chart.Create()
            .Title("Authored small-text strikes")
            .Subtitle("Full fitting uses exact-size embedded pixels when supplied by the selected font")
            .Theme(VisualTheme.Graphite())
            .Size(800, 383)
            .XAxis(x => x.Title("Calibri when installed · ordinary outlines otherwise"))
            .YAxis(y => y.Title("Samples"))
            .TickLabelStyle(style => style.FontFamily("Calibri, 'Noto Sans', sans-serif").FontSize(12))
            .XLabels("Report 12", "Chart AV", "office", "Value 123")
            .AddBar("Observed", Points(32, 48, 43, 66), ChartColor.FromHex("#2563eb"));
        chart.Options.Png.TextHinting = TextHinting.Full;
        return chart;
    }
    private static Chart CreateFontPaletteShowcase() => Chart.Create()
        .Title("Colour-font palettes in report labels")
        .Subtitle("Palette 3 uses the font's alternate colours; fonts without that palette keep their default")
        .Theme(VisualTheme.Graphite())
        .Size(800, 383)
        .XAxis(x => x.Title("Nabla when installed · ordinary fallback otherwise"))
        .YAxis(y => y.Title("Samples"))
        .TickLabelStyle(style => style.FontFamily("Nabla, system-ui, sans-serif").ColorPaletteIndex(3).FontSize(28))
        .XLabels("Report", "Chart", "Value", "Label")
        .AddBar("Observed", Points(32, 48, 43, 66), ChartColor.FromHex("#2563eb"));
    private static Chart CreateVariableFontShowcase() => Chart.Create()
        .Title("Variable font report labels")
        .Subtitle("Explicit weight and optical size share the font's outlines, advances and attached marks")
        .Theme(VisualTheme.Graphite())
        .Size(800, 383)
        .XAxis(x => x.Title("Selected axes · wght 650 · opsz 20"))
        .YAxis(y => y.Title("Samples"))
        .TickLabelStyle(style => style.FontFamily("'Segoe UI Variable', 'Source Serif 4', sans-serif").Variation("wght", 650).Variation("opsz", 20).FontSize(26))
        .XLabels("Variable AV", "office", "Hé xÁ", "Report 123")
        .AddBar("Observed", Points(32, 48, 43, 66), ChartColor.FromHex("#2563eb"));
    private static Chart CreateSmallTextLayoutShowcase() {
        var chart = Chart.Create()
            .Title("Small report labels and attached marks")
            .Subtitle("Font-defined positioning follows the logical text size when export resolution changes")
            .Theme(VisualTheme.Graphite())
            .Size(800, 383)
            .XAxis(x => x.Title("Labels at 12 logical pixels"))
            .YAxis(y => y.Title("Samples"))
            .TickLabelStyle(style => style.FontFamily("Arial, 'Noto Sans', sans-serif").FontSize(12))
            .DataLabelStyle(style => style.FontSize(10))
            .XLabels("a\u0301", "i\u0307", "a\u0301\u0300", "بِبّ")
            .DataLabels()
            .AddBar("Observed", Points(32, 48, 43, 66), ChartColor.FromHex("#2563eb"));
        // Full fitting is visible at 1x export; 2x labels exceed the small-text pixel-size threshold.
        chart.Options.Png.TextHinting = TextHinting.Full;
        return chart;
    }

    private static Chart CreateLanguageFormsShowcase() => Chart.Create()
        .Title("Localized font forms in report labels")
        .Subtitle("An explicit Serbian language system selects the font's localized Cyrillic forms")
        .Theme(VisualTheme.Graphite())
        .Size(800, 383)
        .XAxis(x => x.Title("Serbian labels · selected font language"))
        .YAxis(y => y.Title("Samples"))
        .TickLabelStyle(style => style.FontFamily("Calibri, 'Segoe UI', 'Noto Sans', sans-serif").Italic().OpenTypeLanguageTag("SRB").FontSize(28))
        .XLabels("бгдпт", "Београд", "Нови Сад", "Крагујевац")
        .AddBar("Observed", Points(32, 48, 43, 66), ChartColor.FromHex("#2563eb"));

    private static Chart CreateSinhalaMyanmarShowcase() => Chart.Create()
        .Title("Sinhala and Myanmar report labels")
        .Subtitle("Split vowels, explicit joining and kinzi use the selected font's forms")
        .Theme(VisualTheme.Graphite())
        .Size(800, 383)
        .XAxis(x => x.Title("Sinhala · Myanmar"))
        .YAxis(y => y.Title("Samples"))
        .TickLabelStyle(style => style.FontFamily("'Nirmala UI', 'Myanmar Text', 'Noto Sans Sinhala', 'Noto Sans Myanmar', sans-serif").FontSize(28))
        .XLabels("සිංහල", "ශ්‍රී ලංකාව", "မြန်မာ", "မင်္ဂလာပါ")
        .AddBar("Observed", Points(32, 48, 43, 66), ChartColor.FromHex("#2563eb"));

    private static Chart CreateColourEmojiShowcase() => Chart.Create()
        .Title("Colour emoji in report labels")
        .Subtitle("Font palettes, joined sequences and bitmap strikes use the same glyph layout")
        .Theme(VisualTheme.Graphite())
        .Size(800, 383)
        .XAxis(x => x.Title("Faces · symbols · joined sequences"))
        .YAxis(y => y.Title("Samples"))
        .TickLabelStyle(style => style.FontFamily("'Segoe UI Emoji', 'Noto Color Emoji', 'Apple Color Emoji', sans-serif").FontSize(32))
        .XLabels("😀", "❤️", "👩‍💻", "👨‍👩‍👧‍👦", "🏳️‍🌈")
        .AddBar("Observed", Points(32, 48, 43, 66, 51), ChartColor.FromHex("#2563eb"));

    private static Chart CreateFontLayoutShowcase() => Chart.Create()
        .Title("Font substitutions and attached marks")
        .Subtitle("One glyph layout for measurement and PNG painting")
        .Theme(VisualTheme.Graphite())
        .Size(800, 409)
        .XAxis(x => x.Title("Labels in the selected font stack"))
        .YAxis(y => y.Title("Samples"))
        .TickLabelStyle(style => style.FontFamily("Arial, 'Leelawadee UI', sans-serif").FontSize(24))
        .XLabels("office", "affine", "بِبّ", "สวัสดี")
        .AddBar("Observed", Points(32, 48, 43, 66), ChartColor.FromHex("#2563eb"));

    private static Chart CreateScriptShapingShowcase() => Chart.Create()
        .Title("Syllables in the selected font")
        .Subtitle("Conjuncts, pre-base vowels and attached marks share one text layout")
        .Theme(VisualTheme.Graphite())
        .Size(800, 383)
        .XAxis(x => x.Title("Devanagari · Bengali · Tamil · Thai · Khmer"))
        .YAxis(y => y.Title("Samples"))
        .TickLabelStyle(style => style.FontFamily("'Nirmala UI', 'Leelawadee UI', 'Noto Sans Devanagari', 'Noto Sans Bengali', 'Noto Sans Tamil', 'Noto Sans Thai', 'Noto Sans Khmer', sans-serif").FontSize(28))
        .XLabels("क्षेत्र", "ক্ষেত্র", "தமிழ்", "น้ำ", "ខ្មែរ")
        .AddBar("Observed", Points(32, 48, 43, 66, 51), ChartColor.FromHex("#2563eb"));

    private static ChartGrid CreateThemeShowcaseGrid() {
        var auroraThemePreview = Chart.Create()
            .Title("Aurora")
            .Subtitle("Geometric sans with vivid dark-mode color")
            .XAxis(x => x.Title("Week"))
            .YAxis(y => y.Title("Signal"))
            .Theme(VisualTheme.Graphite())
            .Size(420, 260)
            .XLabels("W1", "W2", "W3", "W4")
            .AddSmoothArea("Observed", Points(32, 48, 43, 66));

        var editorialThemePreview = Chart.Create()
            .Title("Editorial")
            .Subtitle("Serif typography for publication-style output")
            .XAxis(x => x.Title("Issue"))
            .YAxis(y => y.Title("Readers"))
            .Theme(VisualTheme.Graphite())
            .Size(420, 260)
            .XLabels("A", "B", "C", "D")
            .AddBar("Readership", Points(44, 58, 51, 72));

        var candyThemePreview = Chart.Create()
            .Title("Candy")
            .Subtitle("Rounded type and playful contrast")
            .XAxis(x => x.Title("Cohort"))
            .YAxis(y => y.Title("Joy"))
            .Theme(VisualTheme.Graphite())
            .Size(420, 260)
            .XLabels("New", "Trial", "Paid", "Fans")
            .AddSmoothLine("Score", Points(48, 64, 70, 88));

        var terminalThemePreview = Chart.Create()
            .Title("Terminal")
            .Subtitle("Monospace operations dashboard styling")
            .XAxis(x => x.Title("Run"))
            .YAxis(y => y.Title("Pass"))
            .Theme(VisualTheme.Graphite())
            .Size(420, 260)
            .XLabels("01", "02", "03", "04")
            .AddStepArea("Passed", Points(55, 61, 58, 74));

        return ChartGrid.Create()
            .Title("Theme and Font Showcase")
            .Subtitle("Built-in themes combine palettes, typography, strokes, and radii")
            .UsePanelThemes()
            .Columns(2)
            .Padding(28)
            .PanelSize(420, 260)
            .Add(auroraThemePreview)
            .Add(editorialThemePreview)
            .Add(candyThemePreview)
            .Add(terminalThemePreview);
    }

    private static Chart CreateTextStyleShowcase() {
        var chart = Chart.Create()
            .Title("Styled Report Typography")
            .Subtitle("Color, family, size, weight, italic, decoration, baseline, and casing share one renderer contract")
            .Theme(VisualTheme.Graphite())
            .Size(800, 447)
            .XAxis(x => x.Title("Audience cohort"))
            .YAxis(y => y.Title("Engagement"))
            .Legend(l => l.Position(ChartLegendPosition.TopRight))
            .DataLabels()
            .FormatValuesBy(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%")
            .TitleStyle(style => style.Color(ChartColor.FromHex("#be123c")).FontFamily("Georgia, 'Times New Roman', serif").FontWeight(900).Italic().UnderlineStyle(TextDecorationStyle.Wavy).TextCase(TextCaseTransform.TitleCase))
            .SubtitleStyle(style => style.Color(ChartColor.FromHex("#0e7490")).Italic())
            .AxisTitleStyle(style => style.Color(ChartColor.FromHex("#7c3aed")).UnderlineStyle(TextDecorationStyle.Double))
            .TickLabelStyle(style => style.Color(ChartColor.FromHex("#2563eb")).Italic().TextCase(TextCaseTransform.Uppercase))
            .LegendStyle(style => style.Color(ChartColor.FromHex("#15803d")).Underline())
            .DataLabelStyle(style => style.Color(ChartColor.FromHex("#b45309")).FontWeight(800).FontSize(15))
            .XLabels("Trial", "First value", "Power user", "Advocate")
            .AddBar("Activation share", Points(38, 54, 72, 84), ChartColor.FromHex("#f472b6"))
            .AddSmoothLine("Referral lift", Points(20, 36, 55, 69), ChartColor.FromHex("#14b8a6"));
        chart.Series[0].PointDataLabelStyle(2, style => style.FontSize(15).Baseline(TextBaseline.Superscript));
        chart.Series[1].DataLabelStyle(style => style.Color(ChartColor.FromHex("#0f766e")).FontWeight(900).FontSize(15).UnderlineStyle(TextDecorationStyle.Dotted).StrikethroughStyle(TextDecorationStyle.Single).Baseline(TextBaseline.Subscript));
        return chart;
    }

}
