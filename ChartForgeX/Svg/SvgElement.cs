using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ChartForgeX.Svg;

internal sealed class SvgElement : SvgNode {
    private readonly List<SvgAttribute> _attributes = new();
    private readonly List<SvgNode> _children = new();

    public SvgElement(string name) {
        SvgMarkupWriter.ValidateName(name, nameof(name));
        Name = name;
    }

    public string Name { get; }

    public IReadOnlyList<SvgAttribute> Attributes => _attributes;

    public IReadOnlyList<SvgNode> Children => _children;

    public SvgElement CloneElement() => (SvgElement)Clone();

    public SvgElement Attribute(string name, string? value) {
        if (value == null) return this;
        SetAttribute(new SvgAttribute(name, value));
        return this;
    }

    public SvgElement Attribute(string name, double value) =>
        Attribute(name, SvgMarkupWriter.FormatNumber(value));

    public SvgElement Attribute(string name, int value) =>
        Attribute(name, value.ToString(CultureInfo.InvariantCulture));

    public SvgElement Attribute(string name, bool value) =>
        Attribute(name, value ? "true" : "false");

    public SvgElement ViewBox(SvgViewBox? viewBox) =>
        Attribute("viewBox", viewBox?.ToMarkup());

    public SvgViewBox? GetViewBox() {
        var value = GetAttribute("viewBox");
        return value == null ? null : SvgViewBox.Parse(value);
    }

    public SvgElement Points(SvgPointList? points) =>
        Attribute("points", points?.ToMarkup());

    public SvgPointList? GetPoints() {
        var value = GetAttribute("points");
        return value == null ? null : SvgPointList.Parse(value);
    }

    public SvgElement PathData(SvgPathData? pathData) =>
        Attribute("d", pathData?.ToMarkup());

    public SvgPathData? GetPathData() {
        var value = GetAttribute("d");
        return value == null ? null : SvgPathData.Parse(value);
    }

    public SvgElement Transform(SvgTransformList? transform) =>
        Attribute("transform", transform?.ToMarkup());

    public SvgTransformList? GetTransform() {
        var value = GetAttribute("transform");
        return value == null ? null : SvgTransformList.Parse(value);
    }

    public SvgElement Style(SvgStyleDeclarationList? style) {
        if (style == null || style.IsEmpty) {
            RemoveAttribute("style");
            return this;
        }

        return Attribute("style", style.ToMarkup());
    }

    public SvgStyleDeclarationList? GetStyle() {
        var value = GetAttribute("style");
        return value == null ? null : SvgStyleDeclarationList.Parse(value);
    }

    public SvgElement OptionalAttribute(string name, double? value) =>
        value.HasValue ? Attribute(name, value.Value) : this;

    public SvgElement OptionalAttribute(string name, int? value) =>
        value.HasValue ? Attribute(name, value.Value) : this;

    public SvgElement Class(string? className) {
        if (string.IsNullOrWhiteSpace(className)) return this;
        var trimmedClassName = (className ?? string.Empty).Trim();
        var current = GetAttribute("class");
        return Attribute("class", string.IsNullOrWhiteSpace(current) ? trimmedClassName : current + " " + trimmedClassName);
    }

    public SvgElement Data(string name, string? value) {
        SvgMarkupWriter.ValidateName("data-" + name, nameof(name));
        return Attribute("data-" + name, value);
    }

    public SvgElement Style(string name, string? value) {
        if (string.IsNullOrWhiteSpace(name)) return this;
        var style = GetStyle() ?? new SvgStyleDeclarationList();
        style.Set(name, value);
        return Style(style);
    }

    public string? GetAttribute(string name) {
        for (var i = _attributes.Count - 1; i >= 0; i--) {
            if (string.Equals(_attributes[i].Name, name, StringComparison.Ordinal)) return _attributes[i].Value;
        }

        return null;
    }

    public bool RemoveAttribute(string name) {
        for (var i = 0; i < _attributes.Count; i++) {
            if (!string.Equals(_attributes[i].Name, name, StringComparison.Ordinal)) continue;
            _attributes.RemoveAt(i);
            return true;
        }

        return false;
    }

    public bool HasClass(string className) =>
        !string.IsNullOrWhiteSpace(className) && HasClass(this, className);

