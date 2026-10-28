using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;

namespace ChartForgeX.Topology;

internal sealed partial class VisualTopologyCompiler {
    /// <summary>Resolves the existing finite native face/slot metrics for compiler and detached input comparison.</summary>
    internal static void InitializeOptionsMetrics(VisualRenderContext context, TopologyRenderOptions options) {
        options.ResolvedIconLabelFontSize = context.Theme.Typography.DataLabelSize * (10.5 / 11);
        options.ResolvedSubtitleChipFontSize = context.Theme.Typography.DataLabelSize * (9.5 / 11);
        options.ResolvedEdgeLabelScale = context.Theme.Typography.DataLabelSize / 11;
        options.ResolvedCaptionStrokeWidth = context.Theme.AxisStrokeWidth;
        var badgeFace = new VisualSceneTextFace(context.Font, 600);
        var badgeFontSize = context.Theme.Typography.DataLabelSize * .75;
        badgeFontSize = ChartTextFitting.FitFontSize(Math.Min(8, badgeFontSize), badgeFontSize,
            size => badgeFace.Prepare("Ag", size).Metrics.LineHeight <= 18);
        options.ResolvedBadgeTextWidth = value => badgeFace.Prepare(value, badgeFontSize).Metrics.Width;
        var endpointFace = new VisualSceneTextFace(context.Font, 600);
        var endpointPreferredSize = context.Theme.Typography.DataLabelSize * .8;
        var endpointMetrics = new Dictionary<string, (double FontSize, double Width, double Height)>(StringComparer.Ordinal);
        options.ResolvedEndpointLabelMetrics = text => {
            if (endpointMetrics.TryGetValue(text, out var cached)) return cached;
            var fitted = ChartTextFitting.FitFontSize(Math.Min(8, endpointPreferredSize), endpointPreferredSize, size => {
                var metrics = endpointFace.Prepare(text, size).Metrics;
                return metrics.Width <= 90 && metrics.Height <= 18;
            });
            var measured = endpointFace.Prepare(text, fitted).Metrics;
            var metrics = (fitted, measured.Width + 6, measured.Height + 6);
            endpointMetrics.Add(text, metrics);
            return metrics;
        };
    }
}
