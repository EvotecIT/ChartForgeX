using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using ChartForgeX.Core;

namespace ChartForgeX.Mermaid;

public sealed partial class MermaidParser {
    private const int MaximumConfigurationLength = 65536;
    private static readonly GeoJsonReadLimits ConfigurationJsonLimits = new GeoJsonReadLimits(1024, 256, 256).LimitDepth(8).RejectDuplicates();

    private static MermaidSourceConfiguration ReadConfiguration(string source, string[] lines, int frontMatterEndLine, string? frontMatter,
        MermaidParseResult<MermaidDocument> result, out List<MermaidDirective> declarations) {
        var configuration = new MermaidSourceConfiguration();
        declarations = new List<MermaidDirective>();
        var originalLineStarts = MermaidParserUtilities.OriginalLineStarts(source);
        var length = frontMatter == null ? 0 : ConfigurationYamlLength(frontMatter);
        if (length > MaximumConfigurationLength) {
            ConfigurationError(result, new MermaidSourceSpan(1, 1, 3), "Mermaid source configuration exceeds 65536 characters.");
        } else if (frontMatter != null) ReadConfigurationYaml(frontMatter, configuration, result);

        for (var index = frontMatterEndLine; index < lines.Length; index++) {
            var prefix = ConfigurationPrefix(lines[index]);
            if (!prefix.Success) continue;
            var start = index;
            var column = LeadingWhitespace(lines[start]) + 1;
            var directive = new StringBuilder();
            var lastLineLength = 0;
            Match ending;
            do {
                var raw = lines[index];
                lastLineLength = raw.Length;
                ending = Regex.Match(raw, @"\}\s*%%\s*$", RegexOptions.CultureInvariant);
                if (directive.Length > 0) directive.Append('\n');
                directive.Append(raw);
                lines[index] = string.Empty;
                if (ending.Success || directive.Length > MaximumConfigurationLength || index + 1 == lines.Length) break;
                index++;
            } while (true);

            var text = directive.ToString();
            ending = Regex.Match(text, @"\}\s*%%\s*$", RegexOptions.CultureInvariant);
            var span = new MermaidSourceSpan(start + 1, column,
                originalLineStarts[index] + lastLineLength - originalLineStarts[start] - column + 1);
            length += text.Length;
            if (length > MaximumConfigurationLength) {
                ConfigurationError(result, span, "Mermaid source configuration exceeds 65536 characters.");
                break;
            }
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

    private static Match ConfigurationPrefix(string text) => Regex.Match(text, @"^\s*%%\s*\{\s*(?:init|initialize)\s*:", RegexOptions.CultureInvariant);

    private static void AddConfigurationJson(Dictionary<string, GeoJsonValue> values, string prefix,
        MermaidSourceSpan span, MermaidSourceConfiguration configuration) {
        foreach (var pair in values) {
            var key = Regex.IsMatch(pair.Key, @"^[A-Za-z_][A-Za-z0-9_-]*$", RegexOptions.CultureInvariant) ? pair.Key : "[" + pair.Key + "]";
            var path = prefix + key;
            if (pair.Value.TryAsObject(out var nested) && nested.Count > 0) {
                if (IsConfigurationStringPath(path)) configuration.Add(new MermaidConfigurationSetting(path, null, false, span));
                AddConfigurationJson(nested, path + ".", span, configuration);
            }
            else configuration.Add(new MermaidConfigurationSetting(path, pair.Value.AsOptionalString(), pair.Value.IsString, span));
        }
    }

    private static bool IsConfigurationStringPath(string path) {
        var key = path.Substring(path.LastIndexOf('.') + 1);
        return key == "theme" || key == "fontFamily" || key == "layout" || key == "look";
    }

    private static void ResolveConfiguration(MermaidSourceConfiguration configuration, MermaidDiagramKind kind,
        MermaidParseResult<MermaidDocument> result) {
        var scope = ConfigurationScope(kind);
        MermaidConfigurationSetting? Selected(string key) => configuration.Last(scope + "." + key) ?? configuration.Last(key);
        string? ReadString(string key) {
            var setting = Selected(key);
            if (setting == null) return null;
            if (!setting.IsString || string.IsNullOrWhiteSpace(setting.Value) || setting.Value!.Length > 4096) {
                ConfigurationError(result, setting.Span, "Mermaid configuration '" + setting.Path + "' must be a non-empty string of at most 4096 characters.");
                return null;
            }
            return setting.Value;
        }
        configuration.Theme = ReadString("theme");
        configuration.Layout = ReadString("layout");
        configuration.Look = ReadString("look");
        configuration.FontFamily = ReadString("fontFamily");
        foreach (var setting in configuration.Settings) {
            var theme = Selected("theme");
            var font = Selected("fontFamily");
            if (ReferenceEquals(setting, theme) && setting.IsString && IsNativeTheme(setting.Value)) {
                if (kind != MermaidDiagramKind.ZenUml && kind != MermaidDiagramKind.Agentflow && kind != MermaidDiagramKind.Railroad) continue;
            } else if (ReferenceEquals(setting, font) && setting.IsString && !string.IsNullOrWhiteSpace(setting.Value) &&
                kind != MermaidDiagramKind.ZenUml && kind != MermaidDiagramKind.Agentflow && kind != MermaidDiagramKind.Railroad) continue;
            else if ((setting.Path == "theme" || setting.Path == scope + ".theme" || setting.Path == "fontFamily" || setting.Path == scope + ".fontFamily") &&
                !ReferenceEquals(setting, theme) && !ReferenceEquals(setting, font)) continue;

            var message = ReferenceEquals(setting, theme) && setting.IsString && !IsNativeTheme(setting.Value)
                ? "Mermaid theme '" + setting.Value + "' uses the native static light palette."
                : "Mermaid source configuration '" + setting.Path + "' is retained but not applied to static rendering.";
            Add(result, setting.Span.Line, setting.Span.Column, setting.Span.Length, MermaidDiagnosticSeverity.Warning, message,
                setting.Path == "layout" || setting.Path == "defaultRenderer" || setting.Path.EndsWith(".layout", StringComparison.Ordinal) || setting.Path.EndsWith(".defaultRenderer", StringComparison.Ordinal)
                    ? MermaidDiagnosticCodes.UnsupportedLayout : MermaidDiagnosticCodes.UnsupportedConfiguration);
        }
    }

    private static string ConfigurationScope(MermaidDiagramKind kind) => kind switch {
        MermaidDiagramKind.Flowchart => "flowchart", MermaidDiagramKind.Swimlane => "swimlane", MermaidDiagramKind.UseCase => "usecase",
        MermaidDiagramKind.Class => "class", MermaidDiagramKind.State => "state", MermaidDiagramKind.EntityRelationship => "er",
        MermaidDiagramKind.Sequence => "sequence", MermaidDiagramKind.Requirement => "requirement", MermaidDiagramKind.GitGraph => "gitGraph",
        MermaidDiagramKind.XYChart => "xyChart", MermaidDiagramKind.Quadrant => "quadrantChart", MermaidDiagramKind.Gantt => "gantt", MermaidDiagramKind.Agentflow => "agentflow",
        MermaidDiagramKind.Railroad => "railroad", MermaidDiagramKind.TreeView => "treeView", _ => kind.ToString().ToLowerInvariant()
    };

    private static bool IsNativeTheme(string? value) => string.Equals(value, "dark", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(value, "default", StringComparison.OrdinalIgnoreCase);

    private static void ConfigurationError(MermaidParseResult<MermaidDocument> result, MermaidSourceSpan span, string message) =>
        Add(result, span.Line, span.Column, span.Length, MermaidDiagnosticSeverity.Error, message, MermaidDiagnosticCodes.InvalidConfiguration);
}
