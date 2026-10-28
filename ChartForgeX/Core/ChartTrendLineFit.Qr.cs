using System;
using System.Threading;

namespace ChartForgeX.Core;

internal sealed partial class ChartTrendLineFit {
    /// <summary>Solves at most four centered/scaled basis columns using Householder QR.</summary>
    private static double[] SolveQr(double[,] matrix, double[] response, CancellationToken cancellationToken, out double[,] upper) {
        var rows = matrix.GetLength(0); var columns = matrix.GetLength(1);
        var threshold = Math.Sqrt(rows) * 1e-12;
        for (var pivot = 0; pivot < columns; pivot++) {
            cancellationToken.ThrowIfCancellationRequested();
            var norm = 0d;
            for (var row = pivot; row < rows; row++) norm = Hypotenuse(norm, matrix[row, pivot]);
            if (!Finite(norm) || norm <= threshold)
                throw new ArgumentException("The requested trend model has numerically rank-deficient predictors; use distinct, better-separated predictors or a lower-degree model.");
            var original = matrix[pivot, pivot]; var diagonal = original >= 0 ? -norm : norm;
            var first = original - diagonal; var tau = (diagonal - original) / diagonal;
            matrix[pivot, pivot] = diagonal;
            for (var row = pivot + 1; row < rows; row++) matrix[row, pivot] /= first;
            for (var column = pivot + 1; column < columns; column++) {
                var dot = matrix[pivot, column];
                for (var row = pivot + 1; row < rows; row++) dot += matrix[row, pivot] * matrix[row, column];
                dot *= tau; matrix[pivot, column] -= dot;
                for (var row = pivot + 1; row < rows; row++) matrix[row, column] -= matrix[row, pivot] * dot;
            }
            var projected = response[pivot];
            for (var row = pivot + 1; row < rows; row++) projected += matrix[row, pivot] * response[row];
            projected *= tau; response[pivot] -= projected;
            for (var row = pivot + 1; row < rows; row++) response[row] -= matrix[row, pivot] * projected;
        }
        upper = new double[columns, columns];
        for (var row = 0; row < columns; row++) for (var column = row; column < columns; column++) upper[row, column] = matrix[row, column];
        var coefficients = new double[columns];
        for (var row = columns - 1; row >= 0; row--) {
            var value = response[row];
            for (var column = row + 1; column < columns; column++) value -= upper[row, column] * coefficients[column];
            coefficients[row] = value / upper[row, row];
            if (!Finite(coefficients[row])) throw new ArgumentException("Trend coefficients exceed finite numerical range.");
        }
        return coefficients;
    }

    private static double Hypotenuse(double first, double second) {
        first = Math.Abs(first); second = Math.Abs(second);
        var maximum = Math.Max(first, second); var minimum = Math.Min(first, second);
        if (maximum == 0) return 0;
        var ratio = minimum / maximum;
        return maximum * Math.Sqrt(1 + ratio * ratio);
    }
}
