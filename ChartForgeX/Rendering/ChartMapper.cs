using ChartForgeX.Primitives;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

internal sealed class ChartMapper {
    private static readonly ChartAxis LinearCategoryAxis = new ChartAxis();
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
    public static ChartMapper ForHorizontalBars(ChartRect plot, ChartRange range, ChartAxis valueAxis) =>
        new ChartMapper(plot, range, valueAxis, LinearCategoryAxis);

    public double X(double value) => _plot.Left + ChartScaleTransform.Normalize(value, _range.MinX, _range.MaxX, _xAxis) * _plot.Width;
    public double Y(double value) => _plot.Bottom - ChartScaleTransform.Normalize(value, _range.MinY, _range.MaxY, _yAxis) * _plot.Height;

    public double XBaseline() {
        if (_xAxis.Scale == ChartScaleKind.Logarithmic) return _plot.Left;
        return System.Math.Min(_plot.Right, System.Math.Max(_plot.Left, X(0)));
    }

    public double YBaseline() {
        if (_yAxis.Scale == ChartScaleKind.Logarithmic) return _plot.Bottom;
        return System.Math.Min(_plot.Bottom, System.Math.Max(_plot.Top, Y(0)));
    }

    /// <summary>Maps a stacked value while treating zero as the positive logarithmic baseline.</summary>
    public double YOrBaseline(double value) {
        if (_yAxis.Scale == ChartScaleKind.Logarithmic && value == 0) return YBaseline();
        return Y(value);
    }

    private static void ValidateRange(double minimum, double maximum, ChartAxis axis, string axisName) {
        if (axis.Scale == ChartScaleKind.Logarithmic && (minimum <= 0 || maximum <= 0)) {
            throw new System.InvalidOperationException("The " + axisName + " logarithmic axis requires positive data and bounds.");
        }
    }
}
