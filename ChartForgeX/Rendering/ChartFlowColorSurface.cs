using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

/// <summary>Captures every authored flow's independent color fact without consulting weight or paint overrides.</summary>
internal sealed class ChartFlowColorSurface {
    private readonly double?[] _values;

    internal ChartFlowColorSurface(ChartSeries series, VisualThemeColors colors, ChartColorPresentation presentation,
        ChartColorScale? scale, bool enabled = true, bool inferEmptyScale = false) {
        Presentation = presentation;
        _values = series.FlowLinks.Select(link => link.ColorValue).ToArray();
        var values = _values.Where(value => value.HasValue).Select(value => value!.Value).ToArray();
        HasValues = values.Length > 0;
        HasMissing = values.Length < _values.Length;
        Minimum = HasValues ? values.Min() : 0;
        Maximum = HasValues ? values.Max() : 0;
        Scale = enabled ? scale ?? (HasValues || inferEmptyScale ? ChartColorScaleSurface.Default(colors) : null) : null;
    }

    internal ChartColorPresentation Presentation { get; }
    internal ChartColorScale? Scale { get; }
    internal bool HasValues { get; }
    internal bool HasMissing { get; }
    internal double Minimum { get; }
    internal double Maximum { get; }
    internal bool HasDomain => ChartColorScaleSurface.HasDomain(Scale, HasValues ? Minimum : null, HasValues ? Maximum : null);

    internal ChartColorBlend? Blend(int index, VisualThemeColors colors) => Scale == null ? null
        : _values[index] is double value ? Scale.BlendFor(value, Minimum, Maximum) : ChartColorScaleSurface.NoData(Scale, colors);

    internal string AddFacts(Dictionary<string, string> metadata, int index, string description) {
        if (Scale == null && !_values[index].HasValue) return description;
        metadata["data-cfx-label-color"] = Presentation.Name;
        metadata["data-cfx-label-no-data"] = Presentation.NoData;
        if (_values[index] is double value) {
            var formatted = Presentation.Value(value);
            metadata["data-cfx-color-value"] = VisualStateSceneTools.Number(value);
            metadata["data-cfx-formatted-color-value"] = formatted;
            return description + "; " + Presentation.Name + ": " + formatted;
        }
        metadata["data-cfx-color-missing"] = "true";
        return description + "; " + Presentation.Name + ": " + Presentation.NoData;
    }

    /// <summary>Allocates the same measured numeric key for either flow family before proportional geometry is prepared.</summary>
    internal ChartRect AllocateKey(VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot,
        VisualThemeColors colors, string family) {
        if (Scale == null || !Presentation.ShowLegend || !context.Frame.ShowLegend) return plot;
        var style = context.Frame.LegendStyle ?? new TextStyle {
            Font = context.Font, FontSize = context.Theme.Typography.LegendSize, Color = colors.Foreground
        };
        var gap = context.Theme.Spacing / 2;
        var maximum = Math.Max(0, plot.Height * .35);
        var height = VisualColorScaleLegendScene.Height(builder, Scale, Presentation.Labels, style,
            gap, plot.Width, maximum, HasMissing, Presentation.ScaleValues, hasDomain: HasDomain,
            compact: true, min: Minimum, max: Maximum, heading: Presentation.Name, legendStyle: Presentation.LegendStyle);
        var key = new ChartRect(plot.Left, plot.Bottom - height, plot.Width, height);
        VisualColorScaleLegendScene.Draw(builder, Scale, Presentation.Labels, style, gap, key,
            Minimum, Maximum, family + "-color-scale", HasMissing, ChartColorScaleSurface.NoData(Scale, colors),
            Presentation.ScaleValues, family + ".scale-overflow", hasDomain: HasDomain, compact: true,
            heading: Presentation.Name, valueName: Presentation.Name, legendStyle: Presentation.LegendStyle);
        return new ChartRect(plot.Left, plot.Top, plot.Width, Math.Max(0, plot.Height - height - Math.Min(gap, plot.Height * .05)));
    }
}
