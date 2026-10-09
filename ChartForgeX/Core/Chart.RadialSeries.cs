using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>Adds numeric angular bars arranged in radial category bands. X identifies a category; Y is the signed value.</summary>
    /// <param name="name">The series name.</param>
    /// <param name="points">Finite category/value observations, in source units.</param>
    /// <param name="color">An optional series color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddRadialBar(string name, IEnumerable<ChartPoint> points, ChartColor? color = null) =>
        Add(name, ChartSeriesKind.RadialBar, points, color);

    /// <summary>Adds numeric radial columns arranged in angular category bands. X identifies a category; Y is the signed value.</summary>
    /// <param name="name">The series name.</param>
    /// <param name="points">Finite category/value observations, in source units.</param>
    /// <param name="color">An optional series color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddRadialColumn(string name, IEnumerable<ChartPoint> points, ChartColor? color = null) =>
        Add(name, ChartSeriesKind.RadialColumn, points, color);

    /// <summary>Sets shared geometry for numeric radial bars and columns. Numeric bounds and formatting belong to the value axis.</summary>
    /// <param name="geometry">Immutable start/end angles, inner radius and spacing.</param>
    /// <returns>The current chart.</returns>
    public Chart WithRadialGeometry(ChartRadialGeometryOptions geometry) {
        Options.RadialGeometry = geometry ?? throw new ArgumentNullException(nameof(geometry));
        return this;
    }
}
