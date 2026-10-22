using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    /// <summary>Applies physical windows to copied ranges after full-data stacking, color resolution and painted fitting.</summary>
    private static void ApplyViewport(Chart chart, ChartCartesianViewport viewport, ChartRange range, ChartRange? secondary,
        ChartStackLayout stacks, ChartBarCoordinateMap coordinates) {
        var horizontal = ChartCartesianOrientation.IsHorizontal(chart);
        ValidateWindow(viewport.X, range.MinX, range.MaxX, chart.Options.XAxis, "X");
        ValidateWindow(viewport.Y, range.MinY, range.MaxY, horizontal ? CategoryProjectionAxis(chart) : chart.Options.YAxis, "Y");
        if (secondary != null) {
            ValidateWindow(viewport.SecondaryX, secondary.MinX, secondary.MaxX, chart.Options.SecondaryXAxis, "secondary X");
            ValidateWindow(viewport.SecondaryY, secondary.MinY, secondary.MaxY, chart.Options.SecondaryYAxis, "secondary Y");
        }
        if (horizontal && viewport.Y != null) {
            range.SetYBounds(viewport.Y.Minimum, viewport.Y.Maximum); secondary?.SetYBounds(viewport.Y.Minimum, viewport.Y.Maximum);
        } else if (!horizontal && viewport.X != null) {
            range.SetXBounds(viewport.X.Minimum, viewport.X.Maximum); secondary?.SetXBounds(viewport.X.Minimum, viewport.X.Maximum);
        }
        var categoryWindow = horizontal ? viewport.Y : viewport.X;
        if (viewport.ValueDomain == ChartValueDomain.VisibleCategories && categoryWindow != null) {
            if ((horizontal ? viewport.X : viewport.Y) == null)
                range.FitVisibleValueDomain(chart, stacks, coordinates, ChartAxisSide.Primary, categoryWindow);
            if (secondary != null && (horizontal ? viewport.SecondaryX : viewport.SecondaryY) == null)
                secondary.FitVisibleValueDomain(chart, stacks, coordinates, ChartAxisSide.Secondary, categoryWindow);
        }
        if (horizontal) {
            if (viewport.X != null) range.SetXBounds(viewport.X.Minimum, viewport.X.Maximum);
            if (viewport.SecondaryX != null) secondary!.SetXBounds(viewport.SecondaryX.Minimum, viewport.SecondaryX.Maximum);
        } else {
            if (viewport.Y != null) range.SetYBounds(viewport.Y.Minimum, viewport.Y.Maximum);
            if (viewport.SecondaryY != null) secondary!.SetYBounds(viewport.SecondaryY.Minimum, viewport.SecondaryY.Maximum);
        }
    }

    private static ChartAxis CategoryProjectionAxis(Chart chart) => chart.Options.YAxis.Scale == ChartScaleKind.Time
        ? chart.Options.YAxis.Clone() : new ChartAxis { Reversed = chart.Options.YAxis.Reversed };

    private static void ValidateWindow(ChartAxisWindow? window, double fullMinimum, double fullMaximum, ChartAxis axis, string name) {
        if (window == null) return;
        ChartTimeSessionValidation.ValidateWindow(axis, window.Minimum, window.Maximum, name + " viewport");
        if (window.Minimum < fullMinimum || window.Maximum > fullMaximum)
            throw new ArgumentOutOfRangeException("viewport", "The " + name + " viewport must be contained in its resolved full domain.");
        var minimum = ChartScaleTransform.Bound(window.Minimum, axis, false); var maximum = ChartScaleTransform.Bound(window.Maximum, axis, true);
        if (!ChartMath.IsFinite(minimum) || !ChartMath.IsFinite(maximum) || maximum <= minimum)
            throw new ArgumentOutOfRangeException("viewport", "The " + name + " viewport requires a finite nonempty projected domain.");
    }

    private static ChartCartesianProjection CaptureAxisProjections(Chart chart, ChartRect plot, ChartRange full, ChartRange range,
        ChartRange? fullSecondary, ChartRange? secondary) {
        var horizontal = ChartCartesianOrientation.IsHorizontal(chart);
        var x = new ChartAxisProjection(full.MinX, full.MaxX, range.MinX, range.MaxX, plot, chart.Options.XAxis, ChartAxisDimension.X);
        var y = new ChartAxisProjection(full.MinY, full.MaxY, range.MinY, range.MaxY, plot,
            horizontal ? CategoryProjectionAxis(chart) : chart.Options.YAxis, ChartAxisDimension.Y);
        var value = secondary == null || fullSecondary == null ? null : horizontal
            ? new ChartAxisProjection(fullSecondary.MinX, fullSecondary.MaxX, secondary.MinX, secondary.MaxX, plot,
                chart.Options.SecondaryXAxis, ChartAxisDimension.X, ChartAxisSide.Secondary)
            : new ChartAxisProjection(fullSecondary.MinY, fullSecondary.MaxY, secondary.MinY, secondary.MaxY, plot,
                chart.Options.SecondaryYAxis, ChartAxisDimension.Y, ChartAxisSide.Secondary);
        return new ChartCartesianProjection(x, y, horizontal ? value : null, horizontal ? null : value, horizontal);
    }

    private static void ValidateFiniteViewportProjection(Chart chart, ChartMapper primary, ChartMapper? secondary) {
        foreach (var series in chart.Series) {
            var map = series.ValueAxis == ChartAxisSide.Secondary ? secondary! : primary;
            var horizontal = ChartCartesianOrientation.IsHorizontal(chart, series);
            for (var raw = 0; raw < series.RenderPoints.Count; raw++) {
                if (series.Kind == ChartSeriesKind.Bubble && raw % 2 != 0) continue;
                var point = series.RenderPoints[raw];
                if (!ChartMath.IsFinite(map.X(horizontal ? point.Y : point.X)) || !ChartMath.IsFinite(map.Y(horizontal ? point.X : point.Y)))
                    throw new ArgumentOutOfRangeException("viewport", "The viewport cannot project source coordinates into finite logical units.");
            }
        }
    }
}
