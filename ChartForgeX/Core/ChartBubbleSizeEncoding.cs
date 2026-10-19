namespace ChartForgeX.Core;

/// <summary>Chooses how a bubble's source size controls its native marker geometry.</summary>
public enum ChartBubbleSizeEncoding {
    /// <summary>Preserves square-root interpolation of the radius range. Equal values have equal radii, without an area guarantee.</summary>
    SquareRootRadius,

    /// <summary>Interpolates reference-circle area linearly across the size domain and normalizes other marker shapes to that area.</summary>
    /// <remarks>Radius endpoints describe reference circles. Other shapes may extend farther to enclose equal fill area.
    /// With a zero size-domain minimum and zero minimum radius, fill area is proportional to the source value.
    /// Strokes, antialiasing and clipping do not contribute to the encoded fill area.</remarks>
    Area
}
