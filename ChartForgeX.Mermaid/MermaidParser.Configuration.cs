using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using ChartForgeX.Core;

namespace ChartForgeX.Mermaid;

public sealed partial class MermaidParser {
    private const int MaximumConfigurationLength = 65536;
    private static readonly GeoJsonReadLimits ConfigurationJsonLimits = new GeoJsonReadLimits(1024, 256, 256).LimitDepth(8).RejectDuplicates().KeepNumberSpelling();

    private static MermaidSourceConfiguration ReadConfiguration(string source, string[] lines, int frontMatterEndLine, string? frontMatter,
        MermaidParseResult<MermaidDocument> result, out List<MermaidDirective> declarations) {
        var configuration = new MermaidSourceConfiguration();
        declarations = new List<MermaidDirective>();
        var originalLineStarts = MermaidParserUtilities.OriginalLineStarts(source);
        long length = frontMatter == null ? 0 : ConfigurationYamlLength(frontMatter);
        var budgetReported = length > MaximumConfigurationLength;
        if (budgetReported) {
            ConfigurationError(result, new MermaidSourceSpan(1, 1, 3), "Mermaid source configuration exceeds 65536 characters.");
        } else if (frontMatter != null) ReadConfigurationYaml(frontMatter, configuration, result, originalLineStarts);

        for (var index = frontMatterEndLine; index < lines.Length; index++) {
            var bodyLine = lines[index].Trim();
            if (IsMultilineAccessibilityDescription(bodyLine)) {
                var part = bodyLine.Substring(bodyLine.IndexOf('{') + 1);
                while (part.IndexOf('}') < 0 && index + 1 < lines.Length) part = lines[++index];
                continue;
            }
            var prefix = ConfigurationPrefix(lines[index]);
            if (!prefix.Success) continue;
            var start = index;
            var column = LeadingWhitespace(lines[start]) + 1;
            var directive = new StringBuilder();
            long directiveLength = 0;
            var lastLineLength = 0;
            Match ending;
            do {
                var raw = lines[index];
                lastLineLength = raw.Length;
                ending = Regex.Match(raw, @"\}\s*%%\s*$", RegexOptions.CultureInvariant);
                directiveLength += raw.Length + (index > start ? 1L : 0L);
                if (directiveLength <= MaximumConfigurationLength) {
                    if (index > start) directive.Append('\n');
                    directive.Append(raw);
                }
                lines[index] = string.Empty;
                // Even rejected declarations must be consumed before scanning the diagram header.
                if (ending.Success || index + 1 == lines.Length) break;
                index++;
            } while (true);

            var span = new MermaidSourceSpan(start + 1, column,
                originalLineStarts[index] + lastLineLength - originalLineStarts[start] - column + 1);
            length += directiveLength;
            if (length > MaximumConfigurationLength) {
                if (!budgetReported) ConfigurationError(result, span, "Mermaid source configuration exceeds 65536 characters.");
                budgetReported = true;
                continue;
            }
            var text = directive.ToString();
            ending = Regex.Match(text, @"\}\s*%%\s*$", RegexOptions.CultureInvariant);
            declarations.Add(new MermaidDirective(text.Trim(), span));
            if (!ending.Success) {
                ConfigurationError(result, span, "Mermaid init configuration must close with '}%%'.");
                continue;
            }
            try {
                var payload = NormalizeConfigurationJson(text.Substring(prefix.Length, ending.Index - prefix.Length));
                var values = GeoJsonValue.Parse(payload, StringComparer.Ordinal, ConfigurationJsonLimits).AsObject("Mermaid init configuration");
                AddConfigurationJson(values, string.Empty, span, configuration);
            } catch (ArgumentException exception) { ConfigurationError(result, span, exception.Message); }
        }
        return configuration;
    }

    private static Match ConfigurationPrefix(string text) => Regex.Match(text, @"^\s*%%\{\s*(?:init|initialize)\s*:", RegexOptions.CultureInvariant);

