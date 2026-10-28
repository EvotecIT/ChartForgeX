using System;
using System.Threading;

namespace ChartForgeX.Raster;

/// <summary>Finite unassociated-channel policy for one admitted matrix or component-transfer source primitive.</summary>
/// <remarks>Coefficient/table arrays are bounded parsed source metadata, not pixel buffers. They never escape this policy.</remarks>
internal sealed class RasterColorTransform {
    private readonly double[]? _matrix;
    private readonly RasterComponentTransfer[]? _functions;
    private readonly RasterColorTransform[]? _stages;
    internal bool LinearRgb { get; }
    internal bool OutputLinearRgb { get; }
    internal int StageCount => _stages?.Length ?? 1;
    internal RasterColorTransform(double[] matrix, bool linearRgb) { _matrix = matrix; LinearRgb = OutputLinearRgb = linearRgb; }
    internal RasterColorTransform(RasterComponentTransfer[] functions, bool linearRgb) { _functions = functions; LinearRgb = OutputLinearRgb = linearRgb; }

    /// <summary>Owns the parsed flat linear stages; every stage consumes only its immediately preceding output.</summary>
    internal RasterColorTransform(RasterColorTransform[] stages) {
        if (stages == null || stages.Length == 0) throw new ArgumentException("A color sequence needs an explicit first stage.", nameof(stages));
        foreach (var stage in stages) if (stage == null || stage._stages != null)
            throw new ArgumentException("Color sequences contain flat scalar stages only.", nameof(stages));
        _stages = stages; LinearRgb = stages[0].LinearRgb; OutputLinearRgb = stages[stages.Length - 1].OutputLinearRgb;
    }

    internal void Apply(double r, double g, double b, double a, out double red, out double green, out double blue, out double alpha,
        CancellationToken cancellation) {
        if (_stages == null) {
            cancellation.ThrowIfCancellationRequested();
            ApplyStage(r, g, b, a, out red, out green, out blue, out alpha); return;
        }
        var linearRgb = LinearRgb;
        foreach (var stage in _stages) {
            cancellation.ThrowIfCancellationRequested();
            if (a == 0) r = g = b = 0;
            if (linearRgb != stage.LinearRgb) {
                if (stage.LinearRgb) {
                    r = RasterColorTransfer.Linear(r); g = RasterColorTransfer.Linear(g); b = RasterColorTransfer.Linear(b);
                } else {
                    r = RasterColorTransfer.Encoded(r); g = RasterColorTransfer.Encoded(g); b = RasterColorTransfer.Encoded(b);
                }
            }
            stage.ApplyStage(r, g, b, a, out r, out g, out b, out a);
            linearRgb = stage.LinearRgb;
        }
        red = r; green = g; blue = b; alpha = a;
    }

    private void ApplyStage(double r, double g, double b, double a, out double red, out double green, out double blue, out double alpha) {
        if (_matrix is { } m) {
            red = Clamp(m[0] * r + m[1] * g + m[2] * b + m[3] * a + m[4]);
            green = Clamp(m[5] * r + m[6] * g + m[7] * b + m[8] * a + m[9]);
            blue = Clamp(m[10] * r + m[11] * g + m[12] * b + m[13] * a + m[14]);
            alpha = Clamp(m[15] * r + m[16] * g + m[17] * b + m[18] * a + m[19]);
        } else {
            var f = _functions!;
            red = f[0].Apply(r); green = f[1].Apply(g); blue = f[2].Apply(b); alpha = f[3].Apply(a);
        }
    }
    internal static double Clamp(double value) => Math.Max(0, Math.Min(1, value));
}

internal enum RasterTransferKind { Identity, Table, Discrete, Linear, Gamma }

/// <summary>One immutable source channel function; missing/empty tables are identity, and C=1 selects the final entry.</summary>
internal readonly struct RasterComponentTransfer {
    private readonly RasterTransferKind _kind;
    private readonly double[]? _table;
    private readonly double _first, _second, _third;
    internal RasterComponentTransfer(RasterTransferKind kind, double[]? table = null,
        double first = 1, double second = 0, double third = 0) {
        _kind = kind; _table = table; _first = first; _second = second; _third = third;
    }
    internal double Apply(double value) {
        double output;
        switch (_kind) {
            case RasterTransferKind.Table:
            case RasterTransferKind.Discrete:
                var table = _table!;
                if (table.Length == 0) return value;
                if (table.Length == 1 || value >= 1) output = table[table.Length - 1];
                else if (_kind == RasterTransferKind.Discrete) output = table[(int)(value * table.Length)];
                else {
                    var position = value * (table.Length - 1);
                    var index = (int)position; var amount = position - index;
                    // Convex interpolation avoids overflowing the difference of opposite finite endpoints.
                    output = (1 - amount) * table[index] + amount * table[index + 1];
                }
                break;
            case RasterTransferKind.Linear: output = _first * value + _second; break;
            case RasterTransferKind.Gamma: output = Gamma(value); break;
            default: return value;
        }
        return RasterColorTransform.Clamp(output);
    }

    /// <summary>Evaluates the finite clamped gamma law without relying on NaN or infinity-to-byte conversions.</summary>
    private double Gamma(double value) {
        // A zero amplitude is a constant function, including the continuous extension at a zero input.
        if (_first == 0) return _third;
        // For a negative exponent the right-hand limit of the clamped function is fixed by the amplitude sign.
        if (value == 0 && _second < 0) return _first > 0 ? 1 : 0;
        var power = Math.Pow(value, _second);
        var scaled = _first * power;
        if (double.IsPositiveInfinity(power)) {
            // A power alone may overflow even though a tiny finite amplitude makes its product representable.
            // Keep ordinary finite pow arithmetic unchanged; use the logarithmic product only for that overflow.
            var magnitude = Math.Exp(Math.Log(Math.Abs(_first)) + _second * Math.Log(value));
            scaled = _first < 0 ? -magnitude : magnitude;
        }
        return scaled + _third; // Finite offset cannot turn a signed overflow into NaN; final Clamp saturates it.
    }
}
