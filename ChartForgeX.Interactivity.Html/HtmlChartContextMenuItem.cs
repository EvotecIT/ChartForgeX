using System;

namespace ChartForgeX.Interactivity.Html;

/// <summary>A host-defined chart context-menu item. Choosing it dispatches a <c>cfxcontextaction</c> event from the chart section.</summary>
/// <remarks>The event detail carries the item's <c>id</c>, the <c>chartId</c>, the current <c>selection</c> and the exported
/// <c>data</c> rows. The generated page runs no host code; the host listens for the event.</remarks>
public sealed class HtmlChartContextMenuItem {
    /// <summary>Initializes a new context-menu item.</summary>
    /// <param name="id">A stable identifier, unique within the chart's menu.</param>
    /// <param name="label">The visible, already translated label.</param>
    public HtmlChartContextMenuItem(string id, string label) {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Context-menu items require an identifier.", nameof(id));
        if (string.IsNullOrWhiteSpace(label)) throw new ArgumentException("Context-menu items require a label.", nameof(label));
        Id = id.Trim();
        Label = label.Trim();
    }

    /// <summary>Gets the item's identifier.</summary>
    public string Id { get; }

    /// <summary>Gets the item's label.</summary>
    public string Label { get; }

    /// <summary>Gets or sets whether the item is shown as unavailable.</summary>
    public bool Disabled { get; set; }
}
