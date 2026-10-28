using System;
using ChartForgeX.Typography;

namespace ChartForgeX.Topology;

public sealed partial class TopologyNode {
    /// <summary>Gets or sets immutable safe caption content. Label projects its plain text; assigning Label resets inline formatting.</summary>
    public TextContent LabelContent { get => _labelContent; set => _labelContent = value ?? throw new ArgumentNullException(nameof(value)); }
}
public sealed partial class TopologyNodeDetail {
    /// <summary>Gets or sets full-width immutable content in place of the label/value columns. Text is its nullable plain projection.</summary>
    public TextContent? Content { get => _content; set => _content = value; }
    /// <summary>Gets or sets immutable content for the value column. Value is its plain projection and resets formatting when assigned.</summary>
    public TextContent ValueContent { get => _valueContent; set => _valueContent = value ?? throw new ArgumentNullException(nameof(value)); }
}

public sealed partial class TopologyGroup {
    /// <summary>Gets or sets immutable safe header content. Label projects its plain text; assigning Label resets inline formatting.</summary>
    public TextContent LabelContent { get => _labelContent; set => _labelContent = value ?? throw new ArgumentNullException(nameof(value)); }
}
public sealed partial class TopologyEdge {
    private TextContent? _labelContent;
    /// <summary>Gets or sets immutable safe primary relationship content. Label is its nullable plain projection; assigning Label resets inline formatting.</summary>
    /// <remarks>Metric-selected labels use their own plain value when it differs from this authored caption. Secondary and endpoint labels retain their existing policies.</remarks>
    public TextContent? LabelContent { get => _labelContent; set => _labelContent = value; }
}
