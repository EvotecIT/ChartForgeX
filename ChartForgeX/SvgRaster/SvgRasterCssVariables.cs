using System;
using System.Collections.Generic;

namespace ChartForgeX.SvgRaster;

internal static class SvgRasterCssVariables {
    public static bool IsCustomProperty(string name) =>
        name.StartsWith("--", StringComparison.Ordinal) && name.Length > 2;

    public static string Resolve(string value, IReadOnlyDictionary<string, string> properties) {
        if (string.IsNullOrWhiteSpace(value) || value.IndexOf("var(", StringComparison.OrdinalIgnoreCase) < 0) return value;
        return Resolve(value, properties, 0, null);
    }

    /// <summary>Freezes aliases against this element's winning values before they can be inherited.</summary>
    public static void Compute(Dictionary<string, string> properties) {
        // Read one snapshot so declaration/dictionary order cannot change alias meaning.
        var specified = new Dictionary<string, string>(properties, StringComparer.Ordinal);
        var cyclic = FindCyclicProperties(specified);
        foreach (var property in specified) {
            if (property.Value.IndexOf("var(", StringComparison.OrdinalIgnoreCase) < 0) continue;
            var computed = Resolve(property.Value, specified, 0, cyclic);
            // Missing or cyclic substitutions remain unavailable, rather than rebinding in a child.
            if (cyclic.Contains(property.Key) || computed.IndexOf("var(", StringComparison.OrdinalIgnoreCase) >= 0) properties.Remove(property.Key);
            else properties[property.Key] = computed;
        }
    }

    private static string Resolve(string value, IReadOnlyDictionary<string, string> properties, int depth, HashSet<string>? cyclic) {
        if (string.IsNullOrWhiteSpace(value) || depth >= 8 || value.IndexOf("var(", StringComparison.OrdinalIgnoreCase) < 0) return value;
        var result = new System.Text.StringBuilder(value.Length);
        var index = 0;
        while (index < value.Length) {
            var open = value.IndexOf("var(", index, StringComparison.OrdinalIgnoreCase);
            if (open < 0) {
                result.Append(value, index, value.Length - index);
                break;
            }

            var close = FindFunctionClose(value, open + 4);
            if (close < 0) {
                result.Append(value, index, value.Length - index);
                break;
            }

            result.Append(value, index, open - index);
            var body = value.Substring(open + 4, close - open - 4);
            result.Append(ResolveVariable(body, properties, depth, cyclic));
            index = close + 1;
        }

        return result.ToString();
    }

    private static string ResolveVariable(string body, IReadOnlyDictionary<string, string> properties, int depth, HashSet<string>? cyclic) {
        SplitVariable(body, out var name, out var fallback);
        if (IsCustomProperty(name) && !(cyclic?.Contains(name) ?? false) && properties.TryGetValue(name, out var value)) {
            var resolved = Resolve(value, properties, depth + 1, cyclic);
            if (resolved.IndexOf("var(", StringComparison.OrdinalIgnoreCase) < 0) return resolved;
        }
        return fallback == null ? "var(" + body + ")" : Resolve(fallback, properties, depth + 1, cyclic);
    }

    /// <summary>Finds same-element cycles, including unused fallback references, without recursive traversal.</summary>
    private static HashSet<string> FindCyclicProperties(IReadOnlyDictionary<string, string> properties) {
        var cyclic = new HashSet<string>(StringComparer.Ordinal);
        var indices = new Dictionary<string, int>(StringComparer.Ordinal);
        var lowLinks = new Dictionary<string, int>(StringComparer.Ordinal);
        var onStack = new HashSet<string>(StringComparer.Ordinal);
        var components = new Stack<string>();
        var path = new List<string>();
        var pending = new Stack<IEnumerator<string>>();
        foreach (var property in properties) {
            if (indices.ContainsKey(property.Key)) continue;
            Enter(property.Key, property.Value);
            while (pending.Count > 0) {
                var current = path[path.Count - 1];
                var references = pending.Peek();
                if (!references.MoveNext()) {
                    references.Dispose();
                    pending.Pop();
                    path.RemoveAt(path.Count - 1);
                    if (lowLinks[current] == indices[current]) {
                        var component = new List<string>();
                        string member;
                        do {
                            member = components.Pop();
                            onStack.Remove(member);
                            component.Add(member);
                        } while (!string.Equals(member, current, StringComparison.Ordinal));
                        if (component.Count > 1) foreach (var name in component) cyclic.Add(name);
                    }
                    if (path.Count > 0) {
                        var parent = path[path.Count - 1];
                        lowLinks[parent] = Math.Min(lowLinks[parent], lowLinks[current]);
                    }
                } else if (properties.TryGetValue(references.Current, out var value)) {
                    var name = references.Current;
                    if (!indices.ContainsKey(name)) Enter(name, value);
                    else if (onStack.Contains(name)) lowLinks[current] = Math.Min(lowLinks[current], indices[name]);
                    if (string.Equals(name, current, StringComparison.Ordinal)) cyclic.Add(name);
                }
            }
        }
        return cyclic;

        void Enter(string name, string value) {
            indices.Add(name, indices.Count);
            lowLinks.Add(name, indices[name]);
            components.Push(name);
            onStack.Add(name);
            path.Add(name);
            pending.Push(References(value).GetEnumerator());
        }
    }

    private static IEnumerable<string> References(string value) {
        var index = 0;
        while (index < value.Length) {
            var open = value.IndexOf("var(", index, StringComparison.OrdinalIgnoreCase);
            if (open < 0) yield break;
            var close = FindFunctionClose(value, open + 4);
            if (close < 0) yield break;
            SplitVariable(value.Substring(open + 4, close - open - 4), out var name, out _);
            if (IsCustomProperty(name)) yield return name;
            index = open + 4; // Visit nested references in the fallback as well.
        }
    }

    private static void SplitVariable(string body, out string name, out string? fallback) {
        var comma = FindTopLevelComma(body);
        if (comma < 0) {
            name = body.Trim();
            fallback = null;
            return;
        }

        name = body.Substring(0, comma).Trim();
        fallback = body.Substring(comma + 1).Trim();
    }

    private static int FindTopLevelComma(string value) {
        var quote = '\0';
        var depth = 0;
        for (var i = 0; i < value.Length; i++) {
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
            else if (ch == ',' && depth == 0) return i;
        }

        return -1;
    }

    private static int FindFunctionClose(string value, int start) {
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
            else if (ch == ')') {
                if (depth == 0) return i;
                depth--;
            }
        }

        return -1;
    }
}
