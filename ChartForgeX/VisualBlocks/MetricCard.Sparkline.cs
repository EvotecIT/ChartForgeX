using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.VisualBlocks;

public sealed partial class MetricCard {
    /// <summary>Gets configured sample slots; null indicates the finite-value convenience API is in use.</summary>
    public SparklineData? SparklineData { get; private set; }
    /// <summary>Gets configured secondary sample slots, which share the primary display domain.</summary>
    public SparklineData? SecondarySparklineData { get; private set; }
    internal int SparklineCount => SparklineData?.Values.Count ?? MiniSparkline.Count;
    internal int SecondarySparklineCount => SecondarySparklineData?.Values.Count ?? SecondaryMiniSparkline.Count;
    internal SparklineData GetSparklineData() => SparklineData ?? Core.SparklineData.FromValues(MiniSparkline);
    internal SparklineData GetSecondarySparklineData() => SecondarySparklineData ?? Core.SparklineData.FromValues(SecondaryMiniSparkline);

    /// <summary>Sets shared sparkline samples, retaining missing slots, zeroes and the requested domain.</summary>
    public MetricCard WithSparkline(SparklineData data, ChartColor? color = null, ChartColor? fillColor = null) {
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (data.Values.Count < 2) throw new ArgumentException("Metric card sparklines require at least two sample slots.", nameof(data));
        if (SecondarySparklineCount > 0 && SecondarySparklineCount != data.Values.Count)
            throw new InvalidOperationException("Secondary sparkline sample slots must match the primary count.");
        SparklineData = data; _miniSparkline.Clear();
        foreach (var value in data.Values) if (value.HasValue) _miniSparkline.Add(value.Value);
        MiniSparklineMinimum = data.RequestedMinimum; MiniSparklineMaximum = data.RequestedMaximum;
        MiniSparklineColor = color; MiniSparklineFillColor = fillColor;
        WithoutMiniBars(); return this;
    }

    /// <summary>Sets secondary samples. Bounds belong to the primary sparkline and are shared by both series.</summary>
    public MetricCard WithSecondarySparkline(SparklineData data, ChartColor? color = null) {
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (data.Values.Count < 2) throw new ArgumentException("Secondary sparklines require at least two sample slots.", nameof(data));
        if (SparklineCount > 0 && data.Values.Count != SparklineCount) throw new InvalidOperationException("Secondary sparkline sample slots must match the primary count.");
        if (data.RequestedMinimum.HasValue || data.RequestedMaximum.HasValue)
            throw new ArgumentException("Set shared bounds on the primary sparkline.", nameof(data));
        SecondarySparklineData = data; _secondaryMiniSparkline.Clear();
        foreach (var value in data.Values) if (value.HasValue) _secondaryMiniSparkline.Add(value.Value);
        SecondaryMiniSparklineColor = color; return this;
    }
}
