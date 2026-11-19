using System;
using System.Collections.Generic;
using System.Linq;

namespace ChartForgeX.Topology;

/// <summary>
/// Packs dense group panels into equal columns that fill the row width. Panels take the columns in declared order, left
/// to right and then from the left again, each directly below the panel above it, unless another column is free more
/// than a gutter higher. Neighbours in the declared order mostly stay neighbours on the canvas, and a short panel no
/// longer leaves the height of a tall row-mate empty below it. An
/// unusually wide panel spans adjacent columns. Every panel is stretched to the width it takes and keeps a gutter to its
/// right and below for routes.
/// </summary>
internal sealed class DensePanelColumns {
    // Panels up to this factor of the median width share one column width; wider panels span columns.
    private const double OrdinaryWidthFactor = 1.25;
    private readonly double _left;
    private readonly double _gap;
    private readonly double[] _tops;
    private int _next;

    internal DensePanelColumns(double left, double top, double width, double gap, IReadOnlyList<double> panelWidths) {
        _left = left; _gap = gap;
        var ordered = panelWidths.OrderBy(value => value).ToArray();
        var median = ordered.Length == 0 ? width : ordered[ordered.Length / 2];
        var column = ordered.Where(value => value <= median * OrdinaryWidthFactor).DefaultIfEmpty(median).Max();
        var count = Math.Max(1, (int)Math.Floor((width + gap + .001) / (column + gap)));
        ColumnWidth = (width + gap) / count - gap;
        _tops = Enumerable.Repeat(top, count).ToArray();
    }

    internal double ColumnWidth { get; }

    /// <summary>Keeps automatic panels out of a pinned panel: every column it overlaps continues below it.</summary>
    internal void Reserve(double x, double y, double width, double height) {
        for (var i = 0; i < _tops.Length; i++) {
            var left = _left + i * (ColumnWidth + _gap);
            if (left < x + width + _gap && x < left + ColumnWidth + _gap) _tops[i] = Math.Max(_tops[i], y + height + _gap);
        }
    }

    /// <summary>Places one panel and returns its position and the width it is stretched to.</summary>
    internal (double X, double Y, double Width) Place(double width, double height) {
        var span = Math.Min(_tops.Length, Math.Max(1, (int)Math.Ceiling((width + _gap - .001) / (ColumnWidth + _gap))));
        // The next column in order wins unless another place is higher by more than a gutter, so order survives small differences.
        var starts = Enumerable.Range(0, _tops.Length - span + 1).ToArray();
        double Top(int at) => _tops.Skip(at).Take(span).Max();
        var lowest = starts.Min(Top);
        var start = starts.OrderBy(at => (at - _next + _tops.Length) % _tops.Length).First(at => Top(at) <= lowest + _gap);
        var top = Top(start);
        for (var i = start; i < start + span; i++) _tops[i] = top + height + _gap;
        _next = (start + span) % _tops.Length;
        return (_left + start * (ColumnWidth + _gap), top, Math.Max(width, span * ColumnWidth + (span - 1) * _gap));
    }
}
