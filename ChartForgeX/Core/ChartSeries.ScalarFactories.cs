using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    /// <summary>Creates a gauge with one authored measured value and scale.</summary>
    public static ChartSeries CreateGauge(ChartSeriesId id, string name, ChartGaugeValue value, ChartColor? color = null) =>
        Scalar(id, name, ChartSeriesKind.Gauge, value, color);

    /// <summary>Creates a gauge with one authored measured value and scale.</summary>
    public static ChartSeries CreateGauge(ChartSeriesId id, string name, double value, double min = 0, double max = 100, ChartColor? color = null) =>
        CreateGauge(id, name, new ChartGaugeValue(value, min, max), color);

    /// <summary>Creates a circular progress series with one authored measured value and scale.</summary>
    public static ChartSeries CreateCircle(ChartSeriesId id, string name, ChartGaugeValue value, ChartColor? color = null) =>
        Scalar(id, name, ChartSeriesKind.Circle, value, color);

    /// <summary>Creates a circular progress series with one authored measured value and scale.</summary>
    public static ChartSeries CreateCircle(ChartSeriesId id, string name, double value, double min = 0, double max = 100, ChartColor? color = null) =>
        CreateCircle(id, name, new ChartGaugeValue(value, min, max), color);

    /// <summary>Creates a bullet comparison with one authored measured value, target, scale and qualitative boundaries.</summary>
    public static ChartSeries CreateBullet(ChartSeriesId id, string name, ChartBulletValue value, ChartColor? color = null) => Bullet(id, name, value, color);

    /// <summary>Creates a bullet comparison with one authored measured value, target, scale and qualitative boundaries.</summary>
    public static ChartSeries CreateBullet(ChartSeriesId id, string name, double value, double target, double min = 0, double max = 100,
        IEnumerable<double>? rangeEnds = null, ChartColor? color = null) => CreateBullet(id, name, new ChartBulletValue(value, target, min, max, rangeEnds), color);

    internal static ChartSeries CreateGauge(string name, ChartGaugeValue value, ChartColor? color) => Scalar(null, name, ChartSeriesKind.Gauge, value, color);
    internal static ChartSeries CreateCircle(string name, ChartGaugeValue value, ChartColor? color) => Scalar(null, name, ChartSeriesKind.Circle, value, color);
    internal static ChartSeries CreateBullet(string name, ChartBulletValue value, ChartColor? color) => Bullet(null, name, value, color);

    private static ChartSeries Scalar(ChartSeriesId? id, string name, ChartSeriesKind kind, ChartGaugeValue value, ChartColor? color) {
        var series = id.HasValue ? new ChartSeries(id.Value, name, kind, Array.Empty<ChartPoint>()) : new ChartSeries(name, kind, Array.Empty<ChartPoint>());
        series.ReplaceGaugeValues(new[] { value }); series.Color = color;
        return series;
    }

    private static ChartSeries Bullet(ChartSeriesId? id, string name, ChartBulletValue value, ChartColor? color) {
        var series = id.HasValue ? new ChartSeries(id.Value, name, ChartSeriesKind.Bullet, Array.Empty<ChartPoint>()) : new ChartSeries(name, ChartSeriesKind.Bullet, Array.Empty<ChartPoint>());
        series.ReplaceBulletValues(new[] { value }); series.Color = color;
        return series;
    }
}
