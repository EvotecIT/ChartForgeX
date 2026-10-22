using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Primitives;
using ChartForgeX.Svg;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.VisualBlocks;

public sealed partial class SvgVisualBlockRenderer {
    private static void RenderRailroadDiagram(SvgMarkupWriter writer, RailroadDiagramBlock block, string id) {
        var layout = VisualBlockRendering.RailroadLayout(block); var theme = block.Options.ResolvedTheme;
        var content = VisualBlockRendering.ContentRect(block.Options); var y = content.Top;
        if (block.Title.Length > 0) {
            WriteText(writer, VisualBlockRendering.RailroadSafeLabel(block.Title), content.Left, y + VisualBlockRendering.TextAscent(layout.Heading.Title), content.Width, TextAlignment.Left, theme.Text, theme.FontFamily, layout.Heading.Title.FontSize, "700", "visual-title");
            y += layout.Heading.TitleAdvance;
        }
        if (block.Subtitle.Length > 0) {
            WriteText(writer, VisualBlockRendering.RailroadSafeLabel(block.Subtitle), content.Left, y + VisualBlockRendering.TextAscent(layout.Heading.Subtitle), content.Width, TextAlignment.Left, theme.MutedText, theme.FontFamily, layout.Heading.Subtitle.FontSize, "400", "visual-subtitle");
            y += layout.Heading.SubtitleAdvance;
        }
        for (var index = 0; index < block.Rules.Count; index++) {
            var rule = block.Rules[index]; var plan = layout.Rules[index]; var headingHeight = layout.Style.FontSize * 1.3 + 12;
            writer.StartElement("g").Attribute("id", id + "-grammar-rule-" + index.ToString(CultureInfo.InvariantCulture)).Attribute("data-cfx-role", "railroad-rule").Attribute("data-rule-id", VisualBlockRendering.RailroadSafeLabel(rule.Id)).Attribute("data-rule-index", index).EndStartElement();
            WriteText(writer, VisualBlockRendering.RailroadSafeLabel(rule.Label), content.Left, y + VisualBlockRendering.TextAscent(layout.Style), content.Width, TextAlignment.Left, theme.Text, theme.FontFamily, layout.Style.FontSize, "700", "railroad-rule-label");
            var baseline = y + headingHeight + plan.Above; var x = content.Left;
            RailroadRail(writer, new SvgPathDataBuilder().MoveTo(x + 4, baseline).LineTo(x + 28, baseline).Build(), theme);
            RailroadNode(writer, plan, x + 28, baseline, block, id, layout.Style);
            RailroadRail(writer, new SvgPathDataBuilder().MoveTo(x + 28 + plan.Width, baseline).LineTo(x + plan.Width + 52, baseline).Build(), theme);
            foreach (var markerX in new[] { x + 4, x + plan.Width + 52 }) writer.StartElement("circle").Attribute("data-cfx-role", "railroad-rule-marker").Attribute("cx", F(markerX)).Attribute("cy", F(baseline)).Attribute("r", 3.5).Attribute("fill", theme.Text.ToCss()).EndEmptyElement().Line();
            RailroadArrow(writer, x + 21, baseline, 1, theme);
            writer.EndElement().Line(); y += headingHeight + plan.Above + plan.Below + 32;
        }
    }

