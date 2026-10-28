using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.VisualBlocks;

internal static partial class VisualBlockRendering {
    public static void ValidateRailroadDiagram(RailroadDiagramBlock block) {
        var layout = RailroadLayout(block);
        if (block.Options.Size.Width < layout.Size.Width || block.Options.Size.Height < layout.Size.Height)
            throw new InvalidOperationException("The railroad frame is smaller than its measured grammar. Call WithNaturalSize after changing its rules or typography.");
    }

    internal static RailroadLayoutPlan RailroadLayout(RailroadDiagramBlock block) {
        if (block.Rules.Count == 0 || block.Rules.Count > RailroadDiagramLimits.MaximumRules) throw new InvalidOperationException("Railroad diagrams require one through 512 rules.");
        var comparer = block.RuleComparison == RailroadRuleComparison.OrdinalIgnoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        var ids = new HashSet<string>(comparer); var count = 0; var theme = block.Options.ResolvedTheme;
        var style = RailroadTextStyle(theme); var ruleHeading = RailroadTextStyle(theme, 700); var heading = RailroadHeading(block);
        var plans = new List<RailroadNodePlan>(); var maxWidth = Math.Max(1, heading.Width);
        var height = heading.Advance;
        foreach (var rule in block.Rules) {
            if (!ids.Add(rule.Id)) throw new InvalidOperationException("Duplicate railroad rule identity: " + rule.Id + ".");
            var plan = MeasureRailroadNode(rule.Expression, style, ref count);
            plans.Add(plan); maxWidth = Math.Max(maxWidth, Math.Max(plan.Width + 56, TextLayoutEngine.Measure(RailroadSafeLabel(rule.Label), ruleHeading).Width));
            height += style.FontSize * 1.3 + 12 + plan.Above + plan.Below + 32;
        }
        return new RailroadLayoutPlan(plans, new ChartSize(RailroadDimension(maxWidth + block.Options.Padding.Left + block.Options.Padding.Right),
            RailroadDimension(height + block.Options.Padding.Top + block.Options.Padding.Bottom)), style, heading);
    }

