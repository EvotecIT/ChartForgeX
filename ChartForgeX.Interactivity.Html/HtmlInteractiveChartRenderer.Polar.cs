using System;
using System.Globalization;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Interactivity.Html;

public sealed partial class HtmlInteractiveChartRenderer {
    /// <summary>Prepares one continuous Polar section and its matching detached angle/radius scene.</summary>
    /// <remarks>The host prepares native replies through Chart.PreparePolar. Browser setPolarViewport requests use the same
    /// scene/basis/cancellation owner as other native preparations. Automatic gestures require the existing native connection to opt in with gestures: true; the host resolves them through its captured projection.</remarks>
    public HtmlChartSection PreparePolarSection(Chart chart, ChartPolarViewport? viewport = null, Action<HtmlChartInteractionOptions>? configure = null) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        var options = new HtmlChartInteractionOptions(); configure?.Invoke(options);
        return PrepareChartSection(chart, options, options.PageTitle ?? ChartTitle(chart, "ChartForgeX interactive chart"), "host", null,
            polarViewport: viewport, requirePolar: true);
    }
    private static void AppendPreparedPolar(StringBuilder json, PreparedVisual prepared) {
        var polar = prepared.PolarProjection;
        if (polar == null) return;
        var angle = polar.AngleWindow;
        json.Append(",\"polarProjection\":{\"angle\":{\"minimum\":").Append(angle.Minimum.ToString("R", CultureInfo.InvariantCulture))
            .Append(",\"maximum\":").Append(angle.Maximum.ToString("R", CultureInfo.InvariantCulture))
            .Append(",\"maximumInclusive\":").Append(angle.MaximumInclusive ? "true" : "false")
            .Append("},\"centerX\":").Append(polar.Center.X.ToString("R", CultureInfo.InvariantCulture))
            .Append(",\"centerY\":").Append(polar.Center.Y.ToString("R", CultureInfo.InvariantCulture))
            .Append(",\"outerRadius\":").Append(polar.OuterRadius.ToString("R", CultureInfo.InvariantCulture))
            .Append(",\"rotationRadians\":").Append(polar.RotationRadians.ToString("R", CultureInfo.InvariantCulture))
            .Append(",\"angleReversed\":").Append(polar.AngleReversed ? "true" : "false");
        var bounds = new ChartRect(polar.Center.X - polar.OuterRadius, polar.Center.Y - polar.OuterRadius, polar.OuterRadius * 2, polar.OuterRadius * 2);
        AppendPolarRadius(json, "radius", polar.Radius, bounds);
        AppendPolarRadius(json, "secondaryRadius", polar.SecondaryRadius, bounds);
        json.Append('}');
    }
    private static void AppendPolarRadius(StringBuilder json, string name, ChartViewportAxisProjection? axis, ChartRect bounds) {
        if (axis == null) return;
        json.Append(',').Append(HtmlJsonString.Encode(name)).Append(':');
        AppendViewportAxis(json, axis.FullMinimum, axis.FullMaximum, axis.Minimum, axis.Maximum,
            axis.TransformedFullMinimum, axis.TransformedFullMaximum, axis.TransformedMinimum, axis.TransformedMaximum,
            axis.Scale, axis.Threshold, axis.Reversed, bounds, axis.TimeSessions, axis.LogarithmBase, axis.LogarithmicDomain, axis.TimeDomain, axis.MaximumInclusive, axis.LogicalMinimum, axis.LogicalMaximum);
    }
}
