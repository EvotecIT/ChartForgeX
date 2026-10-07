using System;
using System.Collections.Generic;
using System.Text;

namespace ChartForgeX.Rendering;

/// <summary>
/// The markup tree of a label scene: elements, text and the other node kinds rendered SVG can carry, with the few
/// tree operations the scene uses. It replaces the XLinq round trip of every rendered chart: parsing builds the same
/// tree <c>XDocument.Load</c> with preserved white space builds, and <see cref="ToString"/> writes exactly what
/// <c>XDocument.ToString(SaveOptions.DisableFormatting)</c> writes for that tree.
/// </summary>
/// <remarks>
/// Names are kept as written (qualified, with their prefix). Markup the parser does not take as it is is first
/// normalized by the framework (see <see cref="SvgMarkupParser"/>), so names here are always the ones the framework
/// writer would write.
/// </remarks>
internal sealed class SvgMarkupDocument {
    internal SvgMarkupDocument(List<SvgMarkupNode> nodes, SvgMarkupElement root) {
        Nodes = nodes;
        Root = root;
    }

    /// <summary>The document-level nodes: white space around the root, comments, processing instructions and the root.</summary>
    internal List<SvgMarkupNode> Nodes { get; }

    internal SvgMarkupElement Root { get; }

    /// <summary>Every element of the document (the root and all below it), in document order, as XLinq's <c>XDocument.Descendants()</c>.</summary>
    internal IEnumerable<SvgMarkupElement> Descendants() => Root.DescendantsAndSelf();

    public override string ToString() => SvgMarkupSerializer.Write(this);
}

/// <summary>A node of a <see cref="SvgMarkupDocument"/>.</summary>
internal abstract class SvgMarkupNode {
}

/// <summary>
/// Character data: text, or a CDATA section when <see cref="IsCData"/>. Text nodes are immutable and carry no parent, so
/// one instance can stand for every equal text (the line ends between elements).
/// </summary>
internal sealed class SvgMarkupText : SvgMarkupNode {
    /// <summary>The line end the renderers put between elements, after line-end normalization.</summary>
    internal static readonly SvgMarkupText LineFeed = new("\n");

    internal SvgMarkupText(string value, bool isCData = false, string? raw = null) {
        Value = value;
        IsCData = isCData;
        Raw = raw ?? value;
    }

    internal string Value { get; }
    internal bool IsCData { get; }
    /// <summary>The section content as written, for CDATA.</summary>
    internal string Raw { get; }
}

/// <summary>A comment or processing instruction, kept as the framework writer writes it. Immutable, like text.</summary>
internal sealed class SvgMarkupOther : SvgMarkupNode {
    internal SvgMarkupOther(string markup) => Markup = markup;

    internal string Markup { get; }
}

/// <summary>An element with its attributes in document order and its child nodes.</summary>
internal sealed class SvgMarkupElement : SvgMarkupNode {
    // Attribute names and values interleaved: [name0, value0, name1, value1, ...].
    private string[] _attributes;
    private int _attributeCount;
    private List<SvgMarkupNode>? _nodes;
    private bool _hasEndTag;

    internal SvgMarkupElement(string name, int attributeCapacity = 4) {
        Name = name;
        _attributes = attributeCapacity == 0 ? Array.Empty<string>() : new string[attributeCapacity * 2];
    }

    /// <summary>The name as written, with its prefix.</summary>
    internal string Name { get; set; }

    internal SvgMarkupElement? Parent { get; private set; }

    /// <summary>The name without its prefix.</summary>
    internal string LocalName {
        get {
            var colon = Name.IndexOf(':');
            return colon < 0 ? Name : Name.Substring(colon + 1);
        }
    }

    /// <summary>The prefix of the name and the colon (<c>"p:"</c>), or an empty string.</summary>
    internal string PrefixWithColon {
        get {
            var colon = Name.IndexOf(':');
            return colon < 0 ? string.Empty : Name.Substring(0, colon + 1);
        }
    }

    /// <summary>
    /// True when the element has no content node and was not written with an end tag; such an element serializes as
    /// <c>&lt;name /&gt;</c>, any other as a start and an end tag, as XLinq's <c>IsEmpty</c>.
    /// </summary>
    internal bool IsEmpty => !_hasEndTag && (_nodes == null || _nodes.Count == 0);

    internal int AttributeCount => _attributeCount;
    internal string AttributeName(int index) => _attributes[index * 2];
    internal string AttributeValue(int index) => _attributes[index * 2 + 1];

    /// <summary>The child nodes; empty for an empty element.</summary>
    internal IReadOnlyList<SvgMarkupNode> Nodes => (IReadOnlyList<SvgMarkupNode>?)_nodes ?? Array.Empty<SvgMarkupNode>();

