using System;
using ChartForgeX.Core;

namespace ChartForgeX.Composition;

public sealed partial class VisualCanvasInfoTileLayer {
    /// <summary>Gets configured sample slots, including missing observations.</summary>
    public SparklineData? SparklineData { get; private set; }
    internal int MiniChartSampleCount => SparklineData?.Values.Count ?? MiniChartValues.Count;

    /// <summary>Sets shared compact samples and presentation, retaining missing slots and real zeroes.</summary>
    public VisualCanvasInfoTileLayer WithSparkline(SparklineData data, SparklineStyle style = SparklineStyle.Line) {
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (!Enum.IsDefined(typeof(SparklineStyle), style)) throw new ArgumentOutOfRangeException(nameof(style));
        SparklineData = data; _miniChartValues.Clear();
        foreach (var value in data.Values) if (value.HasValue) _miniChartValues.Add(value.Value);
        MiniChartKind = style == SparklineStyle.Bars ? VisualCanvasInfoTileMiniChartKind.Bars
            : style == SparklineStyle.Area ? VisualCanvasInfoTileMiniChartKind.Area : VisualCanvasInfoTileMiniChartKind.Sparkline;
        MiniChartMaximum = null; return this;
    }

    internal SparklineData GetSparklineData() {
        if (SparklineData == null) return Core.SparklineData.FromValues(MiniChartValues, maximum: MiniChartMaximum, includeZero: true);
        return MiniChartMaximum.HasValue
            ? new SparklineData(SparklineData.Values, SparklineData.RequestedMinimum, MiniChartMaximum, SparklineData.MissingDataPolicy, SparklineData.IncludeZero)
            : SparklineData;
    }
}
