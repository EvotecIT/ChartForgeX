using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

/// <summary>The shared numeric circle-key contract implemented by actual Bubble and geographic marker size owners.</summary>
internal interface IChartMarkerSizeScale {
    double MinimumValue { get; }
    double MaximumValue { get; }
    bool Reversed { get; }
    string Mapping { get; }
    ChartSizePresentation Presentation { get; }
    double Radius(double value, ChartRect plot);
}

/// <summary>Owns stable source-domain normalization and marker radius interpolation without changing family laws.</summary>
internal static class ChartMarkerSizeMapping {
    internal static double Ratio(double value, double minimum, double maximum, bool reversed, double constantRatio) {
        var ratio = minimum == maximum ? constantRatio : Math.Max(0, Math.Min(1, ChartMath.Normalize(value, minimum, maximum)));
        return reversed ? 1 - ratio : ratio;
    }

    internal static double BubbleRadius(double ratio, double minimumRadius, double maximumRadius, bool constant,
        ChartBubbleSizeEncoding encoding, ChartMarkerShape shape) {
        if (encoding == ChartBubbleSizeEncoding.Area) {
            if (maximumRadius == 0) return 0;
            var minimumRatio = minimumRadius / maximumRadius;
            return maximumRadius * Math.Sqrt(minimumRatio * minimumRatio * (1 - ratio) + ratio) * ChartMarkerGeometry.CircleAreaRadiusFactor(shape);
        }
        return ChartMath.InterpolateRange(minimumRadius, maximumRadius, constant ? .5 : Math.Sqrt(ratio));
    }
}
