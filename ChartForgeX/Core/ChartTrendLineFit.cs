using System;
using System.Collections.Generic;
using System.Threading;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

/// <summary>One immutable retained-source fit shared by native range, curve and interval preparation.</summary>
internal sealed partial class ChartTrendLineFit {
    private readonly ChartTrendLineOptions _options;
    private readonly double _minimumX, _maximumX, _predictorMinimum, _predictorSpan;
    private readonly double _responseOrigin, _responseScale;
    private readonly double[] _coefficients;
    private readonly double[,] _upper;
    private readonly double _residualVariance;
    private readonly double _critical;
    internal IReadOnlyList<ChartTrendLineSample> Samples { get; private set; } = Array.Empty<ChartTrendLineSample>();
    internal bool LogResponse => _options.Model is ChartTrendLineModel.Exponential or ChartTrendLineModel.Power;

    private ChartTrendLineFit(ChartTrendLineOptions options, double minX, double maxX, double predictorMinimum,
        double predictorSpan, double responseOrigin, double responseScale, double[] coefficients,
        double[,] upper, double residualVariance, double critical) {
        _options = options; _minimumX = minX; _maximumX = maxX;
        _predictorMinimum = predictorMinimum; _predictorSpan = predictorSpan;
        _responseOrigin = responseOrigin; _responseScale = responseScale;
        _coefficients = coefficients; _upper = upper; _residualVariance = residualVariance; _critical = critical;
    }

    internal static ChartTrendLineFit Create(IReadOnlyList<ChartPoint> source, ChartTrendLineOptions requested,
        CancellationToken cancellationToken) {
        var options = requested.Capture();
        if (source.Count < 2) throw new ArgumentException("Trend lines require at least two points.", "observations");
        if (options.Model == ChartTrendLineModel.Linear && options.Interval == ChartTrendLineIntervalKind.None)
            return CreateLegacyFit(source, options, cancellationToken);
        var minimumX = double.PositiveInfinity; var maximumX = double.NegativeInfinity;
        var minimum = double.PositiveInfinity; var maximum = double.NegativeInfinity;
        var predictors = new double[source.Count]; var responses = new double[source.Count];
        for (var index = 0; index < source.Count; index++) {
            cancellationToken.ThrowIfCancellationRequested();
            var point = source[index];
            ChartGuards.Finite(point.X, nameof(source)); ChartGuards.Finite(point.Y, nameof(source));
            minimumX = Math.Min(minimumX, point.X); maximumX = Math.Max(maximumX, point.X);
            predictors[index] = TransformPredictor(point.X, options.Model);
            responses[index] = TransformResponse(point.Y, options.Model);
            minimum = Math.Min(minimum, predictors[index]); maximum = Math.Max(maximum, predictors[index]);
        }
        var span = maximum - minimum;
        if (span <= 0 || !Finite(span) || !Finite(maximumX - minimumX))
            throw new ArgumentException("Trend lines require a finite, representable span of distinct transformed predictors.", nameof(source));
        var degree = options.Model == ChartTrendLineModel.Polynomial ? options.PolynomialDegree : 1;
        var columns = degree + 1;
        if (source.Count < columns) throw new ArgumentException("The requested trend model needs at least degree plus one observations.", nameof(source));
        if (options.Interval != ChartTrendLineIntervalKind.None && source.Count <= columns)
            throw new ArgumentException("Trend intervals require positive residual degrees of freedom.", nameof(source));
        var origin = responses[0]; var responseScale = 0d;
        foreach (var response in responses) responseScale = Math.Max(responseScale, Math.Abs(response - origin));
        if (!Finite(responseScale)) {
            origin = 0; responseScale = 0;
            foreach (var response in responses) responseScale = Math.Max(responseScale, Math.Abs(response));
        }
        if (responseScale == 0) responseScale = 1;
        var basis = new double[source.Count, columns]; var normalizedResponse = new double[source.Count];
        for (var row = 0; row < source.Count; row++) {
            cancellationToken.ThrowIfCancellationRequested();
            var z = 2 * ((predictors[row] - minimum) / span - .5); var power = 1d;
            for (var column = 0; column < columns; column++) { basis[row, column] = power; power *= z; }
            normalizedResponse[row] = (responses[row] - origin) / responseScale;
        }
        var coefficients = SolveQr(basis, normalizedResponse, cancellationToken, out var upper);
        var residualSum = 0d; var correction = 0d;
        for (var row = 0; row < source.Count; row++) {
            cancellationToken.ThrowIfCancellationRequested();
            var z = 2 * ((predictors[row] - minimum) / span - .5);
            var residual = (responses[row] - origin) / responseScale - EvaluatePolynomial(coefficients, z);
            CompensatedAdd(ref residualSum, ref correction, residual * residual);
        }
        var variance = source.Count > columns ? (residualSum + correction) / (source.Count - columns) : 0;
        var critical = options.Interval == ChartTrendLineIntervalKind.None ? 0 : StudentCritical(options.ConfidenceLevel, source.Count - columns);
        var fit = new ChartTrendLineFit(options, minimumX, maximumX, minimum, span, origin, responseScale,
            coefficients, upper, variance, critical);
        fit.Samples = fit.CaptureSamples(cancellationToken);
        return fit;
    }

    private ChartTrendLineSample Evaluate(double x) {
        var z = 2 * ((TransformPredictor(x, _options.Model) - _predictorMinimum) / _predictorSpan - .5);
        var normalized = EvaluatePolynomial(_coefficients, z);
        var value = ResponseValue(normalized);
        if (_options.Interval == ChartTrendLineIntervalKind.None) return new ChartTrendLineSample(x, value);
        var distance = _critical * Math.Sqrt(_residualVariance * (Leverage(z) + (_options.Interval == ChartTrendLineIntervalKind.Prediction ? 1 : 0)));
        if (!Finite(distance)) throw new ArgumentException("The requested trend interval exceeds finite numerical range.");
        return new ChartTrendLineSample(x, value, ResponseValue(normalized - distance), ResponseValue(normalized + distance));
    }

    private double ResponseValue(double normalized) {
        var response = _responseOrigin + _responseScale * normalized;
        var value = LogResponse ? Math.Exp(response) : response;
        if (!Finite(value) || LogResponse && value <= 0)
            throw new ArgumentException("A fitted trend sample or interval is outside the finite representable response range.");
        return value;
    }

    private static double TransformPredictor(double value, ChartTrendLineModel model) {
        if (model is not ChartTrendLineModel.Logarithmic and not ChartTrendLineModel.Power) return value;
        if (value <= 0) throw new ArgumentException("Logarithmic and Power trend models require positive source X.");
        return Math.Log(value);
    }

    private static double TransformResponse(double value, ChartTrendLineModel model) {
        if (model is not ChartTrendLineModel.Exponential and not ChartTrendLineModel.Power) return value;
        if (value <= 0) throw new ArgumentException("Exponential and Power trend models require positive source Y.");
        return Math.Log(value);
    }

    private static double EvaluatePolynomial(double[] coefficients, double z) {
        var value = coefficients[coefficients.Length - 1];
        for (var index = coefficients.Length - 2; index >= 0; index--) value = value * z + coefficients[index];
        return value;
    }

    private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

    private static void CompensatedAdd(ref double sum, ref double correction, double value) {
        var next = sum + value;
        correction += Math.Abs(sum) >= Math.Abs(value) ? (sum - next) + value : (value - next) + sum;
        sum = next;
    }
}
