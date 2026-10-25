using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.SvgRaster;

namespace ChartForgeX.Mermaid;

internal sealed partial class MermaidSankeySettings {
    private readonly Dictionary<string, ChartColor> _nodeColors = new(StringComparer.Ordinal);
    private ChartSankeyFlowColorMode? _flowColorMode;
    private ChartColor? _ribbonFill;
    private ChartSankeyLabelStyle? _labelStyle;

    internal static bool IsNodeColorsPath(string path) => path == "sankey.nodeColors";
    private static bool IsPaintScalarPath(string path) => path == "sankey.linkColor" || path == "sankey.labelStyle";

    private void ReadPaint(MermaidSourceConfiguration configuration, Action<MermaidConfigurationSetting, string> error) {
        var linkColor = Select("sankey.linkColor");
        if (linkColor != null) {
            if (!linkColor.IsString) Invalid(linkColor, "must be source, target, gradient or a supported literal color string");
            else switch (linkColor.Value) {
                case "source": _flowColorMode = ChartSankeyFlowColorMode.Source; break;
                case "target": _flowColorMode = ChartSankeyFlowColorMode.Target; break;
                case "gradient": _flowColorMode = ChartSankeyFlowColorMode.Gradient; break;
                default:
                    if (SvgRasterColor.TryParse(linkColor.Value, out var color)) _ribbonFill = color;
                    else Invalid(linkColor, "must be source, target, gradient or a supported literal color string");
                    break;
            }
        }
        var labelStyle = Select("sankey.labelStyle");
        if (labelStyle != null) {
            if (labelStyle.IsString && labelStyle.Value == "legacy") _labelStyle = ChartSankeyLabelStyle.Plain;
            else if (labelStyle.IsString && labelStyle.Value == "outlined") _labelStyle = ChartSankeyLabelStyle.Outlined;
            else Invalid(labelStyle, "must be legacy or outlined");
        }
        var members = new Dictionary<string, MermaidConfigurationSetting>(StringComparer.Ordinal);
        foreach (var setting in configuration.Settings) {
            if (IsNodeColorsPath(setting.Path)) {
                _handled.Add(setting.Path);
                if (ReferenceEquals(configuration.Last(setting.Path), setting) && !setting.IsMapping)
                    Invalid(setting, "must be an object whose exact node-ID keys have supported literal color string values");
            } else if (setting.Path.StartsWith("sankey.nodeColors.", StringComparison.Ordinal)) {
                _handled.Add(setting.Path);
                if (setting.ParentPath != "sankey.nodeColors" || setting.Key == null) {
                    Invalid(setting, "must be a direct node-ID color entry, not a nested mapping");
                    continue;
                }
                // The existing configuration parser retains the decoded key. Path is display metadata, not an ID parser.
                members[setting.Key] = setting;
            }
        }
        foreach (var entry in members) {
            var setting = entry.Value;
            if (setting.IsString && SvgRasterColor.TryParse(setting.Value, out var color)) _nodeColors.Add(entry.Key, color);
            else Invalid(setting, "must be a supported literal color string");
        }

        MermaidConfigurationSetting? Select(string path) {
            var setting = configuration.Last(path);
            if (setting != null) _handled.Add(path);
            return setting;
        }
        void Invalid(MermaidConfigurationSetting setting, string requirement) =>
            error(setting, "Mermaid configuration '" + setting.Path + "' " + requirement + ".");
    }

    private void ApplyPaint(Chart chart) {
        var options = chart.Options.Sankey;
        if (_flowColorMode.HasValue) options.FlowColorMode = _flowColorMode.Value;
        if (_ribbonFill.HasValue) {
            // A source literal colors the complete link independently of nodeColors. The existing ID-keyed flow owner
            // has exactly that precedence, whereas the family fallback deliberately yields to point colors.
            var series = chart.Series[0];
            foreach (var link in series.FlowLinks) series.WithFlowStyle(link.Id, new ChartFlowStyle(fill: _ribbonFill.Value));
        }
        if (_labelStyle.HasValue) options.LabelStyle = _labelStyle.Value;
        var nodes = chart.Series[0].Nodes;
        for (var index = 0; index < nodes.Count; index++)
            if (_nodeColors.TryGetValue(nodes[index].Id, out var color)) chart.Series[0].WithPointColor(index, color);
    }
}
