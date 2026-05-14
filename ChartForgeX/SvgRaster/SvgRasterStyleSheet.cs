using System;
using System.Collections.Generic;
using ChartForgeX.Svg;

namespace ChartForgeX.SvgRaster;

internal sealed class SvgRasterStyleSheet {
    public static SvgRasterStyleSheet Empty { get; } = new(Array.Empty<SvgRasterStyleRule>());

    private readonly IReadOnlyList<SvgRasterStyleRule> _rules;

    private SvgRasterStyleSheet(IReadOnlyList<SvgRasterStyleRule> rules) {
        _rules = rules;
    }

    public bool IsEmpty => _rules.Count == 0;

    public static SvgRasterStyleSheet Parse(IEnumerable<string> blocks) {
        var rules = new List<SvgRasterStyleRule>();
        var order = 0;
        foreach (var block in blocks) ParseBlock(block, rules, ref order);
        return rules.Count == 0 ? Empty : new SvgRasterStyleSheet(rules);
    }

    public IEnumerable<SvgStyleDeclaration> DeclarationsFor(SvgRasterElement element, IReadOnlyList<SvgRasterElement>? ancestors = null) {
        if (_rules.Count == 0) yield break;
        foreach (var rule in _rules) {
            if (!rule.Selector.Matches(element, ancestors)) continue;
            foreach (var declaration in rule.Declarations.Declarations) yield return declaration;
        }
    }

    private static void ParseBlock(string? css, List<SvgRasterStyleRule> rules, ref int order) {
        if (string.IsNullOrWhiteSpace(css)) return;
        var clean = StripComments(css!);
        var start = 0;
        while (start < clean.Length) {
            var open = clean.IndexOf('{', start);
            if (open < 0) break;
            var close = FindCloseBrace(clean, open + 1);
            if (close < 0) break;
            var selectorText = clean.Substring(start, open - start).Trim();
            var declarationsText = clean.Substring(open + 1, close - open - 1).Trim();
            start = close + 1;
            if (selectorText.Length == 0 || selectorText[0] == '@' || declarationsText.Length == 0) continue;

            SvgStyleDeclarationList declarations;
            try {
                declarations = SvgStyleDeclarationList.Parse(declarationsText);
            } catch (FormatException) {
                continue;
            } catch (ArgumentException) {
                continue;
            }

            foreach (var selectorPart in SplitSelectors(selectorText)) {
                if (!SvgRasterStyleSelector.TryParse(selectorPart, out var selector)) continue;
                rules.Add(new SvgRasterStyleRule(selector, declarations, order++));
            }
        }

        rules.Sort(SvgRasterStyleRuleComparer.Instance);
    }

    private static int FindCloseBrace(string value, int start) {
        var quote = '\0';
        var depth = 0;
        for (var i = start; i < value.Length; i++) {
            var ch = value[i];
            if (quote != '\0') {
                if (ch == quote) quote = '\0';
                continue;
            }

            if (ch == '\'' || ch == '"') {
                quote = ch;
                continue;
            }

            if (ch == '(') depth++;
            else if (ch == ')' && depth > 0) depth--;
            else if (ch == '}' && depth == 0) return i;
        }

        return -1;
    }

    private static IEnumerable<string> SplitSelectors(string value) {
        var start = 0;
        for (var i = 0; i < value.Length; i++) {
            if (value[i] != ',') continue;
            var selector = value.Substring(start, i - start).Trim();
            if (selector.Length > 0) yield return selector;
            start = i + 1;
        }

        var tail = value.Substring(start).Trim();
        if (tail.Length > 0) yield return tail;
    }

    private static string StripComments(string value) {
        var result = new System.Text.StringBuilder(value.Length);
        for (var i = 0; i < value.Length; i++) {
            if (i + 1 < value.Length && value[i] == '/' && value[i + 1] == '*') {
                i += 2;
                while (i + 1 < value.Length && !(value[i] == '*' && value[i + 1] == '/')) i++;
                if (i + 1 < value.Length) i++;
                continue;
            }

            result.Append(value[i]);
        }

        return result.ToString();
    }

    private readonly struct SvgRasterStyleRule {
        public SvgRasterStyleRule(SvgRasterStyleSelector selector, SvgStyleDeclarationList declarations, int order) {
            Selector = selector;
            Declarations = declarations;
            Order = order;
        }

        public SvgRasterStyleSelector Selector { get; }
        public SvgStyleDeclarationList Declarations { get; }
        public int Order { get; }
    }

