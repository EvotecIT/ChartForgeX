using System;
using ChartForgeX.Core;

namespace ChartForgeX.Markup;

/// <summary>
/// Describes a ChartForgeX chart parsed from markup.
/// </summary>
public sealed class MarkupChartDocument {
    private string _id = "chart";

    /// <summary>Gets or sets the artifact identifier.</summary>
    public string Id { get => _id; set => _id = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the parsed chart model.</summary>
    public Chart Chart { get; set; } = Chart.Create();
}