    private static void RailroadNode(SvgMarkupWriter writer, VisualBlockRendering.RailroadNodePlan plan, double x, double baseline,
        RailroadDiagramBlock block, string id, TextStyle style) {
        var expression = plan.Expression; var theme = block.Options.ResolvedTheme;
        writer.StartElement("g").Attribute("data-cfx-role", "railroad-expression").Attribute("data-expression-kind", expression.Kind.ToString())
            .Attribute("data-minimum", expression.Kind == RailroadExpressionKind.Repeat ? expression.MinimumOccurrences.ToString(CultureInfo.InvariantCulture) : null)
            .Attribute("data-maximum", expression.Kind == RailroadExpressionKind.Repeat ? expression.MaximumOccurrences?.ToString(CultureInfo.InvariantCulture) ?? "unbounded" : null)
            .Attribute("data-ordered-choice", expression.Kind == RailroadExpressionKind.Choice ? expression.OrderedChoice ? "true" : "false" : null)
            .Attribute("data-consuming", expression.Kind is RailroadExpressionKind.Lookahead or RailroadExpressionKind.NegativeLookahead ? "false" : null).EndStartElement();
        switch (expression.Kind) {
            case RailroadExpressionKind.Sequence:
                if (plan.Children.Count == 0) RailroadRail(writer, Line(x, baseline, x + plan.Width, baseline), theme);
                for (var index = 0; index < plan.Children.Count; index++) {
                    var child = plan.Children[index];
                    if (index > 0) { RailroadRail(writer, Line(x + plan.Children[index - 1].X + plan.Children[index - 1].Node.Width, baseline, x + child.X, baseline), theme); RailroadArrow(writer, x + child.X - 5, baseline, 1, theme); }
                    RailroadNode(writer, child.Node, x + child.X, baseline, block, id, style);
                }
                break;
            case RailroadExpressionKind.Choice:
                for (var index = 0; index < plan.Children.Count; index++) {
                    var child = plan.Children[index]; Branch(child);
                    if (expression.OrderedChoice) WriteText(writer, (index + 1).ToString(CultureInfo.InvariantCulture), x + plan.Bend / 2, baseline + child.Baseline - 4, plan.Bend, TextAlignment.Center, theme.MutedText, theme.FontFamily, Math.Max(8, style.FontSize * .7), "600", "railroad-choice-priority");
                }
                break;
            case RailroadExpressionKind.Optional:
                RailroadRail(writer, Line(x, baseline, x + plan.Width, baseline), theme); Branch(plan.Children[0]); break;
            case RailroadExpressionKind.Repeat:
                if (expression.MaximumOccurrences == 0) {
                    RailroadRail(writer, Line(x, baseline, x + plan.Width, baseline), theme);
                    WriteText(writer, plan.Label, x, baseline - 8, plan.Width, TextAlignment.Center, theme.MutedText, theme.FontFamily, style.FontSize, "500", "railroad-repeat-constraint"); break;
                }
                var repeated = plan.Children[0];
                if (expression.MinimumOccurrences == 0) RailroadRail(writer, Line(x, baseline, x + plan.Width, baseline), theme);
                Branch(repeated); var childBaseline = baseline + repeated.Baseline;
                var loopY = childBaseline + repeated.Node.Below + plan.Gap;
                var start = x + repeated.X; var end = start + repeated.Node.Width;
                var loop = new SvgPathDataBuilder().MoveTo(end, childBaseline).CubicTo(end + plan.Bend, childBaseline, end + plan.Bend, loopY, end, loopY)
                    .LineTo(start, loopY).CubicTo(start - plan.Bend, loopY, start - plan.Bend, childBaseline, start, childBaseline).Build();
                RailroadRail(writer, loop, theme, "railroad-repeat-loop"); RailroadArrow(writer, (start + end) / 2, loopY, -1, theme);
                WriteText(writer, VisualBlockRendering.RailroadRepeatLabel(expression), x, loopY + style.FontSize + 4, plan.Width, TextAlignment.Center, theme.MutedText, theme.FontFamily, style.FontSize, "500", "railroad-repeat-constraint");
                break;
            case RailroadExpressionKind.Lookahead:
            case RailroadExpressionKind.NegativeLookahead:
                var predicate = plan.Children[0]; var predicateBaseline = baseline + predicate.Baseline;
                var frameTop = predicateBaseline - predicate.Node.Above - style.FontSize * 1.25 - 8;
                var frameBottom = predicateBaseline + predicate.Node.Below + 5;
                writer.StartElement("rect").Attribute("data-cfx-role", "railroad-predicate-frame").Attribute("x", F(x + 3)).Attribute("y", F(frameTop)).Attribute("width", F(plan.Width - 6)).Attribute("height", F(frameBottom - frameTop)).Attribute("rx", 5).Attribute("fill", "none").Attribute("stroke", theme.MutedText.ToCss()).Attribute("stroke-dasharray", "4 3").EndEmptyElement().Line();
                WriteText(writer, plan.Label, x + 6, frameTop + style.FontSize + 2, plan.Width - 12, TextAlignment.Center, theme.MutedText, theme.FontFamily, style.FontSize, "500", "railroad-predicate-label");
                RailroadNode(writer, predicate.Node, x + predicate.X, predicateBaseline, block, id, style);
                RailroadRail(writer, Line(x, baseline, x + plan.Width, baseline), theme);
                RailroadRail(writer, Line(x + plan.Width / 2, baseline - 3, x + plan.Width / 2, frameBottom), theme, "railroad-predicate-leader", true);
                break;
            case RailroadExpressionKind.Except:
                Branch(plan.Children[0]); var excluded = plan.Children[1]; var excludedBaseline = baseline + excluded.Baseline;
                WriteText(writer, "except (not an alternative)", x, excludedBaseline - excluded.Node.Above - 8, plan.Width, TextAlignment.Center, theme.MutedText, theme.FontFamily, style.FontSize, "500", "railroad-exception-label");
                RailroadNode(writer, excluded.Node, x + excluded.X, excludedBaseline, block, id, style);
                RailroadRail(writer, Line(x + plan.Width / 2, baseline + plan.Children[0].Node.Below + 3, x + plan.Width / 2, excludedBaseline - excluded.Node.Above - style.FontSize - 9), theme, "railroad-exception-leader", true);
                break;
            case RailroadExpressionKind.Empty:
                RailroadRail(writer, Line(x, baseline, x + plan.Width, baseline), theme); break;
            default:
                RailroadLeaf(writer, plan, x, baseline, block, id, style); break;
        }
        writer.EndElement().Line();

        void Branch(VisualBlockRendering.RailroadChildPlan child) {
            var cy = baseline + child.Baseline; var cx = x + child.X;
            RailroadRail(writer, new SvgPathDataBuilder().MoveTo(x, baseline).CubicTo(x + plan.Bend, baseline, cx - plan.Bend, cy, cx, cy).Build(), theme);
            RailroadNode(writer, child.Node, cx, cy, block, id, style);
            RailroadRail(writer, new SvgPathDataBuilder().MoveTo(cx + child.Node.Width, cy).CubicTo(cx + child.Node.Width + plan.Bend, cy, x + plan.Width - plan.Bend, baseline, x + plan.Width, baseline).Build(), theme);
            RailroadArrow(writer, cx - 4, cy, 1, theme);
        }
    }

