using System;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Themes;

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
            .WithTitle("Styled Grid Header")
            .WithSubtitle("Grid-level typography should match chart-level polish")
            .WithTitleStyle(style => style.WithColor("#be123c").WithFontSize(32).WithFontFamily("Georgia, serif").WithWeight("900").WithItalic().WithUnderline())
            .WithSubtitleStyle(style => style.WithColor("#0e7490").WithFontSize(15).WithItalic())
            .WithPanelSize(260, 160)
            .Add(Chart.Create().WithTitle("Panel").WithSize(260, 160).AddLine("Values", Points(1, 2, 3)));
        var svg = grid.ToSvg();
        Assert(svg.Contains("data-cfx-role=\"grid-title\"", StringComparison.Ordinal) && svg.Contains("fill=\"#BE123C\"", StringComparison.Ordinal), "SVG grid titles should honor grid title styles.");
        Assert(svg.Contains("font-family=\"Georgia, serif\"", StringComparison.Ordinal), "SVG grid title styles should honor font families.");
        Assert(svg.Contains("font-style=\"italic\"", StringComparison.Ordinal) && svg.Contains("text-decoration=\"underline\"", StringComparison.Ordinal), "SVG grid title styles should honor italic and underline.");
        Assert(svg.Contains("data-cfx-role=\"grid-subtitle\"", StringComparison.Ordinal) && svg.Contains("fill=\"#0E7490\"", StringComparison.Ordinal), "SVG grid subtitles should honor grid subtitle styles.");
        Assert(grid.ToHtmlFragment().Contains("text-decoration:underline", StringComparison.Ordinal), "HTML grid headers should honor grid text decoration.");
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
        AssertThrows<ArgumentNullException>(() => ChartGrid.Create().WithTitleStyle(null!), "Grid title styles should reject null callbacks.");
        AssertThrows<ArgumentOutOfRangeException>(() => ChartGrid.Create().WithSubtitleStyle(style => style.WithFontSize(0)), "Grid subtitle styles should reject invalid font sizes.");
    }
}
