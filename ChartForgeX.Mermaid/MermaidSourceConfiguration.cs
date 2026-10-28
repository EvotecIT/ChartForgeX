using System;
using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

/// <summary>Retains bounded source configuration and the effective settings for the parsed diagram family.</summary>
public sealed class MermaidSourceConfiguration {
    private readonly List<MermaidConfigurationSetting> _settings = new();
    internal MermaidSequenceSettings? Sequence { get; set; }
    internal MermaidRequirementThemeSettings? RequirementTheme { get; set; }
    /// <summary>Gets the validated authored sequence forceMenus source fact; a source-aware HTML host may select initially expanded participant navigation.</summary>
    /// <remarks>Null means omitted. Static rendering remains menu-free and does not interpret this value.</remarks>
    public bool? SequenceForceMenus { get; internal set; }
    internal MermaidPacketSettings? Packet { get; set; }
    internal MermaidRadarSettings? Radar { get; set; }
    internal MermaidQuadrantSettings? Quadrant { get; set; }
    internal MermaidRadarThemeSettings? RadarTheme { get; set; }
    internal MermaidChartColorSettings? ChartColors { get; set; }
    internal MermaidThemeColorSettings? ThemeColors { get; set; }
    internal MermaidThemePresetInputs? Preset { get; set; }
    internal MermaidSankeySettings? Sankey { get; set; }
    internal MermaidXYChartSettings? XYChart { get; set; }
    internal MermaidTreemapSettings? Treemap { get; set; }
    internal MermaidMindMapSettings? MindMap { get; set; }
    internal MermaidTreeViewSettings? TreeView { get; set; }
    internal MermaidKanbanSettings? Kanban { get; set; }
    internal MermaidIshikawaSettings? Ishikawa { get; set; }
    internal MermaidGitGraphSettings? GitGraph { get; set; }
    internal MermaidC4Settings? C4 { get; set; }
    internal MermaidUseCaseSettings? UseCase { get; set; }
    internal MermaidWardleySettings? Wardley { get; set; }
    internal MermaidCynefinSettings? Cynefin { get; set; }
    internal MermaidSwimlaneSettings? Swimlane { get; set; }
    internal MermaidGraphSettings? Graph { get; set; }

    /// <summary>Gets the effective source theme; MermaidDocument.Theme can also set this value for direct conversion.</summary>
    public string? Theme { get; internal set; }
    /// <summary>Gets the requested layout, retained independently from the native static layout.</summary>
    public string? Layout { get; internal set; }
    /// <summary>Gets the requested appearance, retained independently from native scene styling.</summary>
    public string? Look { get; internal set; }
    /// <summary>Gets the effective font stack from the theme variable or root setting, in that order; native font resolution does not fetch remote fonts.</summary>
    public string? FontFamily { get; internal set; }
    /// <summary>Gets the requested theme-variable base font size in logical pixels, when supported.</summary>
    public double? FontSize { get; internal set; }
    /// <summary>Gets declarations in source precedence order: frontmatter, then legacy directives.</summary>
    public IReadOnlyList<MermaidConfigurationSetting> Settings => _settings.AsReadOnly();

    internal void Add(MermaidConfigurationSetting setting) {
        if (_settings.Count >= 256) throw new ArgumentException("Mermaid configuration exceeds 256 settings.");
        _settings.Add(setting);
    }

    // Replace only the immutable node-kind fact when a YAML mapping/sequence acquires children.
    // Existing public projections, declaration order and diagnostic locations remain unchanged.
    internal void MarkStructured(string path, MermaidSourceSpan span) {
        for (var index = _settings.Count - 1; index >= 0; index--) {
            var row = _settings[index];
            if (row.Path != path || row.Span.Line != span.Line || row.Span.Column != span.Column) continue;
            if (!row.IsStructured) {
                var structured = new MermaidConfigurationSetting(row.Path, row.Value, row.IsString,
                    row.Span, row.ParentPath, row.Key, row.IsMapping, true);
                if (row.Palette != null) structured.WithPalette(new List<string?>(row.Palette));
                _settings[index] = structured;
            }
            return;
        }
    }

    internal MermaidConfigurationSetting? Last(string path) {
        for (var i = _settings.Count - 1; i >= 0; i--) if (_settings[i].Path == path) return _settings[i];
        return null;
    }
}

/// <summary>A retained source setting, including the original declaration location.</summary>
public sealed class MermaidConfigurationSetting {
    internal bool TryGetNumber(out double number) {
        number = 0;
        return !IsString && MermaidConfigurationNumber.TryParse(Value, out number);
    }
    internal MermaidConfigurationSetting(string path, string? value, bool isString, MermaidSourceSpan span)
        : this(path, value, isString, span, null, null, false) { }

    // Only the existing configuration parser supplies leaf identity. A display path alone is not reversible:
    // the literal key A.].[B. and nested keys A. / B. can otherwise have equal Path strings.
    internal MermaidConfigurationSetting(string path, string? value, bool isString, MermaidSourceSpan span,
        string? parentPath, string? key, bool isMapping)
        : this(path, value, isString, span, parentPath, key, isMapping, isMapping) { }

    internal MermaidConfigurationSetting(string path, string? value, bool isString, MermaidSourceSpan span,
        string? parentPath, string? key, bool isMapping, bool isStructured) {
        Path = path; Value = value; IsString = isString; Span = span;
        ParentPath = parentPath; Key = key; IsMapping = isMapping; IsStructured = isStructured;
    }
    internal string? ParentPath { get; }
    internal string? Key { get; }
    internal bool IsMapping { get; }
    internal bool IsStructured { get; }
    internal IReadOnlyList<string?>? Palette {get;private set;}
    internal MermaidConfigurationSetting WithPalette(IList<string?> values) { Palette=new List<string?>(values).AsReadOnly();return this; }
    /// <summary>Gets the dotted configuration path, such as flowchart.theme.</summary>
    public string Path { get; }
    /// <summary>Gets a scalar value; structured values remain in the original source and have no scalar projection.</summary>
    public string? Value { get; }
    /// <summary>Gets whether the scalar was a string rather than a number, boolean or null.</summary>
    public bool IsString { get; }
    /// <summary>Gets the original YAML setting line or legacy directive declaration span.</summary>
    public MermaidSourceSpan Span { get; }
}
