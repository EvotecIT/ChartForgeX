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
            .Title("Wide panel")
            .Size(620, 200)
            .AddLine("Trend", Points(10, 24, 18, 35));
        var compact = Chart.Create()
            .Title("Compact panel")
            .Size(300, 200)
            .AddBar("Values", Points(22, 31, 27));
        var grid = ChartGrid.Create()
            .Theme(VisualTheme.Graphite())
            .Columns(3)
            .Gap(10)
            .Padding(10)
            .PanelSize(300, 200).PanelFit(ChartForgeX.Primitives.VisualPanelFit.Stretch)
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

        var mutable = ChartGrid.Create().Add(compact).PanelSpan(0, 2);
        Assert(mutable.PanelSpans[0].ColumnSpan == 2, "Existing grid panels should support span updates.");
        AssertThrows<ArgumentOutOfRangeException>(() => ChartGrid.Create().Add(compact, 0), "Grid chart adds should reject zero column spans.");
        AssertThrows<ArgumentOutOfRangeException>(() => ChartGrid.Create().Add(compact, 1, 0), "Grid chart adds should reject zero row spans.");
        AssertThrows<ArgumentOutOfRangeException>(() => mutable.PanelSpan(1, 1), "Grid panel span updates should reject missing chart indexes.");
    }

    private static void ChartGridHeadersSupportTextStyles() {
        var grid = ChartGrid.Create()
            .Title("styled grid header")
            .Subtitle("GRID-LEVEL TYPOGRAPHY SHOULD MATCH CHART-LEVEL POLISH")
            .TitleStyle(style => style.Color(ChartColor.FromHex("#be123c")).FontSize(32).FontFamily("Georgia, serif").FontWeight(900).Italic().UnderlineStyle(TextDecorationStyle.Dotted).StrikethroughStyle(TextDecorationStyle.Wavy).Baseline(TextBaseline.Superscript).TextCase(TextCaseTransform.Uppercase))
            .SubtitleStyle(style => style.Color(ChartColor.FromHex("#0e7490")).FontSize(16).Italic().UnderlineStyle(TextDecorationStyle.Dotted).Baseline(TextBaseline.Subscript).TextCase(TextCaseTransform.Lowercase))
            .PanelSize(260, 160)
            .Add(Chart.Create().Title("Panel").Size(260, 160).AddLine("Values", Points(1, 2, 3)));
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
            && headingText.Any(node => Math.Abs(node.Text.Size - 16 * .65) < .001)
            && headingText.All(node => node.Text.Style.Baseline == TextBaseline.Normal),
            "Prepared headings should materialize script sizing and position once, without retaining a second backend baseline shift.");
        Assert(string.Join(" ", headings.Select(element => element.Value)).Contains("STYLED GRID HEADER", StringComparison.Ordinal),
            "Shared header measurement and painting should materialize title casing.");
        grid.ThemeMode = ThemeMode.Light;
        prepared = grid.Prepare(VisualExportRequest.ForGrid(grid).Context);
        var html = grid.ToHtmlFragment();
        var inlineSvg = prepared.ToSvg(new VisualSvgOptions(colorVariables: grid.SvgColorVariables, responsive: true));
        Assert(html.Contains(inlineSvg, StringComparison.Ordinal), "Static HTML should retain the exact prepared typography and layout with responsive export policy.");
        Assert(ReadBigEndianInt32(grid.ToPng(), 16) > 0, "Styled grid headers should render native PNG output.");
        var panel = Chart.Create().Title("Panel").Size(260, 160).AddLine("Values", Points(1, 2, 3));
        var regularRaster = ChartGrid.Create().Title("Italic Grid Header").Subtitle("Italic Grid Subtitle").PanelSize(260, 160).Add(panel).ToPng();
        var italicRaster = ChartGrid.Create()
            .Title("Italic Grid Header")
            .Subtitle("Italic Grid Subtitle")
            .TitleStyle(style => style.Italic())
            .SubtitleStyle(style => style.Italic())
            .PanelSize(260, 160)
            .Add(panel)
            .ToPng();
        Assert(!regularRaster.SequenceEqual(italicRaster), "PNG grid headers should render italic pixels instead of silently using regular text.");

        var themedFont = VisualTheme.Graphite();
        themedFont = themedFont.WithFontFamily("Georgia, serif");
        var inheritedFontRaster = ChartGrid.Create()
            .Theme(themedFont)
            .Title("Inherited Grid Header")
            .Subtitle("Theme font inheritance")
            .PanelSize(260, 160)
            .Add(panel)
            .ToPng();
        var explicitFontRaster = ChartGrid.Create()
            .Theme(themedFont)
            .Title("Inherited Grid Header")
            .Subtitle("Theme font inheritance")
            .TitleStyle(style => style.FontFamily("Georgia, serif"))
            .SubtitleStyle(style => style.FontFamily("Georgia, serif"))
            .PanelSize(260, 160)
            .Add(panel)
            .ToPng();
        Assert(inheritedFontRaster.SequenceEqual(explicitFontRaster), "PNG grid headers without a role font override should draw with the grid theme font used for measurement.");
        AssertThrows<ArgumentNullException>(() => ChartGrid.Create().TitleStyle((Action<TextStyleOverride>)null!), "Grid title styles should reject null callbacks.");
        AssertThrows<ArgumentOutOfRangeException>(() => ChartGrid.Create().SubtitleStyle(style => style.FontSize(0)), "Grid subtitle styles should reject invalid font sizes.");
    }
}
