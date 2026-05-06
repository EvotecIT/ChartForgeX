using System;

namespace ChartForgeX.Svg;

internal sealed class SvgCommentNode : SvgNode {
    public SvgCommentNode(string? text) {
        Text = text ?? string.Empty;
    }

    public string Text { get; }

    public override void WriteTo(SvgMarkupWriter writer) {
        if (writer == null) throw new ArgumentNullException(nameof(writer));
        writer.Comment(Text);
    }

    protected override SvgNode CloneCore() => new SvgCommentNode(Text);
}
