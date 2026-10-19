using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>
    /// Adds a polar area series with equal-angle radial segments whose areas are proportional to non-negative values.
    /// </summary>
    /// <remarks>Positive values use square-root radius scaling; zero-valued categories retain visible placeholder slots.</remarks>
    /// <param name="name">The series name.</param>
    /// <param name="points">The segment values. The x values are used for optional segment labels.</param>
    /// <returns>The current chart.</returns>
    public Chart AddPolarArea(string name, IEnumerable<ChartPoint> points) => Add(name, ChartSeriesKind.PolarArea, points, null);
}