    private static RailroadNodePlan MeasureRailroadNode(RailroadExpression expression, TextStyle style, ref int count) {
        if (++count > RailroadDiagramLimits.MaximumExpressionOccurrences) throw new InvalidOperationException("Railroad diagrams allow no more than 32768 expression occurrences.");
        if (expression.Depth > RailroadDiagramLimits.MaximumExpressionDepth) throw new InvalidOperationException("Railroad expression nesting must not exceed 128 levels.");
        if (expression.Children.Count > RailroadDiagramLimits.MaximumExpressionOccurrences - count) throw new InvalidOperationException("Railroad diagrams allow no more than 32768 expression occurrences.");
        var measured = new List<RailroadNodePlan>(expression.Children.Count);
        foreach (var child in expression.Children) measured.Add(MeasureRailroadNode(child, style, ref count));
        var children = measured.ToArray();
        var gap = Math.Max(18, style.FontSize * .8); var bend = Math.Max(14, style.FontSize * .6); var annotation = style.FontSize * 1.25 + 8;
        var label = RailroadLabel(expression); var metrics = TextLayoutEngine.Measure(label, style);
        var plan = new RailroadNodePlan(expression, label) { Width = Math.Max(28, metrics.Width + 28), Above = metrics.Height / 2 + 9, Below = metrics.Height / 2 + 9, Gap = gap, Bend = bend, Occurrences = 1 + children.Sum(child => child.Occurrences) };
        switch (expression.Kind) {
            case RailroadExpressionKind.Empty:
                plan.Width = 28; plan.Above = plan.Below = 8; break;
            case RailroadExpressionKind.Sequence:
                plan.Width = children.Sum(child => child.Width) + Math.Max(0, children.Length - 1) * gap;
                plan.Above = children.Length == 0 ? 8 : children.Max(child => child.Above); plan.Below = children.Length == 0 ? 8 : children.Max(child => child.Below);
                var x = 0.0; foreach (var child in children) { plan.Children.Add(new RailroadChildPlan(child, x, 0)); x += child.Width + gap; }
                if (children.Length == 0) plan.Width = 28;
                break;
            case RailroadExpressionKind.Choice:
                plan.Width = children.Max(child => child.Width) + 4 * bend;
                plan.Above = children[0].Above; var baseline = 0.0; var previousBelow = 0.0;
                for (var index = 0; index < children.Length; index++) {
                    var child = children[index]; if (index > 0) baseline += previousBelow + gap + child.Above;
                    plan.Children.Add(new RailroadChildPlan(child, (plan.Width - child.Width) / 2, baseline)); previousBelow = child.Below;
                }
                plan.Below = baseline + previousBelow; break;
            case RailroadExpressionKind.Optional:
                plan.Width = children[0].Width + 4 * bend; plan.Above = 8;
                plan.Children.Add(new RailroadChildPlan(children[0], 2 * bend, gap + children[0].Above));
                plan.Below = gap + children[0].Above + children[0].Below; break;
            case RailroadExpressionKind.Repeat:
                if (expression.MaximumOccurrences == 0) { plan.Width = Math.Max(76, metrics.Width + 28); plan.Above = annotation; plan.Below = 8; break; }
                var repeated = children[0]; var childBaseline = expression.MinimumOccurrences == 0 ? gap + repeated.Above : 0;
                plan.Width = Math.Max(repeated.Width + 4 * bend, TextLayoutEngine.Measure(RailroadRepeatLabel(expression), style).Width + 2 * bend);
                plan.Children.Add(new RailroadChildPlan(repeated, (plan.Width - repeated.Width) / 2, childBaseline));
                plan.Above = expression.MinimumOccurrences == 0 ? 8 : repeated.Above;
                plan.Below = childBaseline + repeated.Below + gap + annotation; break;
            case RailroadExpressionKind.Lookahead:
            case RailroadExpressionKind.NegativeLookahead:
                var predicate = children[0]; plan.Width = Math.Max(predicate.Width + 4 * bend, TextLayoutEngine.Measure(label, style).Width + 28);
                plan.Children.Add(new RailroadChildPlan(predicate, (plan.Width - predicate.Width) / 2, -predicate.Below - gap));
                plan.Above = predicate.Above + predicate.Below + gap + annotation; plan.Below = 8; break;
            case RailroadExpressionKind.Except:
                plan.Width = Math.Max(Math.Max(children[0].Width, children[1].Width) + 4 * bend, TextLayoutEngine.Measure("except (not an alternative)", style).Width + 28);
                plan.Children.Add(new RailroadChildPlan(children[0], (plan.Width - children[0].Width) / 2, 0));
                var excludedBaseline = children[0].Below + gap + annotation + children[1].Above;
                plan.Children.Add(new RailroadChildPlan(children[1], (plan.Width - children[1].Width) / 2, excludedBaseline));
                plan.Above = children[0].Above; plan.Below = excludedBaseline + children[1].Below + 6; break;
        }
        return plan;
    }

