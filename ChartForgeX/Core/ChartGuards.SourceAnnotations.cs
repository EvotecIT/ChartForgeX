using System;
using System.Linq;
using ChartForgeX.Rendering;

namespace ChartForgeX.Core;

internal static partial class ChartGuards {
    private static void ValidateSourceAnnotations(Chart chart) {
        foreach (var annotation in chart.Annotations.Where(item => item.HasSourceGeometry)) {
            if (!chart.Series.All(series => VisualCartesianCompiler.Supports(series.Kind)) || ChartCartesianOrientation.IsHorizontal(chart))
                throw new NotSupportedException("Source-coordinate annotations require a vertical Cartesian coordinate system.");
            if (annotation.SourceYAxis == ChartAxisSide.Secondary && !chart.Series.Any(series => series.YAxis == ChartAxisSide.Secondary))
                throw new InvalidOperationException("A source annotation on the secondary Y axis requires a series owning that axis.");
            var yAxis = annotation.SourceYAxis == ChartAxisSide.Secondary ? chart.Options.SecondaryYAxis : chart.Options.YAxis;
            var start = annotation.SourceStart!.Value;
            var end = annotation.SourceEnd ?? start;
            if (chart.Options.XAxis.Scale == ChartScaleKind.Logarithmic && (start.X <= 0 || end.X <= 0))
                throw new InvalidOperationException("Source annotations on a logarithmic X axis require positive source X coordinates.");
            if (yAxis.Scale == ChartScaleKind.Logarithmic
                && (start.Y <= 0 || end.Y <= 0 || start.Y + annotation.ChannelOffset <= 0 || end.Y + annotation.ChannelOffset <= 0))
                throw new InvalidOperationException("Source annotations on a logarithmic Y axis require positive source Y coordinates on both boundaries.");
        }
    }
}
