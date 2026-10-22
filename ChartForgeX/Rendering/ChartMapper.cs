using ChartForgeX.Primitives;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

internal sealed class ChartMapper {
    private readonly ChartRect _plot;
    private readonly ChartRange _range;
    private readonly ChartAxis _xAxis;
    private readonly ChartAxis _yAxis;

    public ChartMapper(ChartRect plot, ChartRange range, ChartAxis xAxis, ChartAxis yAxis) {
        _plot = plot;
        _range = range;
        _xAxis = xAxis;
        _yAxis = yAxis;
        ValidateRange(range.MinX, range.MaxX, xAxis, "x");
        ValidateRange(range.MinY, range.MaxY, yAxis, "y");
    }

    /// <summary>Creates a mapper whose horizontal-bar category coordinates remain linear.</summary>
    public static ChartMapper ForHorizontalBars(ChartRect plot, ChartRange range, ChartAxis valueAxis, ChartAxis categoryAxis) =>
        new ChartMapper(plot, range, valueAxis, categoryAxis.Scale == ChartScaleKind.Time ? categoryAxis : new ChartAxis { Reversed = categoryAxis.Reversed, PreparedViewportWindow = categoryAxis.PreparedViewportWindow });

    /// <summary>Returns the actual calendar-band pitch without inventing missing source observations.</summary>
    internal double CalendarBandPitch(bool horizontal) {
        var axis = horizontal ? _yAxis : _xAxis;
        if (axis.PreparedTimeDomain?.Unit != true) return double.PositiveInfinity;
        var minimum = horizontal ? _range.MinY : _range.MinX; var maximum = horizontal ? _range.MaxY : _range.MaxX;
        return (horizontal ? _plot.Height : _plot.Width) / (ChartScaleTransform.Bound(maximum, axis, true) - ChartScaleTransform.Bound(minimum, axis, false));
    }

    internal bool HasCalendarBandAxis(bool horizontalAxis) => (horizontalAxis ? _xAxis : _yAxis).PreparedTimeDomain?.Unit == true;

    /// <summary>Tests source membership through the same discrete calendar bands as the painted marks.</summary>
    internal bool OverlapsSourceWindow(ChartAxisWindow? window, double first, double last, bool horizontalAxis) {
        if (window == null) return true;
        var axis = horizontalAxis ? _xAxis : _yAxis;
        if (axis.PreparedTimeDomain?.Unit != true) return window.Overlaps(first, last);
        var start = ChartScaleTransform.Bound(window.Minimum, axis, false); var end = ChartScaleTransform.Bound(window.Maximum, axis, true, window.MaximumInclusive);
        return System.Math.Max(ChartScaleTransform.Forward(first, axis), ChartScaleTransform.Forward(last, axis)) >= start
            && System.Math.Min(ChartScaleTransform.Forward(first, axis), ChartScaleTransform.Forward(last, axis)) < end;
    }

    public double X(double value) => ProjectX(ChartScaleTransform.Normalize(value, _range.MinX, _range.MaxX, _xAxis), _plot);
    internal static double ProjectX(double ratio, ChartRect plot) => plot.Left + ratio * plot.Width;
    public double Y(double value) => ChartMath.InterpolateOrExtrapolateRange(_plot.Bottom, _plot.Top,
        ChartScaleTransform.Normalize(value, _range.MinY, _range.MaxY, _yAxis));

    public double XBaseline() {
        if (_xAxis.Scale == ChartScaleKind.Logarithmic) return X(_xAxis.LogarithmicDomain == ChartLogarithmicDomain.Negative ? _range.MaxX : _range.MinX);
        return System.Math.Min(_plot.Right, System.Math.Max(_plot.Left, X(0)));
    }

    /// <summary>Maps a horizontal cumulative value while treating zero as the selected logarithmic domain's nearest baseline.</summary>
    public double XOrBaseline(double value) => _xAxis.Scale == ChartScaleKind.Logarithmic && value == 0 ? XBaseline() : X(value);

    public double YBaseline() {
        if (_yAxis.Scale == ChartScaleKind.Logarithmic) return Y(_yAxis.LogarithmicDomain == ChartLogarithmicDomain.Negative ? _range.MaxY : _range.MinY);
        return System.Math.Min(_plot.Bottom, System.Math.Max(_plot.Top, Y(0)));
    }

    /// <summary>Maps a stacked value while treating zero as the selected logarithmic domain's nearest baseline.</summary>
    public double YOrBaseline(double value) {
        if (_yAxis.Scale == ChartScaleKind.Logarithmic && value == 0) return YBaseline();
        return Y(value);
    }

    private static void ValidateRange(double minimum, double maximum, ChartAxis axis, string axisName) {
        if (axis.Scale == ChartScaleKind.Logarithmic && (!axis.IsInLogarithmicDomain(minimum) || !axis.IsInLogarithmicDomain(maximum))) {
            throw new System.InvalidOperationException("The " + axisName + " logarithmic axis requires " + axis.LogarithmicDomainName + " data and bounds.");
        }
    }
}
