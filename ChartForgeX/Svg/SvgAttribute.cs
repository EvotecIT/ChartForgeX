using System;

namespace ChartForgeX.Svg;

internal readonly struct SvgAttribute {
    public SvgAttribute(string name, string value) {
        SvgMarkupWriter.ValidateName(name, nameof(name));
        Name = name;
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }

    public string Name { get; }

    public string Value { get; }
}
