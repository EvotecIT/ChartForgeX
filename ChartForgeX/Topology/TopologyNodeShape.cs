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
    LinedDocument,
    /// <summary>A twelve-point burst with an enclosed caption area.</summary>
    Burst,
    /// <summary>A horizontal cylinder or queue, using the canonical cylinder contour with transposed axes.</summary>
    HorizontalCylinder,
    /// <summary>An outlined person figure with an external actor caption.</summary>
    ActorHollow,
    /// <summary>A stick actor with the UML business head marker.</summary>
    ActorBusiness,
    /// <summary>An outlined person figure with the UML business head marker.</summary>
    ActorBusinessHollow,
    /// <summary>A filled person silhouette with an external actor caption.</summary>
    ActorSilhouette,
    /// <summary>A framed native icon or artwork with an external actor caption.</summary>
    ActorIcon,
    /// <summary>An ellipse with the UML business-use-case marker.</summary>
    BusinessEllipse,
    /// <summary>Two horizontal data-store rules around an open caption area.</summary>
    DataStore = 27,
    /// <summary>A folder with a raised tab.</summary>
    Folder,
    /// <summary>A tapered bucket with an elliptical rim.</summary>
    Bucket,
    /// <summary>A terminal window with an inset prompt band.</summary>
    Console,
    /// <summary>A browser window with controls and an address band.</summary>
    Browser,
    /// <summary>A head and rounded torso with an enclosed torso caption.</summary>
    Person,
    /// <summary>A borderless text block with logical rectangular attachment bounds.</summary>
    TextBlock,
    /// <summary>A rectangle with a clipped upper-left corner.</summary>
    NotchedRectangle,
    /// <summary>A rectangle with one inset left rule.</summary>
    LinedRectangle,
    /// <summary>An accent-filled circle.</summary>
    FilledCircle,
    /// <summary>An outer circle around an accent-filled inner disk.</summary>
    FramedCircle,
    /// <summary>A crossed hourglass polygon.</summary>
    Hourglass,
    /// <summary>A left comment brace beside a logical caption area.</summary>
    BraceLeft,
    /// <summary>A right comment brace beside a logical caption area.</summary>
    BraceRight,
    /// <summary>Comment braces on both sides of a logical caption area.</summary>
    Braces,
    /// <summary>A lightning bolt polygon.</summary>
    LightningBolt,
    /// <summary>A document with a wavy lower edge.</summary>
    Document,
    /// <summary>A rectangle with a semicircular right end.</summary>
    HalfRoundedRectangle,
    /// <summary>A cylinder with an additional storage rule.</summary>
    LinedCylinder,
    /// <summary>A display surface with a left wedge and curved right side.</summary>
    CurvedTrapezoid,
    /// <summary>A rectangle with a reserved upper compartment.</summary>
    DividedRectangle,
    /// <summary>An upward triangle.</summary>
    Triangle,
    /// <summary>A rectangle with upper and left inset rules.</summary>
    WindowPane,
    /// <summary>A loop-limit surface with clipped upper corners.</summary>
    NotchedPentagon,
    /// <summary>A downward triangle.</summary>
    TriangleDown,
    /// <summary>A manual-input rectangle with a sloping upper edge.</summary>
    SlopedRectangle,
    /// <summary>Three offset document sheets.</summary>
    StackedDocument,
    /// <summary>Three offset rectangles.</summary>
    StackedRectangle,
    /// <summary>A stored-data rectangle with opposing curved sides.</summary>
    BowTieRectangle,
    /// <summary>A circle with an interior cross.</summary>
    CrossedCircle,
    /// <summary>A document with a lower-right tag.</summary>
    TaggedDocument,
    /// <summary>A rectangle with a lower-right tag.</summary>
    TaggedRectangle,
    /// <summary>A paper-tape rectangle with wavy upper and lower edges.</summary>
    WavyRectangle,
    /// <summary>A rounded summary with an upper caption and lower ellipsis indicator.</summary>
    CollapsedContainer,
    /// <summary>A burst bounded by concave circular scallops.</summary>
    ScallopedBurst
}
