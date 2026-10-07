using System;
using System.Linq;
using System.Xml.Linq;
using ChartForgeX.Rendering;
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
            .WithPanelSize(300, 200).WithPanelFit(ChartForgeX.Primitives.VisualPanelFit.Stretch)
            .Add(wide, 2)
            .Add(compact)
            .Add(compact);

        var html = grid.ToHtmlPage();
        Assert(CountOccurrences(html, "<svg ") == 1, "Spanned HTML grids should embed the same complete prepared comparison.");
        var request = VisualExportRequest.ForGrid(grid);
        var panels = grid.Prepare(request.Context).Regions.Where(region => region.Role == "panel").ToArray();
        Assert(panels.Length == 3 && panels[0].Bounds.Width > panels[1].Bounds.Width * 1.9,
            "A two-column stretch panel should occupy both columns and their intervening gap.");
        var svg = XDocument.Parse(grid.ToSvg());
        Assert(svg.Root!.Attribute("width")!.Value == "940" && svg.Root.Attribute("height")!.Value == "430",
            "Spanned SVG grids should preserve the composed viewport dimensions.");
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
        var headings = XDocument.Parse(svg).Descendants().Where(element => ((string?)element.Attribute("data-cfx-role"))?.StartsWith("frame-heading", StringComparison.Ordinal) == true).ToArray();
        Assert(headings.SelectMany(element => element.Descendants()).Any(element => (string?)element.Attribute("fill") == "#BE123C" && (string?)element.Attribute("font-family") == "Georgia, serif"),
            "Prepared grid headings should honor explicit colors and role fonts.");
        var prepared = grid.Prepare(VisualExportRequest.ForGrid(grid).Context);
        Assert(svg.Contains("font-style=\"italic\"", StringComparison.Ordinal)
            && prepared.Scene.Nodes.OfType<VisualScenePath>().Any(node => node.Role == "text-decoration")
            && prepared.Scene.Nodes.OfType<VisualSceneLine>().Any(node => node.Role == "text-decoration" && node.Dash != null),
            "Prepared headings should paint both wavy strike geometry and dotted underline geometry in every backend.");
        var headingText = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "frame-heading").ToArray();
        Assert(headingText.Any(node => Math.Abs(node.Text.Size - 32 * .65) < .001)
            && headingText.Any(node => Math.Abs(node.Text.Size - 15 * .65) < .001)
            && headingText.All(node => node.Text.Style.Baseline == TextBaseline.Normal),
            "Prepared headings should materialize script sizing and position once, without retaining a second backend baseline shift.");
        Assert(string.Join(" ", headings.Select(element => element.Value)).Contains("STYLED GRID HEADER", StringComparison.Ordinal),
            "Shared header measurement and painting should materialize title casing.");
        var html = grid.ToHtmlFragment();
        Assert(html.Contains(svg, StringComparison.Ordinal), "Static HTML should retain the exact prepared typography and layout.");
        Assert(ReadBigEndianInt32(grid.ToPng(), 16) > 0, "Styled grid headers should render native PNG output.");
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
