using System;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void ChartGridsSupportPanelSpans() {
        var wide = Chart.Create()
            .WithTitle("Wide panel")
            .WithSize(620, 200)
            .AddLine("Trend", Points(10, 24, 18, 35));
        var compact = Chart.Create()
            .WithTitle("Compact panel")
            .WithSize(300, 200)
            .AddBar("Values", Points(22, 31, 27));
        var grid = ChartGrid.Create()
            .WithTheme(ChartTheme.ReportLight())
            .WithColumns(3)
            .WithGap(10)
            .WithPadding(10)
            .WithPanelSize(300, 200)
            .Add(wide, 2)
            .Add(compact)
            .Add(compact);

        var html = grid.ToHtmlPage();
        Assert(html.Contains("grid-column:span 2", StringComparison.Ordinal), "HTML grids should expose panel column spans.");
        Assert(html.Contains("grid-auto-rows:var(--cfx-grid-panel-height,auto)", StringComparison.Ordinal), "HTML grids should define stable rows for fixed-height spanned panels.");
        Assert(html.Contains("@media(max-width:900px){body{padding:16px}.chartforgex-grid-body{grid-template-columns:1fr;grid-auto-rows:auto}.chartforgex-grid-panel{grid-column:auto!important;grid-row:auto!important;min-height:0}", StringComparison.Ordinal), "HTML grids should collapse fixed panel heights on narrow screens so wide panels do not leave large blank sections.");
        Assert(CountOccurrences(html, "<svg ") == 3, "Spanned HTML grids should still render every chart inline.");

        var svg = grid.ToSvg();
        Assert(svg.Contains("width=\"940\" height=\"430\"", StringComparison.Ordinal), "Spanned SVG grids should preserve composed grid dimensions.");
        Assert(svg.Contains("width=\"610\" height=\"197\"", StringComparison.Ordinal), "Spanned SVG grids should fit wide charts into the wider panel area.");

        var png = grid.ToPng();
        Assert(ReadBigEndianInt32(png, 16) == 940, "Spanned PNG grids should preserve composed grid width.");
        Assert(ReadBigEndianInt32(png, 20) == 430, "Spanned PNG grids should preserve composed grid height.");

        var mutable = ChartGrid.Create().Add(compact).WithPanelSpan(0, 2);
        Assert(mutable.PanelSpans[0].ColumnSpan == 2, "Existing grid panels should support span updates.");
        AssertThrows<ArgumentOutOfRangeException>(() => ChartGrid.Create().Add(compact, 0), "Grid chart adds should reject zero column spans.");
        AssertThrows<ArgumentOutOfRangeException>(() => ChartGrid.Create().Add(compact, 1, 0), "Grid chart adds should reject zero row spans.");
        AssertThrows<ArgumentOutOfRangeException>(() => mutable.WithPanelSpan(1, 1), "Grid panel span updates should reject missing chart indexes.");
    }

    private static void ChartGridHeadersSupportTextStyles() {
        var grid = ChartGrid.Create()
            .WithTitle("styled grid header")
            .WithSubtitle("GRID-LEVEL TYPOGRAPHY SHOULD MATCH CHART-LEVEL POLISH")
            .WithTitleStyle(style => style.WithColor("#be123c").WithFontSize(32).WithFontFamily("Georgia, serif").WithWeight("900").WithItalic().WithUnderline(TextDecorationStyle.Dotted).WithStrikethrough(TextDecorationStyle.Wavy).WithSuperscript().WithTextCase(TextCaseTransform.Uppercase))
            .WithSubtitleStyle(style => style.WithColor("#0e7490").WithFontSize(15).WithItalic().WithUnderline(TextDecorationStyle.Dotted).WithSubscript().WithTextCase(TextCaseTransform.Lowercase))
            .WithPanelSize(260, 160)
            .Add(Chart.Create().WithTitle("Panel").WithSize(260, 160).AddLine("Values", Points(1, 2, 3)));
        var svg = grid.ToSvg();
        Assert(svg.Contains("data-cfx-role=\"grid-title\"", StringComparison.Ordinal) && svg.Contains("fill=\"#BE123C\"", StringComparison.Ordinal), "SVG grid titles should honor grid title styles.");
        Assert(svg.Contains("font-family=\"Georgia, serif\"", StringComparison.Ordinal), "SVG grid title styles should honor font families.");
        Assert(svg.Contains("font-style=\"italic\"", StringComparison.Ordinal) && svg.Contains("text-decoration=\"line-through\"", StringComparison.Ordinal) && svg.Contains("text-decoration-style=\"wavy\"", StringComparison.Ordinal) && svg.Contains("<tspan text-decoration=\"underline\" text-decoration-style=\"dotted\">STYLED GRID HEADER</tspan>", StringComparison.Ordinal), "SVG grid title styles should preserve independent underline and strikethrough patterns.");
        Assert(svg.Contains("data-cfx-role=\"grid-subtitle\"", StringComparison.Ordinal) && svg.Contains("fill=\"#0E7490\"", StringComparison.Ordinal) && svg.Contains("baseline-shift=\"sub\"", StringComparison.Ordinal), "SVG grid subtitles should honor colors and script placement.");
        Assert(svg.Contains(">STYLED GRID HEADER</tspan></text>", StringComparison.Ordinal) && svg.Contains(">grid-level typography", StringComparison.Ordinal), "SVG grid headers should materialize casing before fitting and trimming.");
        var html = grid.ToHtmlFragment();
        Assert(html.Contains("text-decoration:line-through;text-decoration-style:wavy", StringComparison.Ordinal) && html.Contains("<span style=\"text-decoration:underline;text-decoration-style:dotted\">STYLED GRID HEADER</span>", StringComparison.Ordinal) && html.Contains("vertical-align:super", StringComparison.Ordinal), "HTML grid headers should preserve independent decoration patterns and baseline styling on inline text.");
        Assert(html.Contains("STYLED GRID HEADER", StringComparison.Ordinal) && html.Contains("grid-level typography should match chart-level polish", StringComparison.Ordinal), "HTML grid headers should materialize casing transforms.");
        Assert(ReadBigEndianInt32(grid.ToPng(), 16) > 0, "Styled grid headers should render PNG output.");

        var panel = Chart.Create().WithTitle("Panel").WithSize(260, 160).AddLine("Values", Points(1, 2, 3));
        var regularRaster = ChartGrid.Create().WithTitle("Italic Grid Header").WithSubtitle("Italic Grid Subtitle").WithPanelSize(260, 160).Add(panel).ToPng();
        var italicRaster = ChartGrid.Create()
            .WithTitle("Italic Grid Header")
            .WithSubtitle("Italic Grid Subtitle")
            .WithTitleStyle(style => style.WithItalic())
            .WithSubtitleStyle(style => style.WithItalic())
            .WithPanelSize(260, 160)
            .Add(panel)
            .ToPng();
        Assert(!regularRaster.SequenceEqual(italicRaster), "PNG grid headers should render italic pixels instead of silently using regular text.");

        var themedFont = ChartTheme.ReportLight();
        themedFont.FontFamily = "Georgia, serif";
        var inheritedFontRaster = ChartGrid.Create()
            .WithTheme(themedFont)
            .WithTitle("Inherited Grid Header")
            .WithSubtitle("Theme font inheritance")
            .WithPanelSize(260, 160)
            .Add(panel)
            .ToPng();
        var explicitFontRaster = ChartGrid.Create()
            .WithTheme(themedFont)
            .WithTitle("Inherited Grid Header")
            .WithSubtitle("Theme font inheritance")
            .WithTitleStyle(style => style.WithFontFamily("Georgia, serif"))
            .WithSubtitleStyle(style => style.WithFontFamily("Georgia, serif"))
            .WithPanelSize(260, 160)
            .Add(panel)
            .ToPng();
        Assert(inheritedFontRaster.SequenceEqual(explicitFontRaster), "PNG grid headers without a role font override should draw with the grid theme font used for measurement.");
        AssertThrows<ArgumentNullException>(() => ChartGrid.Create().WithTitleStyle(null!), "Grid title styles should reject null callbacks.");
        AssertThrows<ArgumentOutOfRangeException>(() => ChartGrid.Create().WithSubtitleStyle(style => style.WithFontSize(0)), "Grid subtitle styles should reject invalid font sizes.");
    }
}
