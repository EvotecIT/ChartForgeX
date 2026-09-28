using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>
    /// Adds a Pareto chart by normalizing raw category values to percentages and adding a cumulative percentage line.
    /// </summary>
    /// <param name="name">The series name.</param>
    /// <param name="items">The labeled values to sort and normalize.</param>
    /// <param name="barColor">An optional bar color.</param>
    /// <param name="cumulativeColor">An optional cumulative line color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddPareto(string name, IEnumerable<ChartParetoItem> items, ChartColor? barColor = null, ChartColor? cumulativeColor = null) {
        EnsureCanAddSeries();
        if (items == null) throw new ArgumentNullException(nameof(items));
        var materialized = items.ToArray();
        if (materialized.Length == 0) throw new ArgumentException("Pareto charts must contain at least one item.", nameof(items));

        var total = materialized.Sum(item => item.Value);
        if (total <= 0) throw new ArgumentException("Pareto charts must contain at least one positive item value.", nameof(items));

        var sorted = materialized
            .Select((item, index) => new ParetoItem(item, index))
            .OrderByDescending(item => item.Item.Value)
            .ThenBy(item => item.Index)
            .ToArray();

        Options.XAxisLabels.Clear();
        var bars = new List<ChartPoint>(sorted.Length);
        var cumulative = new List<ChartPoint>(sorted.Length);
        var running = 0.0;
        for (var i = 0; i < sorted.Length; i++) {
            var x = i + 1;
            var percent = sorted[i].Item.Value / total * 100.0;
            running += percent;
            if (i == sorted.Length - 1) running = 100.0;
            Options.XAxisLabels.Add(new ChartAxisLabel(x, sorted[i].Item.Label));
            bars.Add(new ChartPoint(x, percent));
            cumulative.Add(new ChartPoint(x, running));
        }

        Add(name, ChartSeriesKind.Bar, bars, barColor);
        Series[Series.Count - 1].ShowDataLabels = false;
        Add("Cumulative " + name, ChartSeriesKind.Line, cumulative, cumulativeColor ?? Options.Theme.Warning);
        return this;
    }

    private readonly struct ParetoItem {
        public readonly ChartParetoItem Item;
        public readonly int Index;

        public ParetoItem(ChartParetoItem item, int index) {
            Item = item;
            Index = index;
        }
    }
}
