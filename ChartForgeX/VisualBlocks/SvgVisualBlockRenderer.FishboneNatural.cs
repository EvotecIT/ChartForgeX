using System;
using ChartForgeX.Primitives;
using ChartForgeX.Svg;
using ChartForgeX.Themes;
using ChartForgeX.Core;
using ChartForgeX.Typography;

namespace ChartForgeX.VisualBlocks;

public sealed partial class SvgVisualBlockRenderer {
    private static void RenderNaturalFishbone(SvgMarkupWriter writer, FishboneDiagramBlock block) {
        var plan = VisualBlockRendering.MeasureFishboneNatural(block); var options = block.Options; var theme = options.ResolvedTheme;
        var content = VisualBlockRendering.ContentRect(options); var y = content.Top;
        if (block.Title.Length > 0) NaturalHeading(block.Title, theme.TitleFontSize, 700, theme.Text, "visual-title", 7 + (block.Subtitle.Length == 0 ? 8 : 0));
        if (block.Subtitle.Length > 0) NaturalHeading(block.Subtitle, theme.SubtitleFontSize, 400, theme.MutedText, "visual-subtitle", 13);
        void NaturalHeading(string value, double size, int weight, ChartColor color, string role, double gap) {
            var style = new TextStyle { Font = new FontSpec { Family = theme.FontFamily, Weight = weight }, FontSize = size, LineHeight = 1 };
            WriteText(writer, value, content.Left, y + VisualBlockRendering.TextAscent(style), content.Width, TextAlignment.Left, color, theme.FontFamily, size, weight.ToString(System.Globalization.CultureInfo.InvariantCulture), role);
            y += TextLayoutEngine.Measure(VisualBlockRendering.SingleLineText(value), style).Height + gap;
        }
        var spineY = content.Top + plan.HeadingHeight + plan.Above;
        var head = new ChartRect(content.Right - plan.HeadWidth, spineY - plan.HeadHeight / 2, plan.HeadWidth, plan.HeadHeight);
        writer.StartElement("line").Attribute("data-cfx-role", "fishbone-spine").Attribute("x1", F(content.Left)).Attribute("y1", F(spineY))
            .Attribute("x2", F(head.Left - 10)).Attribute("y2", F(spineY)).Attribute("stroke", theme.Axis.ToCss()).Attribute("stroke-width", 2.4).Attribute("stroke-linecap", "round").EndEmptyElement().Line();
        for (var index = 0; index < plan.Pairs.Count; index++) {
            var pair = plan.Pairs[index]; var x = head.Left - 20 - pair.X - pair.Width;
            var rail = x + pair.Column + plan.Gap + pair.Depth * plan.Gutter;
            NaturalFishboneNode(writer, pair.Upper, x, spineY - 24 - pair.Upper.Height, 1, rail, null, plan, theme, index * 2);
            NaturalFishboneRoot(writer, rail, spineY - 24 - pair.Upper.Height + pair.Upper.Label.Height / 2, rail + plan.Gutter, spineY, theme, index * 2);
            if (pair.Lower != null) {
                NaturalFishboneNode(writer, pair.Lower, x, spineY + 24 + pair.Lower.Height, -1, rail, null, plan, theme, index * 2 + 1);
                NaturalFishboneRoot(writer, rail, spineY + 24 + pair.Lower.Height - pair.Lower.Label.Height / 2, rail + plan.Gutter, spineY, theme, index * 2 + 1);
            }
        }
        writer.StartElement("rect").Attribute("data-cfx-role", "fishbone-effect").Attribute("x", F(head.X)).Attribute("y", F(head.Y))
            .Attribute("width", F(head.Width)).Attribute("height", F(head.Height)).Attribute("rx", F(Math.Min(16, head.Height * .28)))
            .Attribute("fill", theme.PlotBackground.ToCss()).Attribute("stroke", theme.Axis.ToCss()).Attribute("stroke-width", 2).EndEmptyElement().Line();
        NaturalFishboneLabel(writer, plan.Effect, new ChartRect(head.Left + 12, head.Top + 10, plan.Effect.Width, plan.Effect.Height), theme, "fishbone-effect-caption");
    }

