using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
internal static partial class ExpressiveExamples {
    private static Chart CreateColourEmojiShowcase() => Chart.Create()
        .WithTitle("Colour emoji in report labels")
        .WithSubtitle("Font palettes, joined sequences and bitmap strikes use the same glyph layout")
        .WithTheme(ChartTheme.ReportLight())
        .WithSize(960, 460)
        .WithXAxis("Faces · symbols · joined sequences")
        .WithYAxis("Samples")
        .WithTickLabelStyle(style => style.WithFontFamily("'Segoe UI Emoji', 'Noto Color Emoji', 'Apple Color Emoji', sans-serif").WithFontSize(32))
        .WithXLabels("😀", "❤️", "👩‍💻", "👨‍👩‍👧‍👦", "🏳️‍🌈")
        .AddBar("Observed", Points(32, 48, 43, 66, 51), ChartColor.FromHex("#2563eb"));

    private static Chart CreateFontLayoutShowcase() => Chart.Create()
        .WithTitle("Font substitutions and attached marks")
        .WithSubtitle("One glyph layout for measurement and PNG painting")
        .WithTheme(ChartTheme.ReportLight())
        .WithSize(860, 440)
        .WithXAxis("Labels in the selected font stack")
        .WithYAxis("Samples")
        .WithTickLabelStyle(style => style.WithFontFamily("Arial, 'Leelawadee UI', sans-serif").WithFontSize(24))
        .WithXLabels("office", "affine", "بِبّ", "สวัสดี")
        .AddBar("Observed", Points(32, 48, 43, 66), ChartColor.FromHex("#2563eb"));

    private static Chart CreateScriptShapingShowcase() => Chart.Create()
        .WithTitle("Syllables in the selected font")
        .WithSubtitle("Conjuncts, pre-base vowels and attached marks share one text layout")
        .WithTheme(ChartTheme.ReportLight())
        .WithSize(960, 460)
        .WithXAxis("Devanagari · Bengali · Tamil · Thai · Khmer")
        .WithYAxis("Samples")
        .WithTickLabelStyle(style => style.WithFontFamily("'Nirmala UI', 'Leelawadee UI', 'Noto Sans Devanagari', 'Noto Sans Bengali', 'Noto Sans Tamil', 'Noto Sans Thai', 'Noto Sans Khmer', sans-serif").WithFontSize(28))
        .WithXLabels("क्षेत्र", "ক্ষেত্র", "தமிழ்", "น้ำ", "ខ្មែរ")
        .AddBar("Observed", Points(32, 48, 43, 66, 51), ChartColor.FromHex("#2563eb"));

    private static ChartGrid CreateThemeShowcaseGrid() {
        var auroraThemePreview = Chart.Create()
            .WithTitle("Aurora")
            .WithSubtitle("Geometric sans with vivid dark-mode color")
            .WithXAxis("Week")
            .WithYAxis("Signal")
            .WithTheme(ChartTheme.Aurora())
            .WithSize(420, 260)
            .WithXLabels("W1", "W2", "W3", "W4")
            .AddSmoothArea("Observed", Points(32, 48, 43, 66));

        var editorialThemePreview = Chart.Create()
            .WithTitle("Editorial")
            .WithSubtitle("Serif typography for publication-style output")
            .WithXAxis("Issue")
            .WithYAxis("Readers")
            .WithTheme(ChartTheme.Editorial())
            .WithSize(420, 260)
            .WithXLabels("A", "B", "C", "D")
            .AddBar("Readership", Points(44, 58, 51, 72));

        var candyThemePreview = Chart.Create()
            .WithTitle("Candy")
            .WithSubtitle("Rounded type and playful contrast")
            .WithXAxis("Cohort")
            .WithYAxis("Joy")
            .WithTheme(ChartTheme.Candy())
            .WithSize(420, 260)
            .WithXLabels("New", "Trial", "Paid", "Fans")
            .AddSmoothLine("Score", Points(48, 64, 70, 88));

        var terminalThemePreview = Chart.Create()
            .WithTitle("Terminal")
            .WithSubtitle("Monospace operations dashboard styling")
            .WithXAxis("Run")
            .WithYAxis("Pass")
            .WithTheme(ChartTheme.Terminal())
            .WithSize(420, 260)
            .WithXLabels("01", "02", "03", "04")
            .AddStepArea("Passed", Points(55, 61, 58, 74));

        return ChartGrid.Create()
            .WithTitle("Theme and Font Showcase")
            .WithSubtitle("Built-in themes combine palettes, typography, strokes, and radii")
            .WithColumns(2)
            .WithPadding(28)
            .WithPanelSize(420, 260)
            .Add(auroraThemePreview)
            .Add(editorialThemePreview)
            .Add(candyThemePreview)
            .Add(terminalThemePreview);
    }

    private static Chart CreateTextStyleShowcase() {
        var chart = Chart.Create()
            .WithTitle("Styled Report Typography")
            .WithSubtitle("Color, family, size, weight, italic, decoration, baseline, and casing share one renderer contract")
            .WithTheme(ChartTheme.Editorial())
            .WithSize(860, 480)
            .WithXAxis("Audience cohort")
            .WithYAxis("Engagement")
            .WithLegendPosition(ChartLegendPosition.TopRight)
            .WithDataLabels()
            .WithValueFormatter(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%")
            .WithTitleStyle(style => style.WithColor("#be123c").WithFontFamily("Georgia, 'Times New Roman', serif").WithWeight("900").WithItalic().WithUnderline(TextDecorationStyle.Wavy).WithTextCase(TextCaseTransform.TitleCase))
            .WithSubtitleStyle(style => style.WithColor("#0e7490").WithItalic())
            .WithAxisTitleStyle(style => style.WithColor("#7c3aed").WithUnderline(TextDecorationStyle.Double))
            .WithTickLabelStyle(style => style.WithColor("#2563eb").WithItalic().WithTextCase(TextCaseTransform.Uppercase))
            .WithLegendStyle(style => style.WithColor("#15803d").WithUnderline())
            .WithDataLabelStyle(style => style.WithColor("#b45309").WithWeight("800").WithFontSize(15))
            .WithXLabels("Trial", "First value", "Power user", "Advocate")
            .AddBar("Activation share", Points(38, 54, 72, 84), ChartColor.FromHex("#f472b6"))
            .AddSmoothLine("Referral lift", Points(20, 36, 55, 69), ChartColor.FromHex("#14b8a6"));
        chart.Series[0].WithPointDataLabelStyle(2, style => style.WithFontSize(15).WithSuperscript());
        chart.Series[1].WithDataLabelStyle(style => style.WithColor("#0f766e").WithWeight("900").WithFontSize(15).WithUnderline(TextDecorationStyle.Dotted).WithStrikethrough(TextDecorationStyle.Single).WithSubscript());
        return chart;
    }

}
