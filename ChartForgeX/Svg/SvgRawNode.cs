using System;

namespace ChartForgeX.Svg;

internal sealed class SvgRawNode : SvgNode {
    public SvgRawNode(string? markup) {
        Markup = markup ?? string.Empty;
    }

    public string Markup { get; }

    public override void WriteTo(SvgMarkupWriter writer) {
        if (writer == null) throw new ArgumentNullException(nameof(writer));
        writer.Raw(Markup);
    }

    protected override SvgNode CloneCore() => new SvgRawNode(Markup);
}
