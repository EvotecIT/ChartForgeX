using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

/// <summary>
/// Assigns one stable, transitive coordinate key to numerically equivalent vertical bar points.
/// The map is built once per chart operation so stacking, range calculation, and total labels
/// share the same identity without repeatedly scanning histogram layouts.
/// </summary>
internal sealed class ChartBarCoordinateMap {
    private readonly Dictionary<double, double> _canonicalCoordinates;

    private ChartBarCoordinateMap(Dictionary<double, double> canonicalCoordinates) {
        _canonicalCoordinates = canonicalCoordinates;
    }

    internal static ChartBarCoordinateMap Create(Chart chart) {
        var coordinates = chart.Series
            .Where(series => series.Kind == ChartSeriesKind.Bar)
            .SelectMany(series => series.Points.Select(point => point.X))
            .Distinct()
            .OrderBy(value => value)
            .ToArray();
        var canonicalCoordinates = new Dictionary<double, double>(coordinates.Length);
        if (coordinates.Length == 0) return new ChartBarCoordinateMap(canonicalCoordinates);

        var canonical = coordinates[0];
        canonicalCoordinates[canonical] = canonical;
        for (var index = 1; index < coordinates.Length; index++) {
            if (!ChartMath.SameCoordinate(coordinates[index - 1], coordinates[index])) canonical = coordinates[index];
            canonicalCoordinates[coordinates[index]] = canonical;
        }

        return new ChartBarCoordinateMap(canonicalCoordinates);
    }

    internal double Resolve(double coordinate) =>
        _canonicalCoordinates.TryGetValue(coordinate, out var canonical) ? canonical : coordinate;
}
