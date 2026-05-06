using System;

namespace ChartForgeX.Svg;

internal abstract class SvgNode {
    public SvgElement? Parent { get; private set; }

    public SvgNode Clone() => CloneCore();

    public bool RemoveFromParent() =>
        Parent?.Remove(this) == true;

    public string ToMarkup() {
        var writer = new SvgMarkupWriter();
        WriteTo(writer);
        return writer.Build();
    }

    public abstract void WriteTo(SvgMarkupWriter writer);

    protected abstract SvgNode CloneCore();

    internal void AttachTo(SvgElement parent) {
        if (Parent != null && !ReferenceEquals(Parent, parent)) {
            throw new InvalidOperationException("SVG nodes cannot be attached to more than one parent.");
        }

        Parent = parent;
    }

    internal void DetachFrom(SvgElement parent) {
        if (ReferenceEquals(Parent, parent)) Parent = null;
    }
}
