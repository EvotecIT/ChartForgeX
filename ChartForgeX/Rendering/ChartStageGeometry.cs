using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

/// <summary>Maps stage cross-axis/process-axis geometry once for vertical and horizontal static producers.</summary>
internal readonly struct ChartStageGeometry {
    private readonly ChartRect _plot;
    private readonly bool _horizontal;

    internal ChartStageGeometry(ChartRect plot, ChartOrientation orientation) {
        _plot = plot;
        _horizontal = orientation == ChartOrientation.Horizontal;
    }

    internal ChartPoint Position(double cross, double process) => _horizontal
        ? new ChartPoint(_plot.Left + process, _plot.Top + cross)
        : new ChartPoint(_plot.Left + cross, _plot.Top + process);

    internal ChartRect Rectangle(double cross, double process, double crossExtent, double processExtent) => _horizontal
        ? new ChartRect(_plot.Left + process, _plot.Top + cross, processExtent, crossExtent)
        : new ChartRect(_plot.Left + cross, _plot.Top + process, crossExtent, processExtent);

    internal ChartPath Polygon(double center, double firstWidth, double firstProcess, double lastWidth, double lastProcess) {
        var a = Position(center - firstWidth / 2, firstProcess); var b = Position(center + firstWidth / 2, firstProcess);
        var c = Position(center + lastWidth / 2, lastProcess); var d = Position(center - lastWidth / 2, lastProcess);
        return new ChartPath(new[] { ChartPathCommand.MoveTo(a.X, a.Y), ChartPathCommand.LineTo(b.X, b.Y),
            ChartPathCommand.LineTo(c.X, c.Y), ChartPathCommand.LineTo(d.X, d.Y) });
    }
}
