using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

/// <summary>Captures full radius domains before changing only the detached presentation axes.</summary>
internal sealed class ChartPolarPreparation {
    private readonly Dictionary<ChartAxisSide, RadialValueScale> _full;
    private readonly Dictionary<ChartAxisSide, ChartPolarRadiusView> _views = new();
    private ChartPolarPreparation(ChartPolarViewport? request, Dictionary<ChartAxisSide, RadialValueScale> full) { Request = request; _full = full; }
    internal ChartPolarViewport? Request { get; }
    internal ChartPolarProjection? Projection { get; private set; }
    internal static ChartPolarPreparation? Resolve(Chart chart, ChartPolarViewport? request, bool required) {
        var continuous = chart.Series.Count > 0 && chart.Series.All(series => series.Kind == ChartSeriesKind.Polar);
        if (!continuous) {
            if (required || request != null) throw new NotSupportedException("Polar viewports require continuous Polar series; category radar, polar area and numeric-radial mixtures are not directional views.");
            return null;
        }
        if (!chart.Series.Any(series => series.RenderPoints.Count > 0)) {
            if (request != null) throw new InvalidOperationException("A polar viewport requires source observations.");
            return null;
        }
        var full = RadialValueScale.ForAxes(chart, index => chart.Series[index].RenderPoints.Select(point => point.Y), "Polar");
        foreach (var side in full.Keys.ToArray()) full[side] = full[side].Detached();
        var result = new ChartPolarPreparation(request, full);
        result.Apply(chart, ChartAxisSide.Primary, request?.Radius);
        result.Apply(chart, ChartAxisSide.Secondary, request?.SecondaryRadius);
        return result;
    }
    private void Apply(Chart chart, ChartAxisSide side, ChartAxisWindow? window) {
        if (window == null) return;
        if (!_full.TryGetValue(side, out var full)) throw new NotSupportedException("The polar viewport requests an unavailable radius axis.");
        if (window.Minimum < full.Minimum || window.Maximum > full.Maximum)
            throw new ArgumentOutOfRangeException(nameof(window), "A radius window must lie within its resolved full source domain; use the captured ConstrainRadius method for gestures.");
        var axis = RadialValueScale.Axis(chart, side);
        var validation = new ChartViewportAxisProjection(full.Minimum, full.Maximum, full.Minimum, full.Maximum, 0, 1, axis, false);
        if (validation.ConstrainSource(window) == null) return;
        _views.Add(side, new ChartPolarRadiusView(full, window));
        axis.Minimum = window.Minimum; axis.Maximum = window.Maximum; axis.PreparedViewportWindow = window;
    }
    internal void Capture(Chart chart, ChartPoint center, double outerRadius, double rotation,
        Dictionary<ChartAxisSide, RadialValueScale> current) {
        ChartViewportAxisProjection? Axis(ChartAxisSide side) {
            if (!_full.TryGetValue(side, out var full) || !current.TryGetValue(side, out var visible)) return null;
            var inner = _views.TryGetValue(side, out var view) ? outerRadius * view.InnerFraction : 0;
            if (view != null && !ChartMath.IsFinite(outerRadius * view.Magnification))
                throw new ArgumentOutOfRangeException(nameof(outerRadius), "polar.viewport-unrepresentable: the captured full-domain geometry cannot be represented at this logical viewport.");
            return new ChartViewportAxisProjection(full.Minimum, full.Maximum, visible.Minimum, visible.Maximum,
                inner, outerRadius, RadialValueScale.Axis(chart, side), false);
        }
        Projection = new ChartPolarProjection(Request, center, outerRadius, rotation, chart.Options.XAxis.Reversed,
            Axis(ChartAxisSide.Primary), Axis(ChartAxisSide.Secondary));
    }
    internal double Magnification(ChartAxisSide side) => _views.TryGetValue(side, out var view) ? view.Magnification : 1;
    internal bool HasRadiusWindow(ChartAxisSide side) => _views.ContainsKey(side);
    internal double InnerFraction(ChartAxisSide side) => _views.TryGetValue(side, out var view) ? view.InnerFraction : 0;
    internal double Normalize(ChartAxisSide side, RadialValueScale current, double source) =>
        _views.TryGetValue(side, out var view) ? view.Normalize(source) : current.Normalize(source);
    internal double BaseNormalize(ChartAxisSide side, RadialValueScale current, double source) =>
        _views.TryGetValue(side, out var view) ? view.BaseNormalize(source) : current.Normalize(source);
    internal double Guide(ChartAxisSide side, double fraction) => _views.TryGetValue(side, out var view) ? view.Guide(fraction) : fraction;
    internal bool Visible(ChartSeries series, ChartPoint point) {
        if (Request == null) return true;
        return Projection!.Contains(point.X, point.Y, series.ValueAxis);
    }
}