    private static void AddConfigurationJson(Dictionary<string, GeoJsonValue> values, string prefix,
        MermaidSourceSpan span, MermaidSourceConfiguration configuration, Func<GeoJsonValue, MermaidSourceSpan>? propertySpan = null) {
        foreach (var pair in values) {
            var key = ConfigurationPathKey(pair.Key);
            var path = prefix + key;
            var declarationSpan = propertySpan?.Invoke(pair.Value) ?? span;
            if (pair.Value.TryAsObject(out var nested) && nested.Count > 0) {
                if (IsConfigurationStringPath(path)) configuration.Add(new MermaidConfigurationSetting(path, null, false, declarationSpan));
                AddConfigurationJson(nested, path + ".", declarationSpan, configuration, propertySpan);
            }
            else configuration.Add(new MermaidConfigurationSetting(path, pair.Value.AsOptionalString(), pair.Value.IsString, declarationSpan));
        }
    }

    private static string ConfigurationPathKey(string key) =>
        Regex.IsMatch(key, @"^[A-Za-z_][A-Za-z0-9_-]*$", RegexOptions.CultureInvariant) ? key : "[" + key + "]";

    private static bool IsConfigurationStringPath(string path) {
        var key = path.Substring(path.LastIndexOf('.') + 1);
        return key == "theme" || key == "fontFamily" || key == "layout" || key == "look" || MermaidSankeySettings.IsScalarPath(path);
    }

    private static void ResolveConfiguration(MermaidSourceConfiguration configuration, MermaidDiagramKind kind,
        MermaidParseResult<MermaidDocument> result) {
        var scope = ConfigurationScope(kind);
        MermaidConfigurationSetting? Selected(string key) {
            if (key == "fontFamily") return configuration.Last(key);
            var scoped = scope == null ? null : configuration.Last(scope + "." + key);
            return scoped != null && IsUnusableAppearance(key, scoped)
                ? configuration.Last(key) ?? scoped : scoped ?? configuration.Last(key);
        }
        void ValidateString(MermaidConfigurationSetting? setting) {
            if (setting != null && !IsConfigurationStringValue(setting)) {
                ConfigurationError(result, setting.Span, "Mermaid configuration '" + setting.Path + "' must be a non-empty string of at most 4096 characters containing complete Unicode characters."
                    + (setting.Path == "fontFamily" ? " Font names must also contain only characters XML allows." : string.Empty));
            }
        }
        foreach (var key in new[] { "theme", "layout", "look" }) {
            ValidateString(configuration.Last(key));
            if (scope != null) ValidateString(configuration.Last(scope + "." + key));
        }
        string? ReadString(string key) {
            var setting = Selected(key);
            if (key == "fontFamily") ValidateString(setting);
            return setting != null && IsConfigurationStringValue(setting) ? setting.Value : null;
        }
        configuration.Theme = ReadString("theme");
        configuration.Layout = ReadString("layout");
        configuration.Look = ReadString("look");
        configuration.FontFamily = ReadString("fontFamily");
        if (kind == MermaidDiagramKind.Sankey)
            configuration.Sankey = MermaidSankeySettings.Read(configuration, (setting, message) => ConfigurationError(result, setting.Span, message));
        foreach (var setting in configuration.Settings) {
            if (configuration.Sankey?.Handles(setting.Path) == true) continue;
            var theme = Selected("theme");
            var font = Selected("fontFamily");
            if (ReferenceEquals(setting, theme) && setting.IsString && IsNativeTheme(setting.Value)) {
                if (kind != MermaidDiagramKind.Sequence && kind != MermaidDiagramKind.ZenUml && kind != MermaidDiagramKind.Agentflow && kind != MermaidDiagramKind.Railroad) continue;
            } else if (ReferenceEquals(setting, font) && setting.IsString && !string.IsNullOrWhiteSpace(setting.Value) && kind != MermaidDiagramKind.Sequence &&
                kind != MermaidDiagramKind.ZenUml && kind != MermaidDiagramKind.Agentflow && kind != MermaidDiagramKind.Railroad) continue;
            else if ((setting.Path == "theme" || setting.Path == scope + ".theme" || setting.Path == "fontFamily") &&
                !ReferenceEquals(setting, theme) && !ReferenceEquals(setting, font) &&
                !(setting.Path == scope + ".theme" && IsUnusableAppearance("theme", setting))) continue;

            var message = ReferenceEquals(setting, theme) && setting.IsString && !IsNativeTheme(setting.Value)
                ? "Mermaid theme '" + setting.Value + "' uses the native static light palette."
                : "Mermaid source configuration '" + setting.Path + "' is retained but not applied to static rendering.";
            Add(result, setting.Span.Line, setting.Span.Column, setting.Span.Length, MermaidDiagnosticSeverity.Warning, message,
                setting.Path == "layout" || setting.Path == "defaultRenderer" || setting.Path.EndsWith(".layout", StringComparison.Ordinal) || setting.Path.EndsWith(".defaultRenderer", StringComparison.Ordinal)
                    ? MermaidDiagnosticCodes.UnsupportedLayout : MermaidDiagnosticCodes.UnsupportedConfiguration);
        }
    }

