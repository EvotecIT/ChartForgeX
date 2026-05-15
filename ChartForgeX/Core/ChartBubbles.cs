using System;
using System.Collections.Generic;
using System.Linq;

namespace ChartForgeX.Core;

/// <summary>
/// Creates common <see cref="ChartBubble"/> sequences for bubble charts.
/// </summary>
public static class ChartBubbles {
    /// <summary>
    /// Creates bubble values from matching x, y, and size sequences.
    /// </summary>
    /// <param name="x">The x values.</param>
    /// <param name="y">The y values.</param>
    /// <param name="size">The bubble size values.</param>
    /// <returns>Bubble values using the supplied x, y, and size values.</returns>
    public static ChartBubble[] FromXYSize(IEnumerable<double> x, IEnumerable<double> y, IEnumerable<double> size) {
        if (x is null) throw new ArgumentNullException(nameof(x));
        if (y is null) throw new ArgumentNullException(nameof(y));
        if (size is null) throw new ArgumentNullException(nameof(size));

        var xValues = x.ToArray();
        var yValues = y.ToArray();
        var sizeValues = size.ToArray();
        if (xValues.Length == 0) throw new ArgumentException("Bubble values cannot be empty.", nameof(x));
        if (xValues.Length != yValues.Length || xValues.Length != sizeValues.Length) throw new ArgumentException("Bubble X, Y, and Size values must contain the same number of items.", nameof(size));

        var bubbles = new ChartBubble[xValues.Length];
        for (var i = 0; i < xValues.Length; i++) {
            if (sizeValues[i] <= 0) throw new ArgumentOutOfRangeException(nameof(size), sizeValues[i], "Bubble size values must be greater than zero.");
            bubbles[i] = new ChartBubble(xValues[i], yValues[i], sizeValues[i]);
        }

        return bubbles;
    }
}