    internal static TextStyle RailroadTextStyle(ResolvedVisualTheme theme, int weight = 500) => new() { Font = new FontSpec { Family = theme.FontFamily, Weight = weight }, FontSize = BodyFontSize(theme, 13), LineHeight = 1 };
    private static int RailroadDimension(double value) {
        if (double.IsNaN(value) || double.IsInfinity(value) || value > int.MaxValue - 1) throw new InvalidOperationException("The measured railroad frame exceeds supported dimensions.");
        return Math.Max(1, (int)Math.Ceiling(value));
    }
    internal static string RailroadRepeatLabel(RailroadExpression expression) => expression.MaximumOccurrences == expression.MinimumOccurrences
        ? "exactly " + expression.MinimumOccurrences.ToString(CultureInfo.InvariantCulture)
        : expression.MinimumOccurrences.ToString(CultureInfo.InvariantCulture) + ".." + (expression.MaximumOccurrences?.ToString(CultureInfo.InvariantCulture) ?? "unbounded");
    internal static string RailroadLabel(RailroadExpression expression) => expression.Kind switch {
        RailroadExpressionKind.Terminal => "\"" + RailroadSafeLabel(expression.Value) + "\"" + (expression.CaseSensitive ? "" : " (ignore case)"),
        RailroadExpressionKind.Reference => RailroadSafeLabel(expression.Value),
        RailroadExpressionKind.Special => "? " + RailroadSafeLabel(expression.Value) + " ?",
        RailroadExpressionKind.AnyCharacter => "any character",
        RailroadExpressionKind.CharacterRange => "U+" + expression.FirstCodePoint.ToString("X4", CultureInfo.InvariantCulture) + (expression.FirstCodePoint == expression.LastCodePoint ? "" : "–U+" + expression.LastCodePoint.ToString("X4", CultureInfo.InvariantCulture)),
        RailroadExpressionKind.Lookahead => "assert next (no consumption)",
        RailroadExpressionKind.NegativeLookahead => "assert not next (no consumption)",
        RailroadExpressionKind.Repeat when expression.MaximumOccurrences == 0 => "0 occurrences",
        _ => string.Empty
    };
    internal static string RailroadSafeLabel(string value) {
        var result = new StringBuilder(value.Length);
        for (var index = 0; index < value.Length; index++) {
            var character = value[index];
            if (char.IsControl(character) || char.IsSurrogate(character) && !(char.IsHighSurrogate(character) && index + 1 < value.Length && char.IsLowSurrogate(value[index + 1])))
                result.Append("\\u").Append(((int)character).ToString("X4", CultureInfo.InvariantCulture));
            else { result.Append(character); if (char.IsHighSurrogate(character)) result.Append(value[++index]); }
        }
        return result.ToString();
    }

    private static RailroadHeadingPlan RailroadHeading(RailroadDiagramBlock block) {
        var theme = block.Options.ResolvedTheme;
        var title = new TextStyle { Font = new FontSpec { Family = theme.FontFamily, Weight = 700 }, FontSize = theme.TitleFontSize, LineHeight = 1 };
        var subtitle = new TextStyle { Font = new FontSpec { Family = theme.FontFamily }, FontSize = theme.SubtitleFontSize, LineHeight = 1 };
        var titleMetrics = TextLayoutEngine.Measure(RailroadSafeLabel(block.Title), title);
        var subtitleMetrics = TextLayoutEngine.Measure(RailroadSafeLabel(block.Subtitle), subtitle);
        return new RailroadHeadingPlan(title, subtitle, block.Title.Length == 0 ? 0 : titleMetrics.Height + 12,
            block.Subtitle.Length == 0 ? 0 : subtitleMetrics.Height + 14, Math.Max(titleMetrics.Width, subtitleMetrics.Width));
    }

    internal sealed class RailroadHeadingPlan {
        public RailroadHeadingPlan(TextStyle title, TextStyle subtitle, double titleAdvance, double subtitleAdvance, double width) { Title = title; Subtitle = subtitle; TitleAdvance = titleAdvance; SubtitleAdvance = subtitleAdvance; Width = width; }
        public TextStyle Title { get; } public TextStyle Subtitle { get; } public double TitleAdvance { get; } public double SubtitleAdvance { get; }
        public double Advance => TitleAdvance + SubtitleAdvance; public double Width { get; }
    }
    internal sealed class RailroadLayoutPlan {
        public RailroadLayoutPlan(IReadOnlyList<RailroadNodePlan> rules, ChartSize size, TextStyle style, RailroadHeadingPlan heading) { Rules = rules; Size = size; Style = style; Heading = heading; }
        public IReadOnlyList<RailroadNodePlan> Rules { get; } public ChartSize Size { get; } public TextStyle Style { get; } public RailroadHeadingPlan Heading { get; }
    }
    internal sealed class RailroadNodePlan {
        public RailroadNodePlan(RailroadExpression expression, string label) { Expression = expression; Label = label; }
        public RailroadExpression Expression { get; } public string Label { get; }
        public double Width { get; set; } public double Above { get; set; } public double Below { get; set; }
        public double Gap { get; set; } public double Bend { get; set; } public int Occurrences { get; set; }
        public List<RailroadChildPlan> Children { get; } = new();
    }
    internal readonly struct RailroadChildPlan {
        public RailroadChildPlan(RailroadNodePlan node, double x, double baseline) { Node = node; X = x; Baseline = baseline; }
        public RailroadNodePlan Node { get; } public double X { get; } public double Baseline { get; }
    }
}
