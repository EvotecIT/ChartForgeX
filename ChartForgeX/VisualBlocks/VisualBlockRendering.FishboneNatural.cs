using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.VisualBlocks;

internal static partial class VisualBlockRendering {
    // Fixed, complete caption cells occupy one lane per source occurrence. Connector gutters
    // are outside those cells; a descendant connects to its real parent's rail, not the spine.
    internal static FishboneNaturalPlan MeasureFishboneNatural(FishboneDiagramBlock block) {
        ValidateFishboneSource(block);
        var theme = block.Options.ResolvedTheme;
        var pairs = new List<FishboneNaturalPair>();
        var gap = Math.Max(10, BodyFontSize(theme, 11.5) * .6);
        var gutter = Math.Max(14, BodyFontSize(theme, 11.5));
        var width = 0.0; var above = 24.0; var below = 24.0;
        for (var index = 0; index < block.Causes.Count; index += 2) {
            var upper = MeasureFishboneTree(block.Causes[index], "cause-" + index.ToString(CultureInfo.InvariantCulture), null, 0, theme, gap);
            var lower = index + 1 < block.Causes.Count
                ? MeasureFishboneTree(block.Causes[index + 1], "cause-" + (index + 1).ToString(CultureInfo.InvariantCulture), null, 0, theme, gap) : null;
            var column = Math.Max(upper.Width, lower?.Width ?? 0);
            var depth = Math.Max(upper.Depth, lower?.Depth ?? 0);
            var pairWidth = column + gap * 2 + gutter * (depth + 2);
            pairs.Add(new FishboneNaturalPair(upper, lower, width, column, depth, pairWidth));
            width += pairWidth + gap * 2;
            above = Math.Max(above, upper.Height + 24);
            below = Math.Max(below, (lower?.Height ?? 0) + 24);
        }
        var effect = FishboneLabel(block.Effect, theme, 13.5, 700);
        var headWidth = effect.Width + 24; var headHeight = effect.Height + 20;
        above = Math.Max(above, headHeight / 2); below = Math.Max(below, headHeight / 2);
        var heading = FishboneNaturalHeading(block);
        var contentWidth = Math.Max(width + headWidth + 20, heading.Width);
        var padding = block.Options.Padding;
        return new FishboneNaturalPlan(pairs, effect, gap, gutter, headWidth, headHeight, above, below, heading.Height,
            new ChartSize(FishboneDimension(contentWidth + padding.Left + padding.Right),
                FishboneDimension(heading.Height + above + below + padding.Top + padding.Bottom)));
    }

    private static FishboneNaturalNode MeasureFishboneTree(FishboneCause cause, string id, string? parent, int depth,
        ResolvedVisualTheme theme, double gap) {
        var label = FishboneLabel(cause.Label, theme, depth == 0 ? 12.5 : 11.5, depth == 0 ? 700 : 600);
        var result = new FishboneNaturalNode(cause, id, parent, depth, label);
        for (var index = 0; index < cause.Children.Count; index++) {
            var child = MeasureFishboneTree(cause.Children[index], id + "." + index.ToString(CultureInfo.InvariantCulture), id, depth + 1, theme, gap);
            result.Children.Add(child); result.Height += gap + child.Height;
            result.Width = Math.Max(result.Width, child.Width); result.Depth = Math.Max(result.Depth, child.Depth);
        }
        return result;
    }

    private static FishboneNaturalLabel FishboneLabel(string text, ResolvedVisualTheme theme, double size, int weight) {
        var style = new TextStyle { Font = new FontSpec { Family = theme.FontFamily, Weight = weight }, FontSize = Math.Max(.001, Math.Round(BodyFontSize(theme, size), 3)), LineHeight = 1.2 };
        var normalized = SingleLineText(text);
        var width = Math.Max(180, Math.Max(style.FontSize * 12, DiagramWordWidth(normalized, style)));
        var layout = TextLayoutEngine.Layout(normalized, width, style, TextWrapMode.Word);
        return new FishboneNaturalLabel(normalized, style, layout, width, layout.Metrics.Height + 4);
    }

    private static (double Width, double Height) FishboneNaturalHeading(FishboneDiagramBlock block) {
        var theme = block.Options.ResolvedTheme; var width = 0.0; var height = 0.0;
        if (block.Title.Length > 0) {
            var style = new TextStyle { Font = new FontSpec { Family = theme.FontFamily, Weight = 700 }, FontSize = theme.TitleFontSize, LineHeight = 1 };
            var metrics = TextLayoutEngine.Measure(SingleLineText(block.Title), style); width = Math.Max(width, metrics.Width);
            height += metrics.Height + 7 + (block.Subtitle.Length == 0 ? 8 : 0);
        }
        if (block.Subtitle.Length > 0) {
            var style = new TextStyle { Font = new FontSpec { Family = theme.FontFamily }, FontSize = theme.SubtitleFontSize, LineHeight = 1 };
            var metrics = TextLayoutEngine.Measure(SingleLineText(block.Subtitle), style); width = Math.Max(width, metrics.Width); height += metrics.Height + 13;
        }
        return (width, height);
    }

    private static int FishboneDimension(double value) {
        if (double.IsNaN(value) || double.IsInfinity(value) || value > int.MaxValue - 1)
            throw new InvalidOperationException("The measured fishbone frame exceeds supported dimensions.");
        return Math.Max(1, (int)Math.Ceiling(value));
    }

