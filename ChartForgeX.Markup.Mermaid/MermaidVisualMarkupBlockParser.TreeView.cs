using ChartForgeX.Mermaid;

namespace ChartForgeX.Markup.Mermaid;

public sealed partial class MermaidVisualMarkupBlockParser {
    private MermaidTreeViewRenderOptions BuildTreeViewOptions(VisualMarkupBlock block) {
        var options = (MermaidTreeViewRenderOptions)_treeViewRenderOptions.Clone();
        if (TryGetAttribute(block, "id", out var id) && !string.IsNullOrWhiteSpace(id)) options.Id = id;
        if (TryGetAttribute(block, "title", out var title) && !string.IsNullOrWhiteSpace(title)) options.Title = title;
        if (TryGetAttribute(block, "subtitle", out var subtitle) && !string.IsNullOrWhiteSpace(subtitle)) options.Subtitle = subtitle;
        if (TryReadIntAttribute(block, "width", out var width)) options.Width = width;
        if (TryReadIntAttribute(block, "height", out var height)) options.Height = height;
        return options;
    }
}
