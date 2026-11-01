using System;

namespace ChartForgeX.Rendering;

/// <summary>The corners of a scene rectangle that take its radius.</summary>
[Flags]
internal enum VisualCorners {
    None = 0,
    TopLeft = 1,
    TopRight = 2,
    BottomRight = 4,
    BottomLeft = 8,
    Top = TopLeft | TopRight,
    Bottom = BottomLeft | BottomRight,
    Left = TopLeft | BottomLeft,
    Right = TopRight | BottomRight,
    All = Top | Bottom
}
