using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

/// <summary>One point estimate with independently authored horizontal and vertical uncertainty intervals.</summary>
public readonly struct ChartErrorBar {
    /// <summary>Gets the horizontal estimate in source coordinates.</summary>
    public double X { get; }
    /// <summary>Gets the vertical estimate in source coordinates.</summary>
    public double Y { get; }
    /// <summary>Gets the horizontal source interval, or null when horizontal uncertainty is not supplied.</summary>
    public ChartErrorBounds? XBounds { get; }
    /// <summary>Gets the vertical source interval, or null when vertical uncertainty is not supplied.</summary>
    public ChartErrorBounds? YBounds { get; }

    /// <summary>Creates an estimate with horizontal, vertical or simultaneous uncertainty.</summary>
    /// <param name="x">The finite horizontal estimate.</param>
    /// <param name="y">The finite vertical estimate.</param>
    /// <param name="xBounds">An interval containing X, or null to omit the horizontal whisker.</param>
    /// <param name="yBounds">An interval containing Y, or null to omit the vertical whisker.</param>
    /// <remarks>At least one interval is required. Equal lower and upper bounds are valid exact estimates.</remarks>
    public ChartErrorBar(double x, double y, ChartErrorBounds? xBounds = null, ChartErrorBounds? yBounds = null) {
        ChartPrimitiveGuards.Finite(x, nameof(x)); ChartPrimitiveGuards.Finite(y, nameof(y));
        if (!xBounds.HasValue && !yBounds.HasValue) throw new ArgumentException("An error bar requires at least one uncertainty interval.");
        ValidateContains(xBounds, x, nameof(xBounds)); ValidateContains(yBounds, y, nameof(yBounds));
        X = x; Y = y; XBounds = xBounds; YBounds = yBounds;
    }

    /// <summary>Creates an estimate with a UTC-converted date/time X coordinate and explicitly supplied source intervals.</summary>
    /// <param name="x">The date/time horizontal estimate, using the existing native UTC conversion policy.</param>
    /// <param name="y">The finite vertical estimate.</param>
    /// <param name="xBounds">A horizontal interval in the same OLE Automation source coordinates.</param>
    /// <param name="yBounds">A vertical interval containing Y.</param>
    public ChartErrorBar(DateTime x, double y, ChartErrorBounds? xBounds = null, ChartErrorBounds? yBounds = null)
        : this(ChartDateTime.ToOADate(x), y, xBounds, yBounds) { }

    private static void ValidateContains(ChartErrorBounds? bounds, double estimate, string parameter) {
        if (bounds.HasValue && (estimate < bounds.Value.Lower || estimate > bounds.Value.Upper))
            throw new ArgumentOutOfRangeException(parameter, "The uncertainty interval must contain its point estimate.");
    }
}