    private static void RailroadLeaf(SvgMarkupWriter writer, VisualBlockRendering.RailroadNodePlan plan, double x, double baseline, RailroadDiagramBlock block, string id, TextStyle style) {
        var expression = plan.Expression; var theme = block.Options.ResolvedTheme; var reference = expression.Kind == RailroadExpressionKind.Reference;
        var target = -1; var comparer = block.RuleComparison == RailroadRuleComparison.OrdinalIgnoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        if (reference) for (var index = 0; index < block.Rules.Count; index++) if (comparer.Equals(block.Rules[index].Id, expression.Value)) { target = index; break; }
        writer.StartElement(target < 0 ? "g" : "a").Attribute("href", target < 0 ? null : "#" + id + "-grammar-rule-" + target.ToString(CultureInfo.InvariantCulture))
            .Attribute("data-cfx-role", reference ? "railroad-reference" : expression.Kind == RailroadExpressionKind.Special ? "railroad-special" : "railroad-terminal")
            .Attribute("data-reference-scope", reference ? target < 0 ? "external" : "local" : null)
            .Attribute("data-reference-id", reference ? VisualBlockRendering.RailroadSafeLabel(expression.Value) : null)
            .Attribute("data-case-sensitive", expression.Kind == RailroadExpressionKind.Terminal ? expression.CaseSensitive ? "true" : "false" : null)
            .Attribute("data-first-code-point", expression.Kind == RailroadExpressionKind.CharacterRange ? expression.FirstCodePoint.ToString(CultureInfo.InvariantCulture) : null)
            .Attribute("data-last-code-point", expression.Kind == RailroadExpressionKind.CharacterRange ? expression.LastCodePoint.ToString(CultureInfo.InvariantCulture) : null).EndStartElement();
        writer.StartElement("rect").Attribute("x", F(x)).Attribute("y", F(baseline - plan.Above)).Attribute("width", F(plan.Width)).Attribute("height", F(plan.Above + plan.Below))
            .Attribute("rx", reference ? 2 : 10).Attribute("fill", theme.CardBackground.ToCss()).Attribute("stroke", theme.Text.ToCss())
            .Attribute("stroke-dasharray", expression.Kind == RailroadExpressionKind.Special ? "4 3" : null).EndEmptyElement().Line();
        var metrics = TextLayoutEngine.Measure(plan.Label, style);
        WriteText(writer, plan.Label, x + 12, baseline - metrics.Height / 2 + VisualBlockRendering.TextAscent(style), plan.Width - 24, TextAlignment.Center, theme.Text, theme.FontFamily, style.FontSize, "500", "railroad-symbol-label");
        writer.EndElement().Line();
    }
    private static string Line(double x1, double y1, double x2, double y2) => new SvgPathDataBuilder().MoveTo(x1, y1).LineTo(x2, y2).Build();
    private static void RailroadRail(SvgMarkupWriter writer, string path, ResolvedVisualTheme theme, string role = "railroad-rail", bool dashed = false) =>
        writer.StartElement("path").Attribute("data-cfx-role", role).Attribute("d", path).Attribute("fill", "none").Attribute("stroke", theme.Text.ToCss()).Attribute("stroke-width", 1.5).Attribute("stroke-dasharray", dashed ? "3 3" : null).EndEmptyElement().Line();
    private static void RailroadArrow(SvgMarkupWriter writer, double x, double y, int direction, ResolvedVisualTheme theme) =>
        writer.StartElement("path").Attribute("data-cfx-role", "railroad-arrow").Attribute("d", new SvgPathDataBuilder().MoveTo(x - direction * 4, y - 3).LineTo(x, y).LineTo(x - direction * 4, y + 3).Build()).Attribute("fill", "none").Attribute("stroke", theme.Text.ToCss()).Attribute("stroke-width", 1.5).EndEmptyElement().Line();
}
