using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeJson {
    // A private post-order wire table preserves native depth 128 without increasing the common JSON depth limit.
    private static void Railroad(VisualArtifactInterchangeJsonWriter writer, VisualArtifactInterchangeRailroadArtifact? railroad) {
        if (railroad == null) return;
        var expressions = new List<(RailroadExpression Expression, int[] Children)>();
        var roots = new List<int>();
        foreach (var rule in railroad.Rules) roots.Add(Append(rule.Expression));
        writer.Property("railroad"); writer.StartObject();
        String(writer, "ruleComparison", railroad.RuleComparison.ToString());
        String(writer, "themeJson", railroad.Theme.ToJson()); // The existing complete native theme codec owns this versioned value.
        String(writer, "themeMode", railroad.ThemeMode.ToString());
        Number(writer, "paddingLeft", railroad.Padding.Left); Number(writer, "paddingTop", railroad.Padding.Top);
        Number(writer, "paddingRight", railroad.Padding.Right); Number(writer, "paddingBottom", railroad.Padding.Bottom);
        Boolean(writer, "transparentBackground", railroad.TransparentBackground); Boolean(writer, "showCard", railroad.ShowCard);
        Boolean(writer, "hostOwnsFrame", railroad.HostOwnsFrame); Number(writer, "pngOutputScale", railroad.PngOutputScale);
        writer.Property("railroadRules"); writer.StartArray();
        for (var index = 0; index < railroad.Rules.Count; index++) {
            var rule = railroad.Rules[index]; writer.StartObject();
            String(writer, "id", rule.Id); String(writer, "label", rule.Label); Number(writer, "expression", roots[index]); writer.EndObject();
        }
        writer.EndArray(); writer.Property("railroadExpressions"); writer.StartArray();
        foreach (var record in expressions) {
            var expression = record.Expression; writer.StartObject(); String(writer, "kind", expression.Kind.ToString());
            if (expression.Kind is RailroadExpressionKind.Terminal or RailroadExpressionKind.Reference or RailroadExpressionKind.Special)
                String(writer, "value", expression.Value);
            if (expression.Kind == RailroadExpressionKind.Terminal) Boolean(writer, "caseSensitive", expression.CaseSensitive);
            if (expression.Kind == RailroadExpressionKind.Choice) Boolean(writer, "ordered", expression.OrderedChoice);
            if (expression.Kind == RailroadExpressionKind.Repeat) {
                Number(writer, "minimum", expression.MinimumOccurrences); OptionalNumber(writer, "maximum", expression.MaximumOccurrences);
            }
            if (expression.Kind == RailroadExpressionKind.CharacterRange) {
                Number(writer, "firstCodePoint", expression.FirstCodePoint); Number(writer, "lastCodePoint", expression.LastCodePoint);
            }
            writer.Property("railroadArguments"); writer.StartArray(); foreach (var child in record.Children) writer.Number(child);
            writer.EndArray(); writer.EndObject();
        }
        writer.EndArray(); writer.EndObject();

        int Append(RailroadExpression expression) {
            var children = expression.Children.Select(Append).ToArray(); var index = expressions.Count;
            expressions.Add((expression, children)); return index;
        }
    }

    private static VisualArtifactInterchangeRailroadArtifact? ReadRailroad(Dictionary<string, GeoJsonValue> root) {
        var item = OptionalObject(root, "railroad"); if (item == null) return null;
        var railroad = new VisualArtifactInterchangeRailroadArtifact {
            RuleComparison = RequiredEnum<RailroadRuleComparison>(item, "ruleComparison"),
            ThemeMode = OptionalEnum<VisualThemeMode>(item, "themeMode") ?? VisualThemeMode.Light,
            Padding = new ChartPadding(OptionalNumber(item, "paddingLeft") ?? 18, OptionalNumber(item, "paddingTop") ?? 16,
                OptionalNumber(item, "paddingRight") ?? 18, OptionalNumber(item, "paddingBottom") ?? 12),
            TransparentBackground = OptionalBool(item, "transparentBackground") ?? false,
            ShowCard = OptionalBool(item, "showCard") ?? true, HostOwnsFrame = OptionalBool(item, "hostOwnsFrame") ?? false,
            PngOutputScale = OptionalInt(item, "pngOutputScale") ?? 1
        };
        var theme = OptionalString(item, "themeJson"); if (theme != null) railroad.Theme = VisualTheme.FromJson(theme);
        var records = OptionalArray(item, "railroadExpressions");
        if (records.Count > RailroadDiagramLimits.MaximumExpressionOccurrences) throw new ArgumentException("Railroad expression table exceeds the native limit.");
        var expressions = new List<RailroadExpression>(records.Count); var arguments = new List<int[]>(records.Count);
        var occurrences = new List<int>(records.Count);
        foreach (var record in records) {
            var fields = record.AsObject("railroad expression"); var children = new List<RailroadExpression>(); var indices = new List<int>();
            var count = 1;
            foreach (var argument in OptionalArray(fields, "railroadArguments")) {
                var index = RailroadIndex(argument, expressions.Count, "railroad argument");
                if (occurrences[index] > RailroadDiagramLimits.MaximumExpressionOccurrences - count)
                    throw new ArgumentException("Railroad expression occurrences exceed the native limit.");
                count += occurrences[index]; children.Add(expressions[index]); indices.Add(index);
            }
            var expression = ReadRailroadExpression(fields, children);
            if (expression.Depth > RailroadDiagramLimits.MaximumExpressionDepth) throw new ArgumentException("Railroad expression nesting exceeds the native limit.");
            expressions.Add(expression); arguments.Add(indices.ToArray()); occurrences.Add(count);
        }
        var rules = OptionalArray(item, "railroadRules");
        if (rules.Count > RailroadDiagramLimits.MaximumRules) throw new ArgumentException("Railroad rule table exceeds the native limit.");
        var total = 0; var used = new bool[expressions.Count]; var pending = new Stack<int>();
        foreach (var ruleValue in rules) {
            var rule = ruleValue.AsObject("railroad rule");
            if (!rule.TryGetValue("expression", out var raw)) throw new ArgumentException("A railroad rule requires an expression index.");
            var index = RailroadIndex(raw, expressions.Count, "railroad rule expression");
            if (occurrences[index] > RailroadDiagramLimits.MaximumExpressionOccurrences - total)
                throw new ArgumentException("Railroad expression occurrences exceed the native limit.");
            total += occurrences[index]; pending.Push(index);
            railroad.Rules.Add(new RailroadRule(RequiredString(rule, "id"), RequiredString(rule, "label"), expressions[index]));
        }
        while (pending.Count != 0) {
            var index = pending.Pop(); if (used[index]) continue; used[index] = true;
            foreach (var child in arguments[index]) pending.Push(child);
        }
        if (used.Any(value => !value)) throw new ArgumentException("Railroad expression tables must not contain unreachable records.");
        return railroad;
    }

    private static int RailroadIndex(GeoJsonValue value, int available, string context) {
        var number = value.AsNumber(context);
        if (number < 0 || number >= available || Math.Truncate(number) != number)
            throw new ArgumentException(context + " must reference an earlier expression in the bounded table.");
        return (int)number;
    }

    private static RailroadExpression ReadRailroadExpression(Dictionary<string, GeoJsonValue> fields, List<RailroadExpression> children) {
        var kind = RequiredEnum<RailroadExpressionKind>(fields, "kind");
        var count = kind switch {
            RailroadExpressionKind.Optional or RailroadExpressionKind.Repeat or RailroadExpressionKind.Lookahead or RailroadExpressionKind.NegativeLookahead => 1,
            RailroadExpressionKind.Except => 2,
            RailroadExpressionKind.Sequence or RailroadExpressionKind.Choice => -1,
            _ => 0
        };
        if (count >= 0 && children.Count != count || kind == RailroadExpressionKind.Choice && children.Count == 0)
            throw new ArgumentException("Railroad expression arity does not match its semantic kind.");
        return kind switch {
            RailroadExpressionKind.Empty => RailroadExpression.Empty(),
            RailroadExpressionKind.Terminal => RailroadExpression.Terminal(RequiredStringAllowEmpty(fields, "value"), OptionalBool(fields, "caseSensitive") ?? true),
            RailroadExpressionKind.Reference => RailroadExpression.Reference(RequiredString(fields, "value")),
            RailroadExpressionKind.Special => RailroadExpression.Special(RequiredString(fields, "value")),
            RailroadExpressionKind.Sequence => RailroadExpression.Sequence(children),
            RailroadExpressionKind.Choice => RailroadExpression.Choice(children, OptionalBool(fields, "ordered") ?? false),
            RailroadExpressionKind.Optional => RailroadExpression.Optional(children[0]),
            RailroadExpressionKind.Repeat => RailroadExpression.Repeat(children[0], RequiredInt(fields, "minimum"), OptionalInt(fields, "maximum")),
            RailroadExpressionKind.Lookahead => RailroadExpression.Lookahead(children[0]),
            RailroadExpressionKind.NegativeLookahead => RailroadExpression.Lookahead(children[0], positive: false),
            RailroadExpressionKind.Except => RailroadExpression.Except(children[0], children[1]),
            RailroadExpressionKind.AnyCharacter => RailroadExpression.AnyCharacter(),
            RailroadExpressionKind.CharacterRange => RailroadExpression.CharacterRange(RequiredInt(fields, "firstCodePoint"), RequiredInt(fields, "lastCodePoint")),
            _ => throw new ArgumentException("Unknown railroad expression kind.")
        };
    }
}
