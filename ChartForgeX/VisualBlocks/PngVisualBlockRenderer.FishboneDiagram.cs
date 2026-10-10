using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;

namespace ChartForgeX.VisualBlocks;

public sealed partial class PngVisualBlockRenderer {
    private static void DrawFishboneDiagram(RgbaCanvas canvas, FishboneDiagramBlock block) {
        var options = block.Options;
        var theme = options.Theme;
        var content = VisualBlockRendering.ContentRect(options);
        var y = content.Y;
        DrawHeading(canvas, block, ref y, content.X, content.Width);
        var layout = VisualBlockRendering.BuildFishboneLayout(block, content, y, options.Padding.Bottom, options.Size.Height);

        canvas.DrawLine(layout.SpineStartX, layout.SpineY, layout.SpineEndX, layout.SpineY, theme.Axis, 2.4);
        foreach (var branch in layout.Branches) DrawFishboneBranch(canvas, branch, theme);
        canvas.FillRoundedRect(layout.Head.X, layout.Head.Y, layout.Head.Width, layout.Head.Height, Math.Min(16, layout.Head.Height * 0.28), theme.PlotBackground);
        canvas.StrokeRoundedRect(layout.Head.X, layout.Head.Y, layout.Head.Width, layout.Head.Height, Math.Min(16, layout.Head.Height * 0.28), VisualBlockRendering.PaletteAt(theme, 0), 2);
        DrawCenteredTextMiddle(canvas, block.Effect, layout.Head.X + layout.Head.Width / 2, layout.Head.Y + layout.Head.Height / 2, VisualBlockRendering.BodyFontSize(theme, 13.5), theme.Text, true, layout.Head.Width - 20);
    }

    private static void DrawFishboneBranch(RgbaCanvas canvas, VisualBlockRendering.FishboneBranchPlacement branch, ChartForgeX.Themes.ChartTheme theme) {
        var color = VisualBlockRendering.PaletteAt(theme, branch.Index);
        canvas.DrawLine(branch.BaseX, branch.BaseY, branch.EndX, branch.EndY, color, 2);
        DrawCenteredTextMiddle(canvas, branch.Cause.Label, branch.LabelX + branch.LabelWidth / 2, branch.LabelY, VisualBlockRendering.BodyFontSize(theme, 12.5), theme.Text, true, branch.LabelWidth);
        for (var index = 0; index < branch.Children.Count; index++) {
            var child = branch.Children[index];
            canvas.DrawLine(child.AnchorX, child.AnchorY, child.EndX, child.EndY, theme.Axis.WithAlpha(145), Math.Max(1, 1.5 - child.Depth * 0.08));
            DrawAlignedTextAtBaseline(canvas, child.Cause.Label, child.LabelX, child.LabelY, child.LabelWidth, TextAlignment.Right, theme.MutedText, VisualBlockRendering.BodyFontSize(theme, Math.Max(8.5, 12.5 - child.Depth)), true);
        }
    }
}
