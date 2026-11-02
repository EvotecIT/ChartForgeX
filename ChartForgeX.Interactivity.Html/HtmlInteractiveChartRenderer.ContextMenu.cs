using System;
using System.Collections.Generic;
using System.Text;

namespace ChartForgeX.Interactivity.Html;

public sealed partial class HtmlInteractiveChartRenderer {
    private static string? ContextMenuItemsJson(HtmlChartInteractionOptions options) {
        if (options.ContextMenuItems.Count == 0) return null;
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var json = new StringBuilder("[");
        foreach (var item in options.ContextMenuItems) {
            if (item == null) throw new InvalidOperationException("Context-menu items must not be null.");
            if (!ids.Add(item.Id)) throw new InvalidOperationException("Context-menu item identifiers must be unique: " + item.Id);
            if (json.Length > 1) json.Append(',');
            json.Append("{\"id\":").Append(HtmlJsonString.Encode(item.Id)).Append(",\"label\":").Append(HtmlJsonString.Encode(item.Label));
            if (item.Disabled) json.Append(",\"disabled\":true");
            json.Append('}');
        }
        return json.Append(']').ToString();
    }
}
