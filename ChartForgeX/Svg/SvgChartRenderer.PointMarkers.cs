using System.Collections.Generic;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawOptionalLineMarkers(StringBuilder sb, Chart chart, ChartSeries series, int seriesIndex, IReadOnlyList<ChartPoint> mapped, double markerRadius, bool includeInteractionTargets) {
        if (!ChartSeriesKindTraits.UsesOptionalLineMarker(series.Kind)) return;
        if (chart.Options.IsSparkline && !series.PreserveInteractionTargetsWhenMarkersHidden) return;
        if (markerRadius <= 0 && (!series.PreserveInteractionTargetsWhenMarkersHidden || !includeInteractionTargets)) return;

        for (var pointIndex = 0; pointIndex < mapped.Count; pointIndex++) {
            var point = mapped[pointIndex];
            var raw = series.Points[pointIndex];
            var markerColor = PointColor(chart, series, seriesIndex, pointIndex);
            var hiddenInteractionTarget = markerRadius <= 0;
            AppendSvg(sb, writer => writer.StartElement("circle")
                .Attribute("class", hiddenInteractionTarget ? "cfx-point-interaction-target" : null)
                .Attribute("data-cfx-role", hiddenInteractionTarget ? "line-point-target" : "line-marker")
                .Attribute("data-cfx-series", seriesIndex)
                .Attribute("data-cfx-point", pointIndex)
                .Attribute("data-cfx-x", raw.X)
                .Attribute("data-cfx-y", raw.Y)
                .Attribute("cx", point.X)
                .Attribute("cy", point.Y)
                .Attribute("r", hiddenInteractionTarget ? ChartVisualPrimitives.HiddenMarkerInteractionRadius : markerRadius)
                .Attribute("fill", markerColor.ToCss())
                .Attribute("opacity", hiddenInteractionTarget ? "0" : null)
                .Attribute("pointer-events", hiddenInteractionTarget ? "all" : null)
                .Attribute("stroke", chart.Options.Theme.CardBackground.ToCss())
                .Attribute("stroke-width", ChartVisualPrimitives.MarkerStrokeWidth)
                .EndEmptyElement()
                .Line());
        }
    }
}
