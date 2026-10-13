using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;

namespace ChartForgeX.VisualBlocks;

public sealed partial class PngVisualBlockRenderer {
    private static void DrawFishboneDiagram(RgbaCanvas canvas, FishboneDiagramBlock block) {
        var options = block.Options;
        var theme = options.ResolvedTheme;
        var content = VisualBlockRendering.ContentRect(options);
        var y = content.Y;
        DrawHeading(canvas, block, ref y, content.X, content.Width);
        var layout = VisualBlockRendering.BuildFishboneLayout(block, content, y, options.Padding.Bottom, options.Size.Height);

        canvas.DrawLine(layout.SpineStartX, layout.SpineY, layout.SpineEndX, layout.SpineY, theme.Axis, 2.4);
        foreach (var branch in layout.Branches) DrawFishboneBranch(canvas, branch, theme);
        canvas.FillRoundedRect(layout.Head.X, layout.Head.Y, layout.Head.Width, layout.Head.Height, Math.Min(16, layout.Head.Height * 0.28), theme.PlotBackground);
        canvas.StrokeRoundedRect(layout.Head.X, layout.Head.Y, layout.Head.Width, layout.Head.Height, Math.Min(16, layout.Head.Height * 0.28), VisualBlockRendering.PaletteAt(theme, 0), 2);
        var effectStyle = VisualBlockRendering.FitBodyTextStyle(theme, block.Effect, 13.5, layout.Head.Width - 20, layout.Head.Height - 16, 800);
        DrawFittedBodyText(canvas, block.Effect, layout.Head.X + 10, VisualBlockRendering.BlockLayoutBaseline(effectStyle, layout.Head.Y + layout.Head.Height / 2), layout.Head.Width - 20, layout.Head.Height - 16, TextAlignment.Center, theme.Text, theme, 13.5, 800);
    }

    private static void DrawFishboneBranch(RgbaCanvas canvas, VisualBlockRendering.FishboneBranchPlacement branch, ChartForgeX.Themes.ResolvedVisualTheme theme) {
        var color = VisualBlockRendering.PaletteAt(theme, branch.Index);
        canvas.DrawLine(branch.BaseX, branch.BaseY, branch.EndX, branch.EndY, color, 2);
        DrawFittedBodyText(canvas, branch.Cause.Label, branch.LabelX, branch.LabelY, branch.LabelWidth, 24, TextAlignment.Center, theme.Text, theme, 12.5, 750);
        for (var index = 0; index < branch.Children.Count; index++) {
            var child = branch.Children[index];
            canvas.DrawLine(child.AnchorX, child.AnchorY, child.EndX, child.EndY, theme.Axis.WithAlpha(145), Math.Max(1, 1.5 - child.Depth * 0.08));
            DrawFittedBodyText(canvas, child.Cause.Label, child.LabelX, child.LabelY, child.LabelWidth, 20, TextAlignment.Right, theme.MutedText, theme, Math.Max(8.5, 12.5 - child.Depth), 600);
        }
    }
}
