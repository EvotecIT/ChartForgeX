using System;

namespace ChartForgeX.Svg;

internal readonly struct SvgAttribute {
    public SvgAttribute(string name, string value, bool raw = false) {
        SvgMarkupWriter.ValidateName(name, nameof(name));
        Name = name;
        Value = value ?? throw new ArgumentNullException(nameof(value));
        IsRawPaint = raw;
    }

    public string Name { get; }

    public string Value { get; }

    /// <summary>Gets whether <see cref="Value"/> is a typed paint token written without escaping (see <see cref="Themes.SvgPaint"/>).</summary>
    public bool IsRawPaint { get; }
}
