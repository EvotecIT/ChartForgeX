using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

/// <summary>
/// Projects the United States as one frame: the lower 48 states at full size with Alaska and Hawaii moved into insets.
/// The Albers layout reproduces the composite used by the built-in us-states geometry (scale 1300, origin 487.5/305);
/// the other variants keep that layout and only change the projection inside each part.
/// </summary>
internal static class ChartUnitedStatesProjection {
    private const double Scale = 1300;
    private const double OriginX = 487.5;
    private const double OriginY = 305;
    private const double Radians = Math.PI / 180;

    /// <summary>Gets the fixed frame shared by every United States composite projection.</summary>
    internal static readonly ChartRect Frame = new(OriginX - .455 * Scale, OriginY - .238 * Scale, .91 * Scale, .476 * Scale);

    private static readonly Part[] Albers = Parts(RawKind.Albers);
    private static readonly Part[] Lambert = Parts(RawKind.Lambert);
    private static readonly Part[] Mercator = Parts(RawKind.Mercator);
    private static readonly Part[] Miller = Parts(RawKind.Miller);

    internal static bool IsComposite(ChartMapGeoJsonProjection projection) => projection is ChartMapGeoJsonProjection.UnitedStatesAlbers
        or ChartMapGeoJsonProjection.UnitedStatesLambert or ChartMapGeoJsonProjection.UnitedStatesMercator or ChartMapGeoJsonProjection.UnitedStatesMiller;

    internal static ChartPoint Project(double longitude, double latitude, ChartMapGeoJsonProjection projection) {
        var parts = projection switch {
            ChartMapGeoJsonProjection.UnitedStatesAlbers => Albers,
            ChartMapGeoJsonProjection.UnitedStatesLambert => Lambert,
            ChartMapGeoJsonProjection.UnitedStatesMercator => Mercator,
            ChartMapGeoJsonProjection.UnitedStatesMiller => Miller,
            _ => throw new ArgumentOutOfRangeException(nameof(projection))
        };
        return parts[PartIndex(longitude, latitude)].Project(longitude, latitude);
    }

    // Every vertex of a state polygon falls in the same part, so polygons never split across insets.
    private static int PartIndex(double longitude, double latitude) {
        if (latitude > 50 && (longitude < -125 || longitude > 150)) return 1;
        if (latitude < 30 && longitude < -150) return 2;
        return 0;
    }

    private static Part[] Parts(RawKind kind) {
        var lower48 = new Part(kind, 96, -.6, 38.7, kind == RawKind.Lambert ? 33 : 29.5, kind == RawKind.Lambert ? 45 : 45.5, Scale, OriginX, OriginY);
        var alaska = new Part(kind, 154, -2, 58.5, 55, 65, Scale * .35, OriginX - .307 * Scale, OriginY + .201 * Scale);
        var hawaii = new Part(kind, 157, -3, 19.9, 8, 18, Scale, OriginX - .205 * Scale, OriginY + .212 * Scale);
        return new[] { lower48, alaska, hawaii };
    }

    private enum RawKind {
        Albers,
        Lambert,
        Mercator,
        Miller
    }

    private sealed class Part {
        private readonly RawKind _kind;
        private readonly double _rotate;
        private readonly double _scale;
        private readonly double _translateX;
        private readonly double _translateY;
        private readonly double _n;
        private readonly double _c;
        private readonly double _r0;
        private readonly double _f;
        private readonly ChartPoint _center;

        internal Part(RawKind kind, double rotate, double centerLongitude, double centerLatitude, double parallel0, double parallel1,
            double albersScale, double translateX, double translateY) {
            _kind = kind;
            _rotate = rotate;
            _translateX = translateX;
            _translateY = translateY;
            var y0 = parallel0 * Radians;
            var y1 = parallel1 * Radians;
            if (kind == RawKind.Lambert) {
                _n = Math.Log(Math.Cos(y0) / Math.Cos(y1)) / Math.Log(Tangent(y1) / Tangent(y0));
                _f = Math.Cos(y0) * Math.Pow(Tangent(y0), _n) / _n;
            } else {
                var sy0 = Math.Sin(y0);
                _n = (sy0 + Math.Sin(y1)) / 2;
                _c = 1 + sy0 * (2 * _n - sy0);
                _r0 = Math.Sqrt(_c) / _n;
            }

            _center = Raw(centerLongitude * Radians, centerLatitude * Radians);
            _scale = kind == RawKind.Albers ? albersScale : albersScale * MatchingScale(centerLongitude, centerLatitude, parallel0, parallel1);
        }

        internal ChartPoint Project(double longitude, double latitude) {
            var lambda = longitude + _rotate;
            if (lambda > 180) lambda -= 360;
            else if (lambda < -180) lambda += 360;
            var point = Raw(lambda * Radians, latitude * Radians);
            return new ChartPoint(_translateX + _scale * (point.X - _center.X), _translateY - _scale * (point.Y - _center.Y));
        }

        // Non-Albers parts use the scale that gives the same east-west width as the Albers part at its center latitude.
        private double MatchingScale(double centerLongitude, double centerLatitude, double parallel0, double parallel1) {
            var albers = new Part(RawKind.Albers, _rotate, centerLongitude, centerLatitude, parallel0, parallel1, 1, 0, 0);
            var latitude = centerLatitude * Radians;
            var west = (centerLongitude - 10) * Radians;
            var east = (centerLongitude + 10) * Radians;
            return Distance(albers.Raw(west, latitude), albers.Raw(east, latitude)) / Distance(Raw(west, latitude), Raw(east, latitude));
        }

        private ChartPoint Raw(double lambda, double phi) {
            switch (_kind) {
                case RawKind.Albers: {
                    var r = Math.Sqrt(_c - 2 * _n * Math.Sin(phi)) / _n;
                    return new ChartPoint(r * Math.Sin(lambda * _n), _r0 - r * Math.Cos(lambda * _n));
                }
                case RawKind.Lambert: {
                    var clamped = Math.Max(-Math.PI / 2 + 1e-6, phi);
                    var r = _f / Math.Pow(Tangent(clamped), _n);
                    return new ChartPoint(r * Math.Sin(_n * lambda), _f - r * Math.Cos(_n * lambda));
                }
                case RawKind.Mercator: {
                    var clamped = Math.Max(-85.05112878 * Radians, Math.Min(85.05112878 * Radians, phi));
                    return new ChartPoint(lambda, Math.Log(Math.Tan(Math.PI / 4 + clamped / 2)));
                }
                default:
                    return new ChartPoint(lambda, 1.25 * Math.Log(Math.Tan(Math.PI / 4 + .4 * phi)));
            }
        }

        private static double Tangent(double phi) => Math.Tan((Math.PI / 2 + phi) / 2);

        private static double Distance(ChartPoint a, ChartPoint b) {
            var dx = b.X - a.X;
            var dy = b.Y - a.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }
    }
}
