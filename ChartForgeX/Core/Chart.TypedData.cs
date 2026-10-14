using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>Adds bubble observations projected once from typed rows. Selectors are evaluated during ingestion; later row changes do not edit the series.</summary>
    public Chart AddBubble<T>(string name, IEnumerable<T> rows, Func<T, ChartBubble> observation, ChartColor? color = null) {
        if (rows == null) throw new ArgumentNullException(nameof(rows));
        if (observation == null) throw new ArgumentNullException(nameof(observation));
        return AddBubble(name, rows.Select(observation), color);
    }

    /// <summary>Adds candlestick observations projected once from typed rows. Selectors are evaluated during ingestion; later row changes do not edit the series.</summary>
    public Chart AddCandlestick<T>(string name, IEnumerable<T> rows, Func<T, ChartCandlestick> observation, ChartColor? color = null) {
        if (rows == null) throw new ArgumentNullException(nameof(rows));
        if (observation == null) throw new ArgumentNullException(nameof(observation));
        return AddCandlestick(name, rows.Select(observation), color);
    }

    /// <summary>Adds ohlc observations projected once from typed rows. Selectors are evaluated during ingestion; later row changes do not edit the series.</summary>
    public Chart AddOhlc<T>(string name, IEnumerable<T> rows, Func<T, ChartCandlestick> observation, ChartColor? color = null) {
        if (rows == null) throw new ArgumentNullException(nameof(rows));
        if (observation == null) throw new ArgumentNullException(nameof(observation));
        return AddOhlc(name, rows.Select(observation), color);
    }

    /// <summary>Adds rangeband observations projected once from typed rows. Selectors are evaluated during ingestion; later row changes do not edit the series.</summary>
    public Chart AddRangeBand<T>(string name, IEnumerable<T> rows, Func<T, ChartRangeBand> observation, ChartColor? color = null) {
        if (rows == null) throw new ArgumentNullException(nameof(rows));
        if (observation == null) throw new ArgumentNullException(nameof(observation));
        return AddRangeBand(name, rows.Select(observation), color);
    }

    /// <summary>Adds rangearea observations projected once from typed rows. Selectors are evaluated during ingestion; later row changes do not edit the series.</summary>
    public Chart AddRangeArea<T>(string name, IEnumerable<T> rows, Func<T, ChartRangeBand> observation, ChartColor? color = null, bool smooth = true) {
        if (rows == null) throw new ArgumentNullException(nameof(rows));
        if (observation == null) throw new ArgumentNullException(nameof(observation));
        return AddRangeArea(name, rows.Select(observation), color, smooth);
    }

    /// <summary>Adds rangebar observations projected once from typed rows. Selectors are evaluated during ingestion; later row changes do not edit the series.</summary>
    public Chart AddRangeBar<T>(string name, IEnumerable<T> rows, Func<T, ChartInterval> observation, ChartColor? color = null) {
        if (rows == null) throw new ArgumentNullException(nameof(rows));
        if (observation == null) throw new ArgumentNullException(nameof(observation));
        return AddRangeBar(name, rows.Select(observation), color);
    }

    /// <summary>Adds errorbar observations projected once from typed rows. Selectors are evaluated during ingestion; later row changes do not edit the series.</summary>
    public Chart AddErrorBar<T>(string name, IEnumerable<T> rows, Func<T, ChartErrorBar> observation, ChartColor? color = null) {
        if (rows == null) throw new ArgumentNullException(nameof(rows));
        if (observation == null) throw new ArgumentNullException(nameof(observation));
        return AddErrorBar(name, rows.Select(observation), color);
    }

    /// <summary>Adds boxplot observations projected once from typed rows. Selectors are evaluated during ingestion; later row changes do not edit the series.</summary>
    public Chart AddBoxPlot<T>(string name, IEnumerable<T> rows, Func<T, ChartBoxPlot> observation, ChartColor? color = null) {
        if (rows == null) throw new ArgumentNullException(nameof(rows));
        if (observation == null) throw new ArgumentNullException(nameof(observation));
        return AddBoxPlot(name, rows.Select(observation), color);
    }

    /// <summary>Adds dumbbell observations projected once from typed rows. Selectors are evaluated during ingestion; later row changes do not edit the series.</summary>
    public Chart AddDumbbell<T>(string name, IEnumerable<T> rows, Func<T, ChartDumbbell> observation, ChartColor? color = null) {
        if (rows == null) throw new ArgumentNullException(nameof(rows));
        if (observation == null) throw new ArgumentNullException(nameof(observation));
        return AddDumbbell(name, rows.Select(observation), color);
    }

    /// <summary>Adds differencearea observations projected once from typed rows. Selectors are evaluated during ingestion; later row changes do not edit the series.</summary>
    public Chart AddDifferenceArea<T>(string name, IEnumerable<T> rows, Func<T, ChartDifferencePoint> observation, ChartColor? color = null) {
        if (rows == null) throw new ArgumentNullException(nameof(rows));
        if (observation == null) throw new ArgumentNullException(nameof(observation));
        return AddDifferenceArea(name, rows.Select(observation), color);
    }

}
