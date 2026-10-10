using System.Collections.Generic;
using System.Globalization;

namespace ChartForgeX.Rendering;

internal sealed partial class ChartStackLayout {
    /// <summary>Writes the same compiled contribution facts for every stack renderer.</summary>
    internal static void AddMetadata(IDictionary<string, string> metadata, ChartStackPoint stack) {
        metadata["data-cfx-base"] = Number(stack.Base);
        if (!stack.IsStacked) return;
        metadata["data-cfx-rendered-y"] = Number(stack.Value);
        metadata["data-cfx-stack-end"] = Number(stack.End);
        metadata["data-cfx-stack-total"] = Number(stack.Total);
        metadata["data-cfx-stack-source-total"] = Number(stack.SourceTotal);
        metadata["data-cfx-stack-group"] = stack.Group ?? string.Empty;
        if (stack.NormalizedTo.HasValue) metadata["data-cfx-normalized-to"] = Number(stack.NormalizedTo.Value);
    }

    private static string Number(double value) => value.ToString("G17", CultureInfo.InvariantCulture);
}
