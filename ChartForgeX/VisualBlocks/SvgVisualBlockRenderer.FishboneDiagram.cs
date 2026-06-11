using System;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using ChartForgeX.Svg;

namespace ChartForgeX.VisualBlocks;

public sealed partial class SvgVisualBlockRenderer {
    private static void RenderFishboneDiagram(SvgMarkupWriter writer, FishboneDiagramBlock block) {
        var options = block.Options;
        var theme = options.Theme;
        var content = VisualBlockRendering.ContentRect(options);
        var y = content.Y;
        RenderBlockHeading(writer, block, ref y, content.X, content.Width);
        var layout = VisualBlockRendering.BuildFishboneLayout(block, content, y, options.Padding.Bottom, options.Size.Height);
        var lineColor = theme.Axis;

        writer.StartElement("line").Attribute("data-cfx-role", "fishbone-spine").Attribute("x1", F(layout.SpineStartX)).Attribute("y1", F(layout.SpineY)).Attribute("x2", F(layout.SpineEndX)).Attribute("y2", F(layout.SpineY)).Attribute("stroke", lineColor.ToCss()).Attribute("stroke-width", 2.4).Attribute("stroke-linecap", "round").EndEmptyElement().Line();
        foreach (var branch in layout.Branches) WriteFishboneBranch(writer, branch, theme);
        writer.StartElement("rect").Attribute("data-cfx-role", "fishbone-effect").Attribute("x", F(layout.Head.X)).Attribute("y", F(layout.Head.Y)).Attribute("width", F(layout.Head.Width)).Attribute("height", F(layout.Head.Height)).Attribute("rx", F(Math.Min(16, layout.Head.Height * 0.28))).Attribute("fill", theme.PlotBackground.ToCss()).Attribute("stroke", VisualBlockRendering.PaletteAt(theme, 0).ToCss()).Attribute("stroke-width", 2).EndEmptyElement().Line();
        WriteText(writer, block.Effect, layout.Head.X + 10, layout.Head.Y + layout.Head.Height * 0.55, layout.Head.Width - 20, VisualTextAlignment.Center, theme.Text, theme.FontFamily, Math.Max(11, theme.SubtitleFontSize + 1), "800");
    }

    private static void WriteFishboneBranch(SvgMarkupWriter writer, VisualBlockRendering.FishboneBranchPlacement branch, ChartForgeX.Themes.ChartTheme theme) {
        var color = VisualBlockRendering.PaletteAt(theme, branch.Index);
        writer.StartElement("line").Attribute("data-cfx-role", "fishbone-branch").Attribute("x1", F(branch.BaseX)).Attribute("y1", F(branch.BaseY)).Attribute("x2", F(branch.EndX)).Attribute("y2", F(branch.EndY)).Attribute("stroke", color.ToCss()).Attribute("stroke-width", 2).Attribute("stroke-linecap", "round").EndEmptyElement().Line();
        WriteText(writer, branch.Cause.Label, branch.LabelX, branch.LabelY, branch.LabelWidth, VisualTextAlignment.Center, theme.Text, theme.FontFamily, Math.Max(10, theme.SubtitleFontSize), "750");
        for (var index = 0; index < branch.Children.Count; index++) {
            var child = branch.Children[index];
            writer.StartElement("line").Attribute("data-cfx-role", "fishbone-subcause-line").Attribute("data-depth", child.Depth).Attribute("x1", F(child.AnchorX)).Attribute("y1", F(child.AnchorY)).Attribute("x2", F(child.EndX)).Attribute("y2", F(child.EndY)).Attribute("stroke", theme.Axis.WithAlpha(145).ToCss()).Attribute("stroke-width", Math.Max(1, 1.5 - child.Depth * 0.08)).Attribute("stroke-linecap", "round").EndEmptyElement().Line();
            WriteText(writer, child.Cause.Label, child.LabelX, child.LabelY, child.LabelWidth, VisualTextAlignment.Right, theme.MutedText, theme.FontFamily, Math.Max(8.5, theme.SubtitleFontSize - child.Depth), "600");
        }
    }
}
