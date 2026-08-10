using System;
using System.Collections.Generic;

namespace ChartForgeX.SvgRaster;

internal sealed class SvgRasterDocument {
    public SvgRasterDocument(SvgRasterViewBox viewBox, SvgRasterElement root) {
        ViewBox = viewBox;
        Root = root ?? throw new ArgumentNullException(nameof(root));
    }

    public SvgRasterViewBox ViewBox { get; }

    public SvgRasterElement Root { get; }

    public IReadOnlyList<SvgRasterElement> Children => Root.Children;
}

internal sealed class SvgRasterElement {
    private readonly Dictionary<string, string> _attributes;

    public SvgRasterElement(string name, Dictionary<string, string> attributes, IReadOnlyList<SvgRasterElement> children, string text, IReadOnlyList<SvgRasterContent>? content = null) {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        _attributes = attributes ?? throw new ArgumentNullException(nameof(attributes));
        Children = children ?? throw new ArgumentNullException(nameof(children));
        Text = text ?? string.Empty;
        Content = content ?? (Text.Length == 0 ? Array.Empty<SvgRasterContent>() : new[] { SvgRasterContent.FromText(Text) });
    }

    public string Name { get; }

    public IReadOnlyList<SvgRasterElement> Children { get; }

    public string Text { get; }

    public IReadOnlyList<SvgRasterContent> Content { get; }

    public string? Get(string name) =>
        _attributes.TryGetValue(name, out var value) ? value : null;

    public bool TryGet(string name, out string value) =>
        _attributes.TryGetValue(name, out value!);

    public Dictionary<string, string> CopyAttributes() => new(_attributes, StringComparer.Ordinal);
}

internal sealed class SvgRasterContent {
    private SvgRasterContent(string? text, SvgRasterElement? element) {
        Text = text;
        Element = element;
    }

    public string? Text { get; }

    public SvgRasterElement? Element { get; }

    public static SvgRasterContent FromText(string text) => new(text ?? string.Empty, null);

    public static SvgRasterContent FromElement(SvgRasterElement element) => new(null, element ?? throw new ArgumentNullException(nameof(element)));
}