    private static void NaturalFishboneRoot(SvgMarkupWriter writer, double x, double y, double baseX, double spineY, ResolvedVisualTheme theme, int index) =>
        writer.StartElement("line").Attribute("data-cfx-role", "fishbone-branch").Attribute("x1", F(baseX)).Attribute("y1", F(spineY)).Attribute("x2", F(x)).Attribute("y2", F(y))
            .Attribute("stroke", VisualBlockRendering.PaletteAt(theme, index).ToCss()).Attribute("stroke-width", 2).Attribute("stroke-linecap", "round").EndEmptyElement().Line();

    private static void NaturalFishboneNode(SvgMarkupWriter writer, VisualBlockRendering.FishboneNaturalNode node, double x, double edgeY, int direction,
        double railX, double? parentRailX, VisualBlockRendering.FishboneNaturalPlan plan, ResolvedVisualTheme theme, int index) {
        var top = direction > 0 ? edgeY : edgeY - node.Label.Height;
        var centerY = top + node.Label.Height / 2; var bounds = new ChartRect(x, top, node.Label.Width, node.Label.Height);
        var lastChildY = edgeY + direction * (node.Label.Height + plan.Gap);
        for (var childIndex = 0; childIndex + 1 < node.Children.Count; childIndex++)
            lastChildY += direction * (node.Children[childIndex].Height + plan.Gap);
        if (node.Children.Count > 0) lastChildY += direction * node.Children[node.Children.Count - 1].Label.Height / 2;
        writer.StartElement("g").Attribute("data-cfx-role", "fishbone-cause").Attribute("data-cause-id", node.Id).Attribute("data-parent-id", node.ParentId)
            .Attribute("data-depth", node.OccurrenceDepth + 1).Attribute("data-caption-x", F(bounds.X)).Attribute("data-caption-y", F(bounds.Y))
            .Attribute("data-caption-width", F(bounds.Width)).Attribute("data-caption-height", F(bounds.Height)).EndStartElement();
        writer.StartElement("path").Attribute("data-cfx-role", "fishbone-subcause-line").Attribute("data-parent-id", node.ParentId)
            .Attribute("d", new SvgPathDataBuilder().MoveTo(x + node.Label.Width + 4, centerY).LineTo(parentRailX ?? railX, centerY).Build())
            .Attribute("fill", "none").Attribute("stroke", VisualBlockRendering.PaletteAt(theme, index).ToCss()).Attribute("stroke-width", 1.4).EndEmptyElement().Line();
        if (node.Children.Count > 0) writer.StartElement("line").Attribute("data-cfx-role", "fishbone-parent-rail").Attribute("data-cause-id", node.Id)
            .Attribute("x1", F(railX)).Attribute("x2", F(railX)).Attribute("y1", F(centerY)).Attribute("y2", F(lastChildY))
            .Attribute("stroke", VisualBlockRendering.PaletteAt(theme, index).ToCss()).Attribute("stroke-width", 1.4).EndEmptyElement().Line();
        NaturalFishboneLabel(writer, node.Label, bounds, theme, "fishbone-cause-caption"); writer.EndElement().Line();
        var next = edgeY + direction * (node.Label.Height + plan.Gap);
        foreach (var child in node.Children) {
            NaturalFishboneNode(writer, child, x, next, direction, railX - plan.Gutter, railX, plan, theme, index);
            next += direction * (child.Height + plan.Gap);
        }
    }

    private static void NaturalFishboneLabel(SvgMarkupWriter writer, VisualBlockRendering.FishboneNaturalLabel label, ChartRect bounds, ResolvedVisualTheme theme, string role) {
        var baselineStyle = new TextStyle { Font = label.Style.Font, FontSize = label.Style.FontSize, LineHeight = 1 };
        var y = bounds.Top + 2 + VisualBlockRendering.TextAscent(baselineStyle);
        writer.StartElement("g").Attribute("data-cfx-role", role).EndStartElement(); writer.StartElement("title").Text(label.Text).EndElement();
        foreach (var line in label.Layout.Lines) {
            writer.StartElement("text").Attribute("x", F(bounds.Left)).Attribute("y", F(y)).Attribute("font-family", theme.FontFamily)
                .Attribute("font-size", F(label.Style.FontSize)).Attribute("font-weight", label.Style.Font.Weight).Attribute("fill", theme.Text.ToCss()).Text(line.Text).EndElement();
            y += label.Layout.Metrics.LineHeight;
        }
        writer.EndElement().Line();
    }
}