    public SvgElement RemoveClass(string className) {
        if (string.IsNullOrWhiteSpace(className)) return this;
        var value = GetAttribute("class");
        if (string.IsNullOrWhiteSpace(value)) return this;

        var tokens = value!.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(token => !string.Equals(token, className, StringComparison.Ordinal))
            .ToArray();
        if (tokens.Length == 0) RemoveAttribute("class");
        else Attribute("class", string.Join(" ", tokens));
        return this;
    }

    public SvgElement ClearChildren() {
        foreach (var child in _children) child.DetachFrom(this);
        _children.Clear();
        return this;
    }

    public SvgElement SetText(string? text) =>
        ClearChildren().Text(text);

    public int IndexOf(SvgNode node) {
        if (node == null) throw new ArgumentNullException(nameof(node));
        for (var i = 0; i < _children.Count; i++) {
            if (ReferenceEquals(_children[i], node)) return i;
        }

        return -1;
    }

    public SvgElement Add(SvgNode? node) =>
        Insert(_children.Count, node);

    public SvgElement Insert(int index, SvgNode? node) {
        if (node == null) return this;
        if (index < 0 || index > _children.Count) throw new ArgumentOutOfRangeException(nameof(index));
        node.AttachTo(this);
        _children.Insert(index, node);
        return this;
    }

    public bool Remove(SvgNode? node) {
        if (node == null) return false;
        var index = IndexOf(node);
        if (index < 0) return false;
        _children.RemoveAt(index);
        node.DetachFrom(this);
        return true;
    }

    public SvgElement AddElement(SvgElement? element) => Add(element);

    public SvgElement Element(string name, Action<SvgElement>? configure = null) {
        var element = new SvgElement(name);
        configure?.Invoke(element);
        Add(element);
        return element;
    }

    public SvgElement Text(string? text) => Add(new SvgTextNode(text));

    public SvgElement Raw(string? markup) => Add(new SvgRawNode(markup));

    public SvgElement Comment(string? text) => Add(new SvgCommentNode(text));

    public SvgElement? FindById(string id) =>
        DescendantsAndSelf().FirstOrDefault(element => string.Equals(element.GetAttribute("id"), id, StringComparison.Ordinal));

    public IEnumerable<SvgElement> FindByTag(string name) =>
        DescendantsAndSelf().Where(element => string.Equals(element.Name, name, StringComparison.Ordinal));

    public IEnumerable<SvgElement> FindByClass(string className) {
        if (string.IsNullOrWhiteSpace(className)) return Enumerable.Empty<SvgElement>();
        return DescendantsAndSelf().Where(element => HasClass(element, className));
    }

    public IEnumerable<SvgElement> DescendantsAndSelf() {
        yield return this;
        foreach (var child in _children) {
            if (child is not SvgElement element) continue;
            foreach (var descendant in element.DescendantsAndSelf()) yield return descendant;
        }
    }

    public override void WriteTo(SvgMarkupWriter writer) {
        if (writer == null) throw new ArgumentNullException(nameof(writer));
        writer.StartElement(Name);
        foreach (var attribute in _attributes) writer.Attribute(attribute.Name, attribute.Value);
        if (_children.Count == 0) {
            writer.EndEmptyElement();
            return;
        }

        writer.EndStartElement();
        foreach (var child in _children) child.WriteTo(writer);
        writer.EndElement();
    }

    protected override SvgNode CloneCore() {
        var clone = new SvgElement(Name);
        foreach (var attribute in _attributes) clone.Attribute(attribute.Name, attribute.Value);
        foreach (var child in _children) clone.Add(child.Clone());
        return clone;
    }

    private void SetAttribute(SvgAttribute attribute) {
        for (var i = 0; i < _attributes.Count; i++) {
            if (!string.Equals(_attributes[i].Name, attribute.Name, StringComparison.Ordinal)) continue;
            _attributes[i] = attribute;
            return;
        }

        _attributes.Add(attribute);
    }

    private static bool HasClass(SvgElement element, string className) {
        var value = element.GetAttribute("class");
        if (string.IsNullOrWhiteSpace(value)) return false;
        var tokens = value!.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        return tokens.Any(token => string.Equals(token, className, StringComparison.Ordinal));
    }
}
