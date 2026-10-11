using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Mermaid;

/// <summary>Validates radar configuration once before mapping it into the shared chart owners.</summary>
internal sealed class MermaidRadarSettings {
    private readonly HashSet<string> _handled = new HashSet<string>(StringComparer.Ordinal);
    internal int? Width { get; private set; }
    internal int? Height { get; private set; }
    internal double? MarginLeft { get; private set; }
    internal double? MarginTop { get; private set; }
    internal double? MarginRight { get; private set; }
    internal double? MarginBottom { get; private set; }
    internal double? AxisScaleFactor { get; private set; }
    internal double? AxisLabelFactor { get; private set; }
    internal double? CurveTension { get; private set; }

    internal static bool IsScalarPath(string path) => path == "radar.width" || path == "radar.height"
        || path == "radar.marginLeft" || path == "radar.marginTop" || path == "radar.marginRight" || path == "radar.marginBottom"
        || path == "radar.axisScaleFactor" || path == "radar.axisLabelFactor" || path == "radar.curveTension";

    internal bool Handles(string path) {
        foreach (var handled in _handled)
            if (path == handled || path.StartsWith(handled + ".", StringComparison.Ordinal)) return true;
        return false;
    }

    internal static MermaidRadarSettings Read(MermaidSourceConfiguration configuration, Action<MermaidConfigurationSetting, string> error) {
        var settings = new MermaidRadarSettings();
        settings.Width = Dimension("width"); settings.Height = Dimension("height");
        settings.MarginLeft = Number("marginLeft"); settings.MarginTop = Number("marginTop");
        settings.MarginRight = Number("marginRight"); settings.MarginBottom = Number("marginBottom");
        settings.AxisScaleFactor = Number("axisScaleFactor"); settings.AxisLabelFactor = Number("axisLabelFactor");
        settings.CurveTension = Number("curveTension", 1);
        return settings;

        double? Number(string key, double maximum = double.MaxValue) {
            var setting = configuration.Last("radar." + key);
            if (setting == null) return null;
            settings._handled.Add(setting.Path);
            if (setting.TryGetNumber(out var value) && value >= 0 && value <= maximum) return value;
            error(setting, "Mermaid configuration '" + setting.Path + "' must be a finite nonnegative number"
                + (maximum == 1 ? " from zero through one." : "."));
            return null;
        }
        int? Dimension(string key) {
            var value = Number(key);
            if (!value.HasValue) return null;
            if (value > 0 && value <= int.MaxValue && Math.Truncate(value.Value) == value.Value) return (int)value.Value;
            var setting = configuration.Last("radar." + key)!;
            error(setting, "Mermaid configuration '" + setting.Path + "' must be a positive whole pixel dimension within the native integer range.");
            return null;
        }
    }

    internal void Apply(Chart chart, MermaidRadarRenderOptions host) {
        chart.Options.PolarAxisScaleFactor = host.AxisScaleFactor ?? AxisScaleFactor ?? 1;
        chart.Options.PolarAxisLabelFactor = host.AxisLabelFactor ?? AxisLabelFactor ?? 1.05;
        if (host.Padding.HasValue) chart.Options.Padding = host.Padding.Value;
        else if (MarginLeft.HasValue || MarginTop.HasValue || MarginRight.HasValue || MarginBottom.HasValue) {
            var padding = chart.Options.Padding;
            chart.Options.Padding = new ChartPadding(MarginLeft ?? padding.Left, MarginTop ?? padding.Top,
                MarginRight ?? padding.Right, MarginBottom ?? padding.Bottom);
        }
    }
}
