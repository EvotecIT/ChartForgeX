using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>
    /// Adds a vertical bar series and a line series for a shared-axis combo chart.
    /// </summary>
    /// <param name="barName">The bar series name.</param>
    /// <param name="barPoints">The bar series data points.</param>
    /// <param name="lineName">The line series name.</param>
    /// <param name="linePoints">The line series data points.</param>
    /// <param name="barColor">An optional bar series color.</param>
    /// <param name="lineColor">An optional line series color.</param>
    /// <param name="smoothLine">True to render the line as a smoothed curve; otherwise false.</param>
    /// <param name="lineAxis">The y-axis used by the line series.</param>
    /// <returns>The current chart.</returns>
    public Chart AddBarLineCombo(
        string barName,
        IEnumerable<ChartPoint> barPoints,
        string lineName,
        IEnumerable<ChartPoint> linePoints,
        ChartColor? barColor = null,
        ChartColor? lineColor = null,
        bool smoothLine = false,
        ChartAxisSide lineAxis = ChartAxisSide.Primary) {
        AddBar(barName, barPoints, barColor);
        if (smoothLine) AddSmoothLine(lineName, linePoints, lineColor);
        else AddLine(lineName, linePoints, lineColor);
        Series[Series.Count - 1].ValueAxis = lineAxis;
        return this;
    }

    /// <summary>
    /// Adds a vertical column series and a line series for a combo chart.
    /// </summary>
    /// <param name="columnName">The column series name.</param>
    /// <param name="columnPoints">The column series data points.</param>
    /// <param name="lineName">The line series name.</param>
    /// <param name="linePoints">The line series data points.</param>
    /// <param name="columnColor">An optional column series color.</param>
    /// <param name="lineColor">An optional line series color.</param>
    /// <param name="smoothLine">True to render the line as a smoothed curve; otherwise false.</param>
    /// <param name="lineAxis">The y-axis used by the line series.</param>
    /// <returns>The current chart.</returns>
    public Chart AddColumnLineCombo(
        string columnName,
        IEnumerable<ChartPoint> columnPoints,
        string lineName,
        IEnumerable<ChartPoint> linePoints,
        ChartColor? columnColor = null,
        ChartColor? lineColor = null,
        bool smoothLine = false,
        ChartAxisSide lineAxis = ChartAxisSide.Primary) =>
        AddBarLineCombo(columnName, columnPoints, lineName, linePoints, columnColor, lineColor, smoothLine, lineAxis);

    /// <summary>
    /// Adds a vertical bar series and an area series for a combo chart.
    /// </summary>
    /// <param name="barName">The bar series name.</param>
    /// <param name="barPoints">The bar series data points.</param>
    /// <param name="areaName">The area series name.</param>
    /// <param name="areaPoints">The area series data points.</param>
    /// <param name="barColor">An optional bar series color.</param>
    /// <param name="areaColor">An optional area series color.</param>
    /// <param name="smoothArea">True to render the area boundary as a smoothed curve; otherwise false.</param>
    /// <param name="areaAxis">The y-axis used by the area series.</param>
    /// <returns>The current chart.</returns>
    public Chart AddBarAreaCombo(
        string barName,
        IEnumerable<ChartPoint> barPoints,
        string areaName,
        IEnumerable<ChartPoint> areaPoints,
        ChartColor? barColor = null,
        ChartColor? areaColor = null,
        bool smoothArea = true,
        ChartAxisSide areaAxis = ChartAxisSide.Primary) {
        AddBar(barName, barPoints, barColor);
        if (smoothArea) AddSmoothArea(areaName, areaPoints, areaColor);
        else AddArea(areaName, areaPoints, areaColor);
        Series[Series.Count - 1].ValueAxis = areaAxis;
        return this;
    }

    /// <summary>
    /// Adds a vertical column series and an area series for a combo chart.
    /// </summary>
    /// <param name="columnName">The column series name.</param>
    /// <param name="columnPoints">The column series data points.</param>
    /// <param name="areaName">The area series name.</param>
    /// <param name="areaPoints">The area series data points.</param>
    /// <param name="columnColor">An optional column series color.</param>
    /// <param name="areaColor">An optional area series color.</param>
    /// <param name="smoothArea">True to render the area boundary as a smoothed curve; otherwise false.</param>
    /// <param name="areaAxis">The y-axis used by the area series.</param>
    /// <returns>The current chart.</returns>
    public Chart AddColumnAreaCombo(
        string columnName,
        IEnumerable<ChartPoint> columnPoints,
        string areaName,
        IEnumerable<ChartPoint> areaPoints,
        ChartColor? columnColor = null,
        ChartColor? areaColor = null,
        bool smoothArea = true,
        ChartAxisSide areaAxis = ChartAxisSide.Primary) =>
        AddBarAreaCombo(columnName, columnPoints, areaName, areaPoints, columnColor, areaColor, smoothArea, areaAxis);

    /// <summary>
    /// Adds a scatter series and a line series for an observed-versus-trend combo chart.
    /// </summary>
    /// <param name="scatterName">The scatter series name.</param>
    /// <param name="scatterPoints">The scatter series data points.</param>
    /// <param name="lineName">The line series name.</param>
    /// <param name="linePoints">The line series data points.</param>
    /// <param name="scatterColor">An optional scatter series color.</param>
    /// <param name="lineColor">An optional line series color.</param>
    /// <param name="smoothLine">True to render the line as a smoothed curve; otherwise false.</param>
    /// <param name="lineAxis">The y-axis used by the line series.</param>
    /// <returns>The current chart.</returns>
    public Chart AddScatterLineCombo(
        string scatterName,
        IEnumerable<ChartPoint> scatterPoints,
        string lineName,
        IEnumerable<ChartPoint> linePoints,
        ChartColor? scatterColor = null,
        ChartColor? lineColor = null,
        bool smoothLine = false,
        ChartAxisSide lineAxis = ChartAxisSide.Primary) {
        AddScatter(scatterName, scatterPoints, scatterColor);
        if (smoothLine) AddSmoothLine(lineName, linePoints, lineColor);
        else AddLine(lineName, linePoints, lineColor);
        Series[Series.Count - 1].ValueAxis = lineAxis;
        return this;
    }
}