    internal bool HasElements {
        get {
            if (_nodes == null) return false;
            foreach (var node in _nodes) {
                if (node is SvgMarkupElement) return true;
            }

            return false;
        }
    }

    /// <summary>Appends an attribute; the caller guarantees the name is not present yet.</summary>
    internal void AddAttribute(string name, string value) {
        if (_attributeCount * 2 == _attributes.Length) Array.Resize(ref _attributes, Math.Max(8, _attributes.Length * 2));
        _attributes[_attributeCount * 2] = name;
        _attributes[_attributeCount * 2 + 1] = value;
        _attributeCount++;
    }

    /// <summary>The value of the attribute with this exact name, as XLinq's <c>Attribute(name)</c> for a name without a namespace.</summary>
    internal string? Attribute(string name) {
        var attributes = _attributes;
        for (var i = 0; i < _attributeCount * 2; i += 2) {
            if (string.Equals(attributes[i], name, StringComparison.Ordinal)) return attributes[i + 1];
        }

        return null;
    }

    /// <summary>
    /// Sets, adds (at the end) or, for a null value, removes an attribute, as XLinq's <c>SetAttributeValue</c> does for a
    /// string value.
    /// </summary>
    internal void SetAttributeValue(string name, string? value) {
        for (var i = 0; i < _attributeCount; i++) {
            if (!string.Equals(_attributes[i * 2], name, StringComparison.Ordinal)) continue;
            if (value != null) {
                _attributes[i * 2 + 1] = value;
                return;
            }

            Array.Copy(_attributes, (i + 1) * 2, _attributes, i * 2, (_attributeCount - i - 1) * 2);
            _attributeCount--;
            _attributes[_attributeCount * 2] = null!;
            _attributes[_attributeCount * 2 + 1] = null!;
            return;
        }

        if (value != null) AddAttribute(name, value);
    }

    /// <summary>Sets an integer attribute in the invariant form XLinq writes.</summary>
    internal void SetAttributeValue(string name, int value) => SetAttributeValue(name, System.Xml.XmlConvert.ToString(value));

    /// <summary>Sets a number attribute in the form XLinq writes (XmlConvert).</summary>
    internal void SetAttributeValue(string name, double value) => SetAttributeValue(name, System.Xml.XmlConvert.ToString(value));

    /// <summary>Appends a child during parsing or construction.</summary>
    internal void Add(SvgMarkupNode node) {
        if (node is SvgMarkupElement element) element.Parent = this;
        (_nodes ??= new List<SvgMarkupNode>()).Add(node);
    }

    /// <summary>Replaces the text or other non-element child at <paramref name="index"/>.</summary>
    internal void ReplaceNode(int index, SvgMarkupNode node) {
        if (_nodes == null || _nodes[index] is SvgMarkupElement || node is SvgMarkupElement) throw new InvalidOperationException("Only text and other nodes can be replaced.");
        _nodes[index] = node;
    }

    /// <summary>Marks the element as written with an end tag, so it keeps one even without content.</summary>
    internal void KeepEndTag() => _hasEndTag = true;

    /// <summary>
    /// The concatenated text of every descendant text and CDATA node, as XLinq's <c>Value</c>. Setting it replaces every
    /// child with the text; an empty text keeps the end tag.
    /// </summary>
    internal string Value {
        get {
            if (_nodes == null || _nodes.Count == 0) return string.Empty;
            if (_nodes.Count == 1 && _nodes[0] is SvgMarkupText single) return single.Value;
            var builder = new StringBuilder();
            AppendText(builder);
            return builder.ToString();
        }
        set {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (_nodes != null) {
                foreach (var node in _nodes) {
                    if (node is SvgMarkupElement element) element.Parent = null;
                }

                _nodes.Clear();
            }

            _hasEndTag = true;
            if (value.Length > 0) Add(new SvgMarkupText(value));
        }
    }

    private void AppendText(StringBuilder builder) {
        if (_nodes == null) return;
        foreach (var node in _nodes) {
            if (node is SvgMarkupText text) builder.Append(text.Value);
            else if (node is SvgMarkupElement element) element.AppendText(builder);
        }
    }

    internal IEnumerable<SvgMarkupElement> Elements() {
        if (_nodes == null) yield break;
        foreach (var node in _nodes) {
            if (node is SvgMarkupElement element) yield return element;
        }
    }

    internal IEnumerable<SvgMarkupElement> Ancestors() {
        for (var element = Parent; element != null; element = element.Parent) yield return element;
    }

    internal IEnumerable<SvgMarkupElement> AncestorsAndSelf() {
        for (var element = this; element != null; element = element.Parent) yield return element;
    }

