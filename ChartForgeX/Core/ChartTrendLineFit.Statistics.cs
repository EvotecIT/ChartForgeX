using System;

namespace ChartForgeX.Core;

internal sealed partial class ChartTrendLineFit {
    /// <summary>Inverts the Student t upper tail; small-sample intervals never substitute a normal critical value.</summary>
    private static double StudentCritical(double confidence, int degreesOfFreedom) {
        if (degreesOfFreedom <= 0) throw new ArgumentOutOfRangeException(nameof(degreesOfFreedom));
        if (degreesOfFreedom == 1) return Math.Tan(Math.PI * confidence / 2);
        if (degreesOfFreedom == 2) return Math.Sqrt(2) * confidence / Math.Sqrt((1 - confidence) * (1 + confidence));
        var target = (1 - confidence) / 2;
        var lower = 0d; var upper = 1d;
        // At the supported confidence levels the bracket closes quickly, including the smallest df.
        for (var iteration = 0; StudentTail(upper, degreesOfFreedom) > target; iteration++) {
            if (iteration == 64) throw new ArgumentException("Unable to bracket the requested Student t interval.");
            upper *= 2;
        }
        for (var iteration = 0; iteration < 160; iteration++) {
            var middle = lower + (upper - lower) / 2;
            if (StudentTail(middle, degreesOfFreedom) > target) lower = middle;
            else upper = middle;
            if (upper - lower <= 2e-13 * upper) return lower + (upper - lower) / 2;
        }
        throw new ArgumentException("The requested Student t interval did not converge.");
    }

    private static double StudentTail(double value, int degreesOfFreedom) {
        var a = degreesOfFreedom / 2d; const double b = .5;
        var x = degreesOfFreedom / (degreesOfFreedom + value * value);
        if (x <= 0) return 0;
        if (x >= 1) return .5;
        var beta = Math.Exp(a * Math.Log(x) + b * Math.Log(1 - x) - LogBetaHalf(a));
        var tail = x < (a + 1) / (a + b + 2)
            ? beta * BetaFraction(a, b, x) / a
            : 1 - beta * BetaFraction(b, a, 1 - x) / b;
        if (!Finite(tail)) throw new ArgumentException("The requested Student t tail exceeds numerical range.");
        return .5 * Math.Max(0, Math.Min(1, tail));
    }

    /// <summary>Lentz's continued fraction, with a fixed convergence limit and explicit failure.</summary>
    private static double BetaFraction(double a, double b, double x) {
        const double minimum = 1e-300; const double tolerance = 3e-14;
        var qab = a + b; var qap = a + 1; var qam = a - 1;
        var c = 1d; var d = 1 - qab * x / qap;
        if (Math.Abs(d) < minimum) d = minimum;
        d = 1 / d; var fraction = d;
        for (var iteration = 1; iteration <= 512; iteration++) {
            var twice = 2d * iteration;
            var coefficient = iteration * (b - iteration) * x / ((qam + twice) * (a + twice));
            Step(coefficient);
            coefficient = -(a + iteration) * (qab + iteration) * x / ((a + twice) * (qap + twice));
            var delta = Step(coefficient);
            if (Math.Abs(delta - 1) <= tolerance) return fraction;
        }
        throw new ArgumentException("The requested Student t beta tail did not converge.");

        double Step(double coefficient) {
            d = 1 + coefficient * d; if (Math.Abs(d) < minimum) d = minimum;
            c = 1 + coefficient / c; if (Math.Abs(c) < minimum) c = minimum;
            d = 1 / d; var delta = d * c; fraction *= delta; return delta;
        }
    }

    private static double LogBetaHalf(double value) {
        // Avoid subtracting two nearly equal large log-gamma values for large source counts.
        if (value > 1e6) {
            var inverse = 1 / value;
            return .5 * Math.Log(Math.PI) - .5 * Math.Log(value) + inverse / 8 - inverse * inverse * inverse / 192;
        }
        return LogGamma(value) + .5 * Math.Log(Math.PI) - LogGamma(value + .5);
    }

    private static double LogGamma(double value) {
        // Lanczos coefficients, g=7, restricted here to the positive Student t beta parameters.
        var coefficients = new[] { 676.5203681218851, -1259.1392167224028, 771.32342877765313,
            -176.61502916214059, 12.507343278686905, -.13857109526572012, 9.9843695780195716e-6, 1.5056327351493116e-7 };
        var z = value - 1; var sum = .99999999999980993;
        for (var index = 0; index < coefficients.Length; index++) sum += coefficients[index] / (z + index + 1);
        var t = z + coefficients.Length - .5;
        return .91893853320467274178 + (z + .5) * Math.Log(t) - t + Math.Log(sum);
    }
}