    private static string? ConfigurationScope(MermaidDiagramKind kind) => kind switch {
        MermaidDiagramKind.Flowchart => "flowchart", MermaidDiagramKind.Swimlane => "swimlane", MermaidDiagramKind.UseCase => "usecase",
        MermaidDiagramKind.Class => "class", MermaidDiagramKind.State => "state", MermaidDiagramKind.EntityRelationship => "er",
        MermaidDiagramKind.Sequence => "sequence", MermaidDiagramKind.Requirement => "requirement", MermaidDiagramKind.GitGraph => "gitGraph",
        MermaidDiagramKind.XYChart => "xyChart", MermaidDiagramKind.Quadrant => "quadrantChart", MermaidDiagramKind.Gantt => "gantt", MermaidDiagramKind.Agentflow => "agentflow",
        MermaidDiagramKind.Railroad => "railroad", MermaidDiagramKind.TreeView => "treeView", MermaidDiagramKind.Wardley => "wardley-beta",
        MermaidDiagramKind.Pie => "pie", MermaidDiagramKind.Journey => "journey", MermaidDiagramKind.MindMap => "mindmap",
        MermaidDiagramKind.Timeline => "timeline", MermaidDiagramKind.Sankey => "sankey", MermaidDiagramKind.Block => "block",
        MermaidDiagramKind.Packet => "packet", MermaidDiagramKind.Architecture => "architecture", MermaidDiagramKind.Kanban => "kanban",
        MermaidDiagramKind.Radar => "radar", MermaidDiagramKind.C4 => "c4", MermaidDiagramKind.Venn => "venn",
        MermaidDiagramKind.Ishikawa => "ishikawa", MermaidDiagramKind.EventModeling => "eventmodeling", MermaidDiagramKind.Cynefin => "cynefin",
        _ => null
    };

    private static bool IsNativeTheme(string? value) => string.Equals(value, "dark", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(value, "default", StringComparison.OrdinalIgnoreCase);

    private static bool IsConfigurationStringValue(MermaidConfigurationSetting setting) =>
        setting.IsString && !string.IsNullOrWhiteSpace(setting.Value) && setting.Value!.Length <= 4096 &&
        IsCompleteConfigurationUnicode(setting.Value, setting.Path == "fontFamily");

    private static bool IsCompleteConfigurationUnicode(string value, bool requireXmlCharacters) {
        for (var index = 0; index < value.Length; index++) {
            if (!char.IsSurrogate(value[index])) {
                if (requireXmlCharacters && !System.Xml.XmlConvert.IsXmlChar(value[index])) return false;
                continue;
            }
            if (!char.IsHighSurrogate(value[index]) || index + 1 >= value.Length || !char.IsLowSurrogate(value[index + 1])) return false;
            index++;
        }
        return true;
    }

    private static bool IsRecognizedTheme(string? value) => IsNativeTheme(value) || value == "base" || value == "forest" ||
        value == "neutral" || value == "null" || value == "neo" || value == "neo-dark" || value == "redux" ||
        value == "redux-dark" || value == "redux-color" || value == "redux-dark-color";

    private static bool IsUnusableAppearance(string key, MermaidConfigurationSetting setting) {
        if (!IsConfigurationStringValue(setting)) return false;
        return key == "theme" ? !IsRecognizedTheme(setting.Value)
            : key == "look" && setting.Value != "classic" && setting.Value != "handDrawn" && setting.Value != "neo";
    }

    private static void ConfigurationError(MermaidParseResult<MermaidDocument> result, MermaidSourceSpan span, string message) =>
        Add(result, span.Line, span.Column, span.Length, MermaidDiagnosticSeverity.Error, message, MermaidDiagnosticCodes.InvalidConfiguration);
}
