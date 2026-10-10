using System;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

public static partial class V2GalleryModels {
    private static Chart AxisPrecision() => Chart.Create().WithXAxis("Measurement").WithYAxis("Fitted drift")
        .AddTrendLine("Least-squares drift", Enumerable.Range(0, 4096)
            .Select(index => new ChartPoint(index % 10 + .5, index % 7 - 3)));

    private static Chart GaugePrecision() => Chart.Create().AddLinearGauge("Tolerance", 1.003, 1.001, 1.005)
        .ConfigureGauge(options => options.Target = 1.0025);
}
