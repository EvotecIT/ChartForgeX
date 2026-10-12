using System;
using System.Collections.Generic;
using System.Xml;
using ChartForgeX.Core;

namespace ChartForgeX.Mermaid;

/// <summary>Validates source scalars once and maps them into the existing native Sankey owners.</summary>
internal sealed class MermaidSankeySettings {
    private readonly HashSet<string> _handled = new HashSet<string>(StringComparer.Ordinal);
    internal int? Width { get; private set; }
    internal int? Height { get; private set; }
    internal double? NodeWidth { get; private set; }
    internal double? NodePadding { get; private set; }
    internal ChartSankeyAlignment? Alignment { get; private set; }
    internal bool? ShowValues { get; private set; }
    internal string? Prefix { get; private set; }
    internal string? Suffix { get; private set; }

    internal static bool IsScalarPath(string path) => path == "sankey.width" || path == "sankey.height"
        || path == "sankey.nodeWidth" || path == "sankey.nodePadding" || path == "sankey.nodeAlignment"
        || path == "sankey.showValues" || path == "sankey.prefix" || path == "sankey.suffix";

    internal bool Handles(string path) {
        foreach (var handled in _handled)
            if (path == handled || path.StartsWith(handled + ".", StringComparison.Ordinal)) return true;
        return false;
    }

    internal static MermaidSankeySettings Read(MermaidSourceConfiguration configuration,
        Action<MermaidConfigurationSetting, string> error) {
        var settings = new MermaidSankeySettings();
        settings.Width = Dimension("width"); settings.Height = Dimension("height");
        settings.NodeWidth = Number("nodeWidth", false); settings.NodePadding = Number("nodePadding", true);
        settings.Prefix = Text("prefix"); settings.Suffix = Text("suffix");
        var values = Select("showValues");
        if (values != null) {
            if (!values.IsString && bool.TryParse(values.Value, out var show)) settings.ShowValues = show;
            else Invalid(values, "must be a boolean");
        }
        var alignment = Select("nodeAlignment");
        if (alignment != null) {
            if (!alignment.IsString) Invalid(alignment, "must be left, right, center or justify");
            else switch (alignment.Value) {
                case "left": settings.Alignment = ChartSankeyAlignment.Left; break;
                case "right": settings.Alignment = ChartSankeyAlignment.Right; break;
                case "center": settings.Alignment = ChartSankeyAlignment.CenterSources; break;
                case "justify": settings.Alignment = ChartSankeyAlignment.Justify; break;
                default: Invalid(alignment, "must be left, right, center or justify"); break;
            }
        }
        return settings;

        MermaidConfigurationSetting? Select(string key) {
            var setting = configuration.Last("sankey." + key);
            if (setting != null) settings._handled.Add(setting.Path);
            return setting;
        }
        void Invalid(MermaidConfigurationSetting setting, string requirement) =>
            error(setting, "Mermaid configuration '" + setting.Path + "' " + requirement + ".");
        double? Number(string key, bool zero) {
            var setting = Select(key);
            if (setting == null) return null;
            if (setting.TryGetNumber(out var value) && (zero ? value >= 0 : value > 0)) return value;
            Invalid(setting, zero ? "must be a finite non-negative number" : "must be a finite positive number");
            return null;
        }
        int? Dimension(string key) {
            var value = Number(key, false);
            if (!value.HasValue) return null;
            if (value.Value <= int.MaxValue && Math.Truncate(value.Value) == value.Value) return (int)value.Value;
            Invalid(configuration.Last("sankey." + key)!, "must be a positive whole pixel dimension within the native integer range");
            return null;
        }
        string? Text(string key) {
            var setting = Select(key);
            if (setting == null) return null;
            if (setting.IsString && setting.Value != null && setting.Value.Length <= 4096) {
                try { return XmlConvert.VerifyXmlChars(setting.Value); }
                catch (XmlException) {
                    Invalid(setting, "must contain complete XML-compatible Unicode characters");
                    return null;
                }
            }
            Invalid(setting, "must be a string of at most 4096 characters");
            return null;
        }
    }

    internal void Apply(Chart chart) {
        var options = chart.Options.Sankey;
        if (NodeWidth.HasValue) options.NodeWidth = NodeWidth.Value;
        if (NodePadding.HasValue) options.NodeGap = NodePadding.Value;
        if (Alignment.HasValue) options.Alignment = Alignment.Value;
        if (ShowValues.HasValue) options.ShowValues = ShowValues.Value;
        if (Prefix != null || Suffix != null) {
            var format = chart.Options.ValueFormat;
            var prefix = Prefix ?? string.Empty; var suffix = Suffix ?? string.Empty;
            chart.Options.ValueFormat = ChartValueFormat.Custom(value => prefix + format.Format(value) + suffix);
        }
    }
}
