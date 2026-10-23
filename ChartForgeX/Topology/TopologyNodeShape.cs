namespace ChartForgeX.Topology;

/// <summary>Product-neutral diagram surfaces shared by SVG and PNG renderers.</summary>
public enum TopologyNodeShape {
    /// <summary>A rectangle.</summary>
    Rectangle,
    /// <summary>A rounded rectangle.</summary>
    Rounded,
    /// <summary>A stadium with semicircular ends.</summary>
    Stadium,
    /// <summary>A circle.</summary>
    Circle,
    /// <summary>Two concentric circles.</summary>
    DoubleCircle,
    /// <summary>A decision diamond.</summary>
    Diamond,
    /// <summary>A six-sided preparation surface.</summary>
    Hexagon,
    /// <summary>A parallelogram leaning right.</summary>
    Parallelogram,
    /// <summary>A parallelogram leaning left.</summary>
    ParallelogramAlt,
    /// <summary>A trapezoid wider at the bottom.</summary>
    Trapezoid,
    /// <summary>A trapezoid wider at the top.</summary>
    TrapezoidAlt,
    /// <summary>A rectangle with subroutine rails.</summary>
    Subroutine,
    /// <summary>A database cylinder.</summary>
    Cylinder,
    /// <summary>A cloud surface.</summary>
    Cloud,
    /// <summary>An asymmetric flag surface.</summary>
    Asymmetric,
    /// <summary>An ellipse.</summary>
    Ellipse,
    /// <summary>A stick actor with a caption below it.</summary>
    Actor,
    /// <summary>A filled rectangular synchronization bar, using the node accent for its entire surface.</summary>
    SynchronizationBar,
    /// <summary>A document surface with a wavy lower edge and an inset reference rail.</summary>
    LinedDocument
}
