using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

/// <summary>Owns renderer-neutral angle/radius layout for polar line charts.</summary>
internal sealed class PolarChartGeometry {
    private readonly RadialValueScale _valueScale;

    private PolarChartGeometry(double centerX, double centerY, double radius, RadialValueScale valueScale, IReadOnlyList<double> angleTicks) {
        CenterX = centerX;
        CenterY = centerY;
        Radius = radius;
        _valueScale = valueScale;
        AngleTicks = angleTicks;
    }

    public double CenterX { get; }

    public double CenterY { get; }

    public double Radius { get; }

    public IReadOnlyList<double> AngleTicks { get; }

    public IReadOnlyList<double> RadiusTicks => _valueScale.Ticks;

    public double MinimumRadius => _valueScale.Minimum;

    public static PolarChartGeometry Create(Chart chart, ChartRect plot) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        var series = chart.Series.Where(item => item.Kind == ChartSeriesKind.Polar).ToArray();
        var valueScale = RadialValueScale.Create(chart.Options.YAxis, series, "Polar");
        var centerX = plot.Left + plot.Width / 2;
        var centerY = plot.Top + plot.Height / 2 + 6;
        var radius = Math.Max(32, Math.Min(plot.Width, plot.Height) / 2 - 42);
        var spokeCount = Math.Max(4, Math.Min(12, chart.Options.XAxis.TickCount));
        var angleTicks = new double[spokeCount];
        for (var i = 0; i < spokeCount; i++) angleTicks[i] = Math.PI * 2 * i / spokeCount;
        return new PolarChartGeometry(centerX, centerY, radius, valueScale, angleTicks);
    }

    public ChartPoint Map(ChartPoint point) {
        var radius = Radius * _valueScale.Normalize(point.Y);
        return new ChartPoint(CenterX + Math.Cos(point.X) * radius, CenterY - Math.Sin(point.X) * radius);
    }

    /// <summary>Maps a polar point's requested data-label placement into renderer-neutral coordinates.</summary>
    public ChartPoint DataLabelPoint(ChartPoint raw, ChartPoint mapped, ChartDataLabelPlacement placement) {
        if (placement == ChartDataLabelPlacement.Center || placement == ChartDataLabelPlacement.Inside) return mapped;
        if (placement == ChartDataLabelPlacement.Left) return new ChartPoint(mapped.X - 20, mapped.Y);
        if (placement == ChartDataLabelPlacement.Right || placement == ChartDataLabelPlacement.Outside) return new ChartPoint(mapped.X + 20, mapped.Y);
        if (placement == ChartDataLabelPlacement.Above) return new ChartPoint(mapped.X, mapped.Y - 20);
        if (placement == ChartDataLabelPlacement.Below) return new ChartPoint(mapped.X, mapped.Y + 20);

        var x = Math.Cos(raw.X);
        var y = -Math.Sin(raw.X);
        return raw.Y <= MinimumRadius ? new ChartPoint(mapped.X, mapped.Y - 16) : new ChartPoint(mapped.X + x * 16, mapped.Y + y * 16);
    }

    public ChartPoint OnOuterRing(double angle, double offset = 0) =>
        new(CenterX + Math.Cos(angle) * (Radius + offset), CenterY - Math.Sin(angle) * (Radius + offset));

    public double RingRadius(double value) => Radius * _valueScale.Normalize(value);

    public bool IsOuterRadius(double value) => _valueScale.IsMaximum(value);
}
