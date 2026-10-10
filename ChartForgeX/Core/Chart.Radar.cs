using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>Adds a closed radar line with optional markers and no area fill.</summary>
    /// <param name="name">The series name.</param>
    /// <param name="points">The category/value observations.</param>
    /// <param name="color">The optional series color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddRadarLine(string name, IEnumerable<ChartPoint> points, ChartColor? color = null) {
        AddRadar(name, points, color);
        Series[Series.Count - 1].Radar.Form = ChartLineAreaForm.Line;
        return this;
    }

    /// <summary>Adds a filled radar area with an outline and optional markers.</summary>
    /// <param name="name">The series name.</param>
    /// <param name="points">The category/value observations.</param>
    /// <param name="color">The optional series color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddRadarArea(string name, IEnumerable<ChartPoint> points, ChartColor? color = null) => AddRadar(name, points, color);
}
