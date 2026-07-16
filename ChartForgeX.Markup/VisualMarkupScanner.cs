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
                descriptor = ResolveFence(result, fenceInfo, fenceLine);
                include = descriptor.HasValue;

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

    /// <summary>
    /// Validates and projects a visual fence that was already discovered by another Markdown parser.
    /// </summary>
    /// <param name="fenceInfo">The full fence info string after the opening marker.</param>
    /// <param name="payload">The fence payload.</param>
    /// <param name="fenceLine">The one-based source line containing the opening fence.</param>
    /// <param name="payloadStartLine">The one-based source line where the payload starts.</param>
    /// <param name="payloadEndLine">The one-based source line where the payload ends.</param>
    /// <returns>A scan result containing the projected block or validation diagnostics.</returns>
    public static VisualMarkupScanResult ParseFenceBlock(
        string fenceInfo,
        string payload,
        int fenceLine,
        int payloadStartLine,
        int payloadEndLine) {
        if (fenceInfo == null) throw new ArgumentNullException(nameof(fenceInfo));
        if (payload == null) throw new ArgumentNullException(nameof(payload));

        var normalizedFenceLine = Math.Max(1, fenceLine);
        var normalizedStartLine = Math.Max(1, payloadStartLine);
        var normalizedEndLine = Math.Max(normalizedStartLine, payloadEndLine);
        var result = new VisualMarkupScanResult();
        var descriptor = ResolveFence(result, fenceInfo, normalizedFenceLine);
        if (descriptor.HasValue) {
            result.Blocks.Add(CreateBlock(
                descriptor.Value,
                fenceInfo,
                payload,
                normalizedFenceLine,
                normalizedStartLine,
                normalizedEndLine));
        }

        return result;
    }

    internal static bool TryResolveFence(string info, out VisualMarkupKind kind, out string fenceName) {
        var descriptor = ResolveFence(null, info, 1);
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
        return CreateBlock(
            descriptor,
            fenceInfo,
            string.Join("\n", payload),
            fenceLine,
            payloadStartLine,
            payloadEndLine);
    }

    private static VisualMarkupBlock CreateBlock(VisualMarkupFenceDescriptor descriptor, string fenceInfo, string payload, int fenceLine, int payloadStartLine, int payloadEndLine) {
        return new VisualMarkupBlock(
            descriptor.Kind,
            descriptor.Name,
            fenceInfo.Trim(),
            descriptor.SchemaVersion,
            payload,
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

    private static VisualMarkupFenceDescriptor? ResolveFence(VisualMarkupScanResult? result, string info, int fenceLine) {
        if (string.IsNullOrWhiteSpace(info)) return null;
        var normalized = NormalizeFenceInfo(WithoutAttributes(info));
        if (IsFenceName(normalized, "mermaid")) return new VisualMarkupFenceDescriptor(VisualMarkupKind.Mermaid, "mermaid", 0);
        if (!IsChartForgeXFamilyFence(normalized)) return null;

        var tokens = SplitFenceTokens(normalized);
        if (tokens.Count < 2 || tokens[0] != "chartforgex") {
            if (result != null) Add(result, fenceLine, MarkupDiagnosticSeverity.Error, "ChartForgeX visual fences must use 'chartforgex <kind> v1'.");
            return null;
        }

        var kindToken = tokens[1];
        if (!TryParseChartForgeXKind(kindToken, out var kind)) {
            if (result != null) Add(result, fenceLine, MarkupDiagnosticSeverity.Warning, "Unsupported ChartForgeX visual kind '" + kindToken + "'.");
            return null;
        }

        if (tokens.Count < 3) {
            if (result != null) Add(result, fenceLine, MarkupDiagnosticSeverity.Error, "ChartForgeX visual fence '" + "chartforgex " + kindToken + "' must declare schema version v1.");
            return null;
        }

        if (tokens[2] != "v1") {
            if (result != null) Add(result, fenceLine, MarkupDiagnosticSeverity.Error, "Unsupported ChartForgeX markup schema version '" + tokens[2] + "'. Supported version is v1.");
            return null;
        }

        if (tokens.Count > 3) {
            if (result != null) Add(result, fenceLine, MarkupDiagnosticSeverity.Error, "Unexpected token '" + tokens[3] + "' in ChartForgeX visual fence. Use attributes in braces after v1.");
            return null;
        }

        return new VisualMarkupFenceDescriptor(kind, "chartforgex " + kindToken, 1);
    }

    private static string NormalizeFenceInfo(string info) => info.Trim().ToLowerInvariant();

    private static string WithoutAttributes(string info) {
        var brace = info.IndexOf('{');
        return (brace >= 0 ? info.Substring(0, brace) : info).Trim();
    }

    private static List<string> SplitFenceTokens(string info) {
        var tokens = new List<string>();
        foreach (var token in info.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)) tokens.Add(token);
        return tokens;
    }

    private static bool IsFenceName(string info, string name) {
        if (info == name) return true;
        if (!info.StartsWith(name, StringComparison.Ordinal)) return false;
        var next = info[name.Length];
        return char.IsWhiteSpace(next) || next == '{';
    }

    private static bool IsChartForgeXFamilyFence(string normalizedInfo) => normalizedInfo == "chartforgex" || normalizedInfo.StartsWith("chartforgex ", StringComparison.Ordinal);

    private static bool TryParseChartForgeXKind(string value, out VisualMarkupKind kind) {
        switch (value) {
            case "topology":
                kind = VisualMarkupKind.Topology;
                return true;
            case "flow":
                kind = VisualMarkupKind.Flow;
                return true;
            case "table":
                kind = VisualMarkupKind.Table;
                return true;
            case "chart":
                kind = VisualMarkupKind.Chart;
                return true;
            case "timeline":
                kind = VisualMarkupKind.Timeline;
                return true;
            case "sequence":
                kind = VisualMarkupKind.Sequence;
                return true;
            default:
                kind = default;
                return false;
        }
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
        public VisualMarkupFenceDescriptor(VisualMarkupKind kind, string name, int schemaVersion) {
            Kind = kind;
            Name = name;
            SchemaVersion = schemaVersion;
        }

        public VisualMarkupKind Kind { get; }

        public string Name { get; }

        public int SchemaVersion { get; }
    }

    private static class EmptyAttributes {
        public static readonly IReadOnlyDictionary<string, string> Value = new Dictionary<string, string>(StringComparer.Ordinal);
    }
}