    private readonly struct SvgRasterStyleSelector {
        private readonly IReadOnlyList<SvgRasterSimpleSelector> _chain;

        private SvgRasterStyleSelector(IReadOnlyList<SvgRasterSimpleSelector> chain) {
            _chain = chain;
            var specificity = 0;
            foreach (var selector in chain) specificity += selector.Specificity;
            Specificity = specificity;
        }

        public int Specificity { get; }

        public static bool TryParse(string value, out SvgRasterStyleSelector selector) {
            selector = default;
            var trimmed = value.Trim();
            if (trimmed.Length == 0 || trimmed.IndexOfAny(new[] { '>', '+', '~', '[', ':', '*' }) >= 0) return false;
            var parts = SplitSelectorParts(trimmed);
            if (parts.Count == 0) return false;
            var chain = new List<SvgRasterSimpleSelector>(parts.Count);
            foreach (var part in parts) {
                if (!SvgRasterSimpleSelector.TryParse(part, out var simple)) return false;
                chain.Add(simple);
            }

            selector = new SvgRasterStyleSelector(chain);
            return true;
        }

        public bool Matches(SvgRasterElement element, IReadOnlyList<SvgRasterElement>? ancestors) {
            if (_chain.Count == 0 || !_chain[_chain.Count - 1].Matches(element)) return false;
            var ancestorIndex = (ancestors?.Count ?? 0) - 1;
            for (var selectorIndex = _chain.Count - 2; selectorIndex >= 0; selectorIndex--) {
                var found = false;
                while (ancestorIndex >= 0) {
                    if (_chain[selectorIndex].Matches(ancestors![ancestorIndex])) {
                        found = true;
                        ancestorIndex--;
                        break;
                    }

                    ancestorIndex--;
                }

                if (!found) return false;
            }

            return true;
        }

        private static IReadOnlyList<string> SplitSelectorParts(string value) {
            var parts = new List<string>();
            var start = 0;
            for (var i = 0; i < value.Length; i++) {
                if (!char.IsWhiteSpace(value[i])) continue;
                AddSelectorPart(value, start, i, parts);
                start = i + 1;
            }

            AddSelectorPart(value, start, value.Length, parts);
            return parts;
        }

        private static void AddSelectorPart(string value, int start, int end, List<string> parts) {
            if (end <= start) return;
            var part = value.Substring(start, end - start).Trim();
            if (part.Length > 0) parts.Add(part);
        }
    }

    private readonly struct SvgRasterSimpleSelector {
        private readonly string? _elementName;
        private readonly string? _id;
        private readonly IReadOnlyList<string> _classNames;

        private SvgRasterSimpleSelector(string? elementName, string? id, IReadOnlyList<string> classNames) {
            _elementName = elementName;
            _id = id;
            _classNames = classNames;
            Specificity = (_id == null ? 0 : 100) + _classNames.Count * 10 + (_elementName == null ? 0 : 1);
        }

        public int Specificity { get; }

        public static bool TryParse(string value, out SvgRasterSimpleSelector selector) {
            selector = default;
            var trimmed = value.Trim();
            if (trimmed.Length == 0 || trimmed.IndexOfAny(new[] { ' ', '>', '+', '~', '[', ':', '*' }) >= 0) return false;
            string? elementName = null;
            string? id = null;
            var classNames = new List<string>();
            var index = 0;
            while (index < trimmed.Length && trimmed[index] != '.' && trimmed[index] != '#') index++;
            if (index > 0) {
                elementName = trimmed.Substring(0, index);
                if (!IsIdentifier(elementName)) return false;
            }

            while (index < trimmed.Length) {
                var marker = trimmed[index++];
                var partStart = index;
                while (index < trimmed.Length && trimmed[index] != '.' && trimmed[index] != '#') index++;
                var part = trimmed.Substring(partStart, index - partStart);
                if (!IsIdentifier(part)) return false;
                if (marker == '#') {
                    if (id != null) return false;
                    id = part;
                } else if (marker == '.') {
                    classNames.Add(part);
                } else {
                    return false;
                }
            }

            if (elementName == null && id == null && classNames.Count == 0) return false;
            selector = new SvgRasterSimpleSelector(elementName, id, classNames);
            return true;
        }

        public bool Matches(SvgRasterElement element) {
            if (_elementName != null && !string.Equals(_elementName, element.Name, StringComparison.OrdinalIgnoreCase)) return false;
            if (_id != null && !string.Equals(_id, element.Get("id"), StringComparison.Ordinal)) return false;
            foreach (var className in _classNames) if (!HasClass(element.Get("class"), className)) return false;
            return true;
        }

        private static bool HasClass(string? value, string className) {
            if (string.IsNullOrWhiteSpace(value)) return false;
            var start = 0;
            while (start < value!.Length) {
                while (start < value.Length && char.IsWhiteSpace(value[start])) start++;
                var end = start;
                while (end < value.Length && !char.IsWhiteSpace(value[end])) end++;
                if (end > start && string.Equals(value.Substring(start, end - start), className, StringComparison.Ordinal)) return true;
                start = end + 1;
            }

            return false;
        }

        private static bool IsIdentifier(string value) {
            if (string.IsNullOrWhiteSpace(value)) return false;
            foreach (var ch in value) {
                if (!(char.IsLetterOrDigit(ch) || ch == '_' || ch == '-')) return false;
            }

            return true;
        }
    }

    private sealed class SvgRasterStyleRuleComparer : IComparer<SvgRasterStyleRule> {
        public static readonly SvgRasterStyleRuleComparer Instance = new();

        public int Compare(SvgRasterStyleRule left, SvgRasterStyleRule right) {
            var specificity = left.Selector.Specificity.CompareTo(right.Selector.Specificity);
            return specificity != 0 ? specificity : left.Order.CompareTo(right.Order);
        }
    }
}
