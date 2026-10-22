using System;
using System.Collections.Generic;

namespace ChartForgeX.VisualBlocks;

/// <summary>Identifies source set memberships that cannot form a consistent Venn diagram.</summary>
public sealed class VennDiagramValidationException : InvalidOperationException {
    /// <summary>Initializes an error for the identified set memberships.</summary>
    public VennDiagramValidationException(string message, IEnumerable<string> setIds) : base(message) { SetIds = new List<string>(setIds ?? throw new ArgumentNullException(nameof(setIds))).AsReadOnly(); }
    /// <summary>Gets the identities involved in the inconsistent region.</summary>
    public IReadOnlyList<string> SetIds { get; }
}
