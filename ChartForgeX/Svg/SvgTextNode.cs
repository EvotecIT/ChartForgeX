using System;

namespace ChartForgeX.Svg;

internal sealed class SvgTextNode : SvgNode {
    public SvgTextNode(string? text) {
        Text = text ?? string.Empty;
    }

    public string Text { get; }

    public override void WriteTo(SvgMarkupWriter writer) {
        if (writer == null) throw new ArgumentNullException(nameof(writer));
        writer.Text(Text);
    }

    protected override SvgNode CloneCore() => new SvgTextNode(Text);
}
