using System;
using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

/// <summary>Retains bounded source configuration and the effective settings for the parsed diagram family.</summary>
public sealed class MermaidSourceConfiguration {
    private readonly List<MermaidConfigurationSetting> _settings = new();

    /// <summary>Gets the effective source theme; MermaidDocument.Theme can also set this value for direct conversion.</summary>
    public string? Theme { get; internal set; }
    /// <summary>Gets the requested layout, retained independently from the native static layout.</summary>
    public string? Layout { get; internal set; }
    /// <summary>Gets the requested appearance, retained independently from native scene styling.</summary>
    public string? Look { get; internal set; }
    /// <summary>Gets the requested font stack; native font resolution does not fetch remote fonts.</summary>
    public string? FontFamily { get; internal set; }
    /// <summary>Gets declarations in source precedence order: frontmatter, then legacy directives.</summary>
    public IReadOnlyList<MermaidConfigurationSetting> Settings => _settings.AsReadOnly();

    internal void Add(MermaidConfigurationSetting setting) {
        if (_settings.Count >= 256) throw new ArgumentException("Mermaid configuration exceeds 256 settings.");
        _settings.Add(setting);
    }

    internal MermaidConfigurationSetting? Last(string path) {
        for (var i = _settings.Count - 1; i >= 0; i--) if (_settings[i].Path == path) return _settings[i];
        return null;
    }
}

/// <summary>A retained source setting, including the original declaration location.</summary>
public sealed class MermaidConfigurationSetting {
    internal MermaidConfigurationSetting(string path, string? value, bool isString, MermaidSourceSpan span) {
        Path = path; Value = value; IsString = isString; Span = span;
    }
    /// <summary>Gets the dotted configuration path, such as flowchart.theme.</summary>
    public string Path { get; }
    /// <summary>Gets a scalar value; structured values remain in the original source and have no scalar projection.</summary>
    public string? Value { get; }
    /// <summary>Gets whether the scalar was a string rather than a number, boolean or null.</summary>
    public bool IsString { get; }
    /// <summary>Gets the original YAML setting line or legacy directive declaration span.</summary>
    public MermaidSourceSpan Span { get; }
}
