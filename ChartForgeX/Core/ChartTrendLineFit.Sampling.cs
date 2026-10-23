using System;
using System.Collections.Generic;
using System.Threading;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

internal sealed partial class ChartTrendLineFit {
    private static ChartTrendLineFit CreateLegacyFit(IReadOnlyList<ChartPoint> source, ChartTrendLineOptions options,
        CancellationToken cancellationToken) {
        var minimum = double.PositiveInfinity; var maximum = double.NegativeInfinity;
        foreach (var point in source) {
            cancellationToken.ThrowIfCancellationRequested();
            ChartGuards.Finite(point.X, nameof(source)); ChartGuards.Finite(point.Y, nameof(source));
            minimum = Math.Min(minimum, point.X); maximum = Math.Max(maximum, point.X);
        }
        var span = maximum - minimum;
        if (minimum == maximum) throw new ArgumentException("Trend lines require at least two distinct x values.", "observations");
        ChartGuards.Finite(span, "observations");
        var endpoints = LegacyLinearSamples(source, minimum, maximum, cancellationToken);
        return new ChartTrendLineFit(options, minimum, maximum, minimum, span, 0, 1,
            Array.Empty<double>(), new double[0, 0], 0, 0) { Samples = Array.AsReadOnly(endpoints) };
    }

    private IReadOnlyList<ChartTrendLineSample> CaptureSamples(CancellationToken cancellationToken) {
        var coordinates = new SortedSet<double> { _minimumX, _maximumX };
        for (var index = 1; index < _options.SampleCount - 1; index++)
            coordinates.Add(_minimumX + (_maximumX - _minimumX) * (index / (double)(_options.SampleCount - 1)));
        if (_options.Model == ChartTrendLineModel.Polynomial) {
            if (_coefficients.Length == 3) AddStationary(-_coefficients[1] / (2 * _coefficients[2]));
            else {
                var a = 3 * _coefficients[3]; var b = 2 * _coefficients[2]; var c = _coefficients[1];
                if (a == 0) AddStationary(-c / b);
                else {
                    var discriminant = b * b - 4 * a * c;
                    if (discriminant >= 0) {
                        var q = -.5 * (b + (b < 0 ? -1 : 1) * Math.Sqrt(discriminant));
                        AddStationary(q / a); AddStationary(q == 0 ? 0 : c / q);
                    }
                }
            }
        }
        var samples = new List<ChartTrendLineSample>(coordinates.Count);
        foreach (var x in coordinates) { cancellationToken.ThrowIfCancellationRequested(); samples.Add(Evaluate(x)); }
        return samples.AsReadOnly();

        void AddStationary(double z) {
            if (Finite(z) && z > -1 && z < 1) coordinates.Add(_minimumX + (_maximumX - _minimumX) * ((z + 1) / 2));
        }
    }

    private double Leverage(double z) {
        // V=QR, so h=v'(V'V)^-1v = ||R^-T v||^2. No inverse or normal equations are formed.
        var solved = new double[_coefficients.Length]; var power = 1d; var sum = 0d;
        for (var row = 0; row < solved.Length; row++) {
            var value = power;
            for (var column = 0; column < row; column++) value -= _upper[column, row] * solved[column];
            solved[row] = value / _upper[row, row]; power *= z; sum += solved[row] * solved[row];
        }
        return sum;
    }

    /// <summary>Preserves the existing centered/scaled linear least-squares endpoint calculation.</summary>
    private static ChartTrendLineSample[] LegacyLinearSamples(IReadOnlyList<ChartPoint> source, double minX, double maxX,
        CancellationToken cancellationToken) {
        var span = maxX - minX;
        var originY = source[0].Y; var meanX = 0d; var meanY = 0d;
        foreach (var point in source) {
            cancellationToken.ThrowIfCancellationRequested();
            meanX += (point.X - minX) / span / source.Count;
            meanY += (point.Y - originY) / source.Count;
        }
        var covariance = 0d; var variance = 0d;
        foreach (var point in source) {
            cancellationToken.ThrowIfCancellationRequested();
            var x = (point.X - minX) / span - meanX;
            covariance += x * (point.Y - originY - meanY); variance += x * x;
        }
        var slope = covariance / variance;
        var first = originY + (meanY - slope * meanX); var last = originY + (meanY + slope * (1 - meanX));
        // The legacy ChartPoint boundary reports an invalid fitted endpoint as Y; validate before installation
        // so the same boundary also leaves an existing retained-source series unchanged.
        ChartGuards.Finite(first, "y"); ChartGuards.Finite(last, "y");
        return new[] { new ChartTrendLineSample(minX, first), new ChartTrendLineSample(maxX, last) };
    }
}