    // Preserve the old source scene only when its actual fitted captions, cells and hierarchy fit.
    // A deeper occurrence needs real parent routing; the former flattening cannot represent it.
    internal static bool FishboneNeedsNaturalLayout(FishboneDiagramBlock block) {
        ValidateFishboneSource(block);
        var options = block.Options; var theme = options.ResolvedTheme; var content = ContentRect(options);
        var headings = Headings(block); var y = content.Top + headings.TitleAdvance + headings.SubtitleAdvance;
        var layout = BuildFishboneLayout(block, content, y, options.Padding.Bottom, options.Size.Height);
        var captions = new List<ChartRect>();
        var effectStyle = FitBodyTextStyle(theme, block.Effect, 13.5, layout.Head.Width - 20, layout.Head.Height - 16, 800);
        if (!Fits(block.Effect, layout.Head.Left + 10, BlockLayoutBaseline(effectStyle, layout.Head.Top + layout.Head.Height / 2), layout.Head.Width - 20, layout.Head.Height - 16, 13.5, 800, true)) return true;
        foreach (var branch in layout.Branches) {
            if (!PointFits(branch.EndX, branch.EndY) || !Fits(branch.Cause.Label, branch.LabelX, branch.LabelY, branch.LabelWidth, 24, 12.5, 750, true)) return true;
            foreach (var child in branch.Children) {
                if (child.Depth > 1 || !PointFits(child.EndX, child.EndY)
                    || !Fits(child.Cause.Label, child.LabelX, child.LabelY, child.LabelWidth, 20, Math.Max(8.5, 12.5 - child.Depth), 600, false)) return true;
            }
        }
        return false;

        bool PointFits(double x, double py) => x >= content.Left && x <= content.Right && py >= y && py <= content.Bottom;
        bool Fits(string text, double x, double baseline, double cellWidth, double cellHeight, double size, int weight, bool center) {
            var style = FitBodyTextStyle(theme, text, size, cellWidth, cellHeight, weight);
            var full = SingleLineText(text);
            if (FitMeasuredText(full, style.FontSize, cellWidth, theme.FontFamily, weight) != full) return false;
            var metrics = TextLayoutEngine.Measure(full, style);
            var left = x + (center ? (cellWidth - metrics.Width) / 2 : cellWidth - metrics.Width);
            var top = baseline - TextAscent(style);
            var rect = new ChartRect(left, top, metrics.Width, metrics.Height);
            if (rect.Left < content.Left || rect.Right > content.Right || rect.Top < y || rect.Bottom > content.Bottom) return false;
            foreach (var previous in captions) if (rect.Left < previous.Right + 4 && rect.Right + 4 > previous.Left
                && rect.Top < previous.Bottom + 4 && rect.Bottom + 4 > previous.Top) return false;
            captions.Add(rect); return true;
        }
    }

    internal sealed class FishboneNaturalPlan {
        internal FishboneNaturalPlan(IReadOnlyList<FishboneNaturalPair> pairs, FishboneNaturalLabel effect, double gap, double gutter,
            double headWidth, double headHeight, double above, double below, double headingHeight, ChartSize size) {
            Pairs = pairs; Effect = effect; Gap = gap; Gutter = gutter; HeadWidth = headWidth; HeadHeight = headHeight;
            Above = above; Below = below; HeadingHeight = headingHeight; Size = size;
        }
        internal IReadOnlyList<FishboneNaturalPair> Pairs { get; }
        internal FishboneNaturalLabel Effect { get; }
        internal double Gap { get; } internal double Gutter { get; }
        internal double HeadWidth { get; } internal double HeadHeight { get; }
        internal double Above { get; } internal double Below { get; } internal double HeadingHeight { get; }
        internal ChartSize Size { get; }
    }
    internal sealed class FishboneNaturalPair {
        internal FishboneNaturalPair(FishboneNaturalNode upper, FishboneNaturalNode? lower, double x, double column, int depth, double width) {
            Upper = upper; Lower = lower; X = x; Column = column; Depth = depth; Width = width;
        }
        internal FishboneNaturalNode Upper { get; } internal FishboneNaturalNode? Lower { get; }
        internal double X { get; } internal double Column { get; } internal int Depth { get; } internal double Width { get; }
    }
    internal sealed class FishboneNaturalNode {
        internal FishboneNaturalNode(FishboneCause cause, string id, string? parentId, int depth, FishboneNaturalLabel label) {
            Cause = cause; Id = id; ParentId = parentId; OccurrenceDepth = depth; Depth = depth;
            Label = label; Width = label.Width; Height = label.Height;
        }
        internal FishboneCause Cause { get; } internal string Id { get; } internal string? ParentId { get; }
        internal int OccurrenceDepth { get; } internal int Depth { get; set; }
        internal FishboneNaturalLabel Label { get; } internal double Width { get; set; } internal double Height { get; set; }
        internal List<FishboneNaturalNode> Children { get; } = new();
    }
    internal sealed class FishboneNaturalLabel {
        internal FishboneNaturalLabel(string text, TextStyle style, TextLayout layout, double width, double height) {
            Text = text; Style = style; Layout = layout; Width = width; Height = height;
        }
        internal string Text { get; } internal TextStyle Style { get; } internal TextLayout Layout { get; }
        internal double Width { get; } internal double Height { get; }
    }
}
