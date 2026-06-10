using System;
using System.Collections.Generic;
using System.Text;

namespace ChartForgeX.Markup;

/// <summary>
/// Scans Markdown for ChartForgeX and Mermaid visual fenced blocks without depending on a Markdown renderer.
/// </summary>
public static class VisualMarkupScanner {
    /// <summary>
    /// Scans Markdown for supported visual fenced blocks and line-aware diagnostics.
    /// </summary>
    /// <param name="text">The Markdown text.</param>
    /// <returns>The visual scan result.</returns>
    public static VisualMarkupScanResult Scan(string text) {
        if (text == null) throw new ArgumentNullException(nameof(text));
        var result = new VisualMarkupScanResult();
        var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var inFence = false;
        var fence = string.Empty;
        var fenceInfo = string.Empty;
        var fenceLine = 1;
        var payloadStartLine = 1;
        var include = false;
        var payload = new List<string>();
        VisualMarkupFenceDescriptor? descriptor = null;

        for (var index = 0; index < lines.Length; index++) {
            var line = lines[index];
            var indent = LeadingIndentColumns(line);
            var trimmed = line.TrimStart();
            if (!inFence) {
                if (indent > 3) continue;
                if (!IsOpeningFence(trimmed, out fence, out fenceInfo)) continue;

                fenceLine = index + 1;
                payloadStartLine = index + 2;
                payload.Clear();
                descriptor = ResolveFence(fenceInfo);
                include = descriptor.HasValue;
                if (!include && IsChartForgeXFamilyFence(fenceInfo)) {
                    Add(result, fenceLine, MarkupDiagnosticSeverity.Warning, "Unsupported ChartForgeX visual fence '" + FirstFenceToken(fenceInfo) + "'.");
                }

                inFence = true;
                continue;
            }

            if (indent <= 3 && IsClosingFence(trimmed, fence)) {
                if (include && descriptor.HasValue) {
                    result.Blocks.Add(CreateBlock(descriptor.Value, fenceInfo, payload, fenceLine, payloadStartLine, index));
                }

                inFence = false;
                include = false;
                descriptor = null;
                payload.Clear();
                continue;
            }

            if (include) payload.Add(line);
        }

        if (inFence && include && descriptor.HasValue) result.Blocks.Add(CreateBlock(descriptor.Value, fenceInfo, payload, fenceLine, payloadStartLine, lines.Length));
        return result;
    }

    /// <summary>
    /// Extracts all supported visual fenced blocks from Markdown.
    /// </summary>
    /// <param name="text">The Markdown text.</param>
    /// <returns>The supported visual blocks.</returns>
    public static List<VisualMarkupBlock> ExtractBlocks(string text) => Scan(text).Blocks;

    internal static bool TryResolveFence(string info, out VisualMarkupKind kind, out string fenceName) {
        var descriptor = ResolveFence(info);
        if (descriptor.HasValue) {
            kind = descriptor.Value.Kind;
            fenceName = descriptor.Value.Name;
            return true;
        }

        kind = default;
        fenceName = string.Empty;
        return false;
    }

    private static VisualMarkupBlock CreateBlock(VisualMarkupFenceDescriptor descriptor, string fenceInfo, List<string> payload, int fenceLine, int payloadStartLine, int payloadEndLine) {
        return new VisualMarkupBlock(
            descriptor.Kind,
            descriptor.Name,
            fenceInfo.Trim(),
            string.Join("\n", payload),
            fenceLine,
            payloadStartLine,
            payloadEndLine < payloadStartLine ? payloadStartLine : payloadEndLine,
            ParseAttributes(fenceInfo));
    }

    private static bool IsOpeningFence(string trimmed, out string fence, out string info) {
        fence = string.Empty;
        info = string.Empty;
        if (!trimmed.StartsWith("```", StringComparison.Ordinal) && !trimmed.StartsWith("~~~", StringComparison.Ordinal)) return false;
        var marker = trimmed[0];
        var count = CountPrefix(trimmed, marker);
        fence = new string(marker, count);
        info = trimmed.Substring(count).Trim();
        return true;
    }

    private static VisualMarkupFenceDescriptor? ResolveFence(string info) {
        if (string.IsNullOrWhiteSpace(info)) return null;
        var normalized = NormalizeFenceInfo(info);
        if (IsFenceName(normalized, "chartforgex topology")) return new VisualMarkupFenceDescriptor(VisualMarkupKind.Topology, "chartforgex topology");
        if (IsFenceName(normalized, "chartforgex-topology")) return new VisualMarkupFenceDescriptor(VisualMarkupKind.Topology, "chartforgex-topology");
        if (IsFenceName(normalized, "cfx topology")) return new VisualMarkupFenceDescriptor(VisualMarkupKind.Topology, "cfx topology");
        if (IsFenceName(normalized, "cfx-topology")) return new VisualMarkupFenceDescriptor(VisualMarkupKind.Topology, "cfx-topology");
        if (IsFenceName(normalized, "chartforgex flow")) return new VisualMarkupFenceDescriptor(VisualMarkupKind.Flow, "chartforgex flow");
        if (IsFenceName(normalized, "cfx flow")) return new VisualMarkupFenceDescriptor(VisualMarkupKind.Flow, "cfx flow");
        if (IsFenceName(normalized, "chartforgex table")) return new VisualMarkupFenceDescriptor(VisualMarkupKind.Table, "chartforgex table");
        if (IsFenceName(normalized, "cfx table")) return new VisualMarkupFenceDescriptor(VisualMarkupKind.Table, "cfx table");
        if (IsFenceName(normalized, "chartforgex chart")) return new VisualMarkupFenceDescriptor(VisualMarkupKind.Chart, "chartforgex chart");
        if (IsFenceName(normalized, "cfx chart")) return new VisualMarkupFenceDescriptor(VisualMarkupKind.Chart, "cfx chart");
        if (IsFenceName(normalized, "chartforgex timeline")) return new VisualMarkupFenceDescriptor(VisualMarkupKind.Timeline, "chartforgex timeline");
        if (IsFenceName(normalized, "cfx timeline")) return new VisualMarkupFenceDescriptor(VisualMarkupKind.Timeline, "cfx timeline");
        if (IsFenceName(normalized, "mermaid")) return new VisualMarkupFenceDescriptor(VisualMarkupKind.Mermaid, "mermaid");
        return null;
    }