    /// <summary>This element and every element below it, in document order.</summary>
    internal IEnumerable<SvgMarkupElement> DescendantsAndSelf() {
        var pending = new Stack<(SvgMarkupElement Element, int Next)>();
        yield return this;
        pending.Push((this, 0));
        while (pending.Count > 0) {
            var (element, next) = pending.Pop();
            var nodes = element._nodes;
            if (nodes == null) continue;
            for (var i = next; i < nodes.Count; i++) {
                if (nodes[i] is not SvgMarkupElement child) continue;
                // Come back to the following siblings after the child's own subtree.
                pending.Push((element, i + 1));
                yield return child;
                pending.Push((child, 0));
                break;
            }
        }
    }

    /// <summary>
    /// Creates an element named <paramref name="localName"/> in this element's namespace, to be inserted as its sibling,
    /// named as XLinq's writer names such an element. When this element declares no namespaces its prefix is in scope
    /// at the sibling too, so the sibling takes the same prefix. Otherwise the sibling takes the nearest prefix bound to
    /// the namespace in the parent's scope, or, when there is none, an empty prefix and the default namespace
    /// declaration the writer adds (<paramref name="declaration"/>, to be appended after the other attributes).
    /// </summary>
    internal SvgMarkupElement CreateSibling(string localName, int attributeCapacity, out string? declaration) {
        declaration = null;
        if (!DeclaresNamespaces()) return new SvgMarkupElement(PrefixWithColon + localName, attributeCapacity);
        var colon = Name.IndexOf(':');
        var prefix = colon < 0 ? string.Empty : Name.Substring(0, colon);
        var space = LookupNamespace(this, prefix) ?? string.Empty;
        if (space.Length == 0) {
            if (!string.IsNullOrEmpty(LookupNamespace(Parent, string.Empty))) declaration = string.Empty;
            return new SvgMarkupElement(localName, attributeCapacity);
        }

        var bound = FindPrefix(Parent, space);
        if (bound == null) {
            if (LookupNamespace(Parent, string.Empty) != space) declaration = space;
            return new SvgMarkupElement(localName, attributeCapacity);
        }

        return new SvgMarkupElement(bound.Length == 0 ? localName : bound + ":" + localName, attributeCapacity);
    }

    private bool DeclaresNamespaces() {
        for (var i = 0; i < _attributeCount; i++) {
            if (IsDeclaration(_attributes[i * 2], out _)) return true;
        }

        return false;
    }

    private static bool IsDeclaration(string name, out string prefix) {
        if (name == "xmlns") {
            prefix = string.Empty;
            return true;
        }

        prefix = name.StartsWith("xmlns:", StringComparison.Ordinal) ? name.Substring(6) : string.Empty;
        return prefix.Length > 0;
    }

    // The namespace bound to prefix at element (null element: the document scope).
    private static string? LookupNamespace(SvgMarkupElement? element, string prefix) {
        var name = prefix.Length == 0 ? "xmlns" : "xmlns:" + prefix;
        for (; element != null; element = element.Parent) {
            if (element.Attribute(name) is { } space) return space;
        }

        return prefix.Length == 0 ? string.Empty : prefix == "xml" ? "http://www.w3.org/XML/1998/namespace" : null;
    }

    // The nearest prefix bound to space at element that no nearer declaration rebinds.
    private static string? FindPrefix(SvgMarkupElement? element, string space) {
        for (var scope = element; scope != null; scope = scope.Parent) {
            for (var i = scope._attributeCount - 1; i >= 0; i--) {
                if (!IsDeclaration(scope._attributes[i * 2], out var prefix) || scope._attributes[i * 2 + 1] != space) continue;
                if (LookupNamespace(element, prefix) == space) return prefix;
            }
        }

        return null;
    }

    /// <summary>Inserts an element just before this one in its parent.</summary>
    internal void AddBeforeSelf(SvgMarkupElement element) {
        var parent = Parent ?? throw new InvalidOperationException("The element has no parent.");
        var nodes = parent._nodes!;
        element.Parent = parent;
        nodes.Insert(nodes.IndexOf(this), element);
    }

    /// <summary>A deep copy without a parent, as <c>new XElement(element)</c>.</summary>
    internal SvgMarkupElement Clone() {
        var copy = new SvgMarkupElement(Name, 0) {
            _attributes = (string[])_attributes.Clone(),
            _attributeCount = _attributeCount,
            _hasEndTag = _hasEndTag
        };
        if (_nodes == null) return copy;
        copy._nodes = new List<SvgMarkupNode>(_nodes.Count);
        foreach (var node in _nodes) {
            if (node is SvgMarkupElement element) {
                var child = element.Clone();
                child.Parent = copy;
                copy._nodes.Add(child);
            } else {
                // Text and other nodes are immutable and shared.
                copy._nodes.Add(node);
            }
        }

        return copy;
    }
}