    private static string NormalizeFenceInfo(string info) => info.Trim().ToLowerInvariant();

    private static bool IsFenceName(string info, string name) {
        if (info == name) return true;
        if (!info.StartsWith(name, StringComparison.Ordinal)) return false;
        var next = info[name.Length];
        return char.IsWhiteSpace(next) || next == '{';
    }

    private static bool IsChartForgeXFamilyFence(string info) {
        var normalized = NormalizeFenceInfo(info);
        return normalized == "chartforgex" ||
            normalized == "cfx" ||
            normalized.StartsWith("chartforgex ", StringComparison.Ordinal) ||
            normalized.StartsWith("chartforgex-", StringComparison.Ordinal) ||
            normalized.StartsWith("cfx ", StringComparison.Ordinal) ||
            normalized.StartsWith("cfx-", StringComparison.Ordinal);
    }

    private static string FirstFenceToken(string info) {
        var trimmed = info.Trim();
        var brace = trimmed.IndexOf('{');
        if (brace >= 0) trimmed = trimmed.Substring(0, brace).Trim();
        return trimmed;
    }

    private static IReadOnlyDictionary<string, string> ParseAttributes(string info) {
        var start = info.IndexOf('{');
        if (start < 0) return EmptyAttributes.Value;
        var end = info.LastIndexOf('}');
        if (end <= start) return EmptyAttributes.Value;
        var body = info.Substring(start + 1, end - start - 1).Trim();
        if (body.Length == 0) return EmptyAttributes.Value;
        var attributes = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var token in SplitAttributeTokens(body)) {
            if (token.Length == 0) continue;
            if (token[0] == '#') {
                attributes["id"] = token.Substring(1);
                continue;
            }

            if (token[0] == '.') {
                AppendClass(attributes, token.Substring(1));
                continue;
            }

            var split = token.IndexOf('=');
            if (split > 0) attributes[token.Substring(0, split)] = Unquote(token.Substring(split + 1));
        }

        return attributes;
    }

    private static List<string> SplitAttributeTokens(string text) {
        var tokens = new List<string>();
        var current = new StringBuilder();
        var quote = '\0';
        for (var index = 0; index < text.Length; index++) {
            var value = text[index];
            if (quote != '\0') {
                current.Append(value);
                if (value == quote) quote = '\0';
                continue;
            }

            if (value == '"' || value == '\'') {
                quote = value;
                current.Append(value);
                continue;
            }

            if (char.IsWhiteSpace(value)) {
                if (current.Length > 0) {
                    tokens.Add(current.ToString());
                    current.Clear();
                }

                continue;
            }

            current.Append(value);
        }

        if (current.Length > 0) tokens.Add(current.ToString());
        return tokens;
    }

    private static string Unquote(string value) {
        if (value.Length >= 2 && ((value[0] == '"' && value[value.Length - 1] == '"') || (value[0] == '\'' && value[value.Length - 1] == '\''))) return value.Substring(1, value.Length - 2);
        return value;
    }

    private static void AppendClass(Dictionary<string, string> attributes, string value) {
        if (value.Length == 0) return;
        if (attributes.TryGetValue("class", out var existing) && existing.Length > 0) attributes["class"] = existing + " " + value;
        else attributes["class"] = value;
    }

    private static int CountPrefix(string text, char value) {
        var count = 0;
        while (count < text.Length && text[count] == value) count++;
        return count;
    }

    private static bool IsClosingFence(string text, string fence) {
        var markerCount = CountPrefix(text, fence[0]);
        if (markerCount < fence.Length) return false;
        for (var i = markerCount; i < text.Length; i++) {
            if (!char.IsWhiteSpace(text[i])) return false;
        }

        return true;
    }

    private static int LeadingIndentColumns(string text) {
        var count = 0;
        for (var i = 0; i < text.Length; i++) {
            if (text[i] == ' ') {
                count++;
            } else if (text[i] == '\t') {
                count += 4;
            } else {
                break;
            }
        }

        return count;
    }

    private static void Add(VisualMarkupScanResult result, int line, MarkupDiagnosticSeverity severity, string message) {
        result.Diagnostics.Add(new MarkupDiagnostic {
            Line = line,
            Severity = severity,
            Message = message
        });
    }

    private readonly struct VisualMarkupFenceDescriptor {
        public VisualMarkupFenceDescriptor(VisualMarkupKind kind, string name) {
            Kind = kind;
            Name = name;
        }

        public VisualMarkupKind Kind { get; }

        public string Name { get; }
    }

    private static class EmptyAttributes {
        public static readonly IReadOnlyDictionary<string, string> Value = new Dictionary<string, string>(StringComparer.Ordinal);
    }
}
