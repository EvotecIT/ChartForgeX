using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Composition;

/// <summary>Image layer for host-provided bitmap or SVG-compatible image references.</summary>
public sealed class VisualCanvasImageLayer : VisualCanvasLayer {
    private string _href = string.Empty;
    private double _opacity = 1;

    /// <summary>Initializes an image layer.</summary>
    /// <param name="x">The image X coordinate.</param>
    /// <param name="y">The image Y coordinate.</param>
    /// <param name="width">The rendered image width.</param>
    /// <param name="height">The rendered image height.</param>
    public VisualCanvasImageLayer(double x, double y, double width, double height) : base(x, y, width, height) { }

    /// <summary>Gets or sets the SVG image href. Renderers do not fetch it for PNG output.</summary>
    public string Href { get => _href; set => _href = value ?? throw new ArgumentNullException(nameof(value)); }
    /// <summary>Gets or sets borrowed source RGBA pixels. SVG embeds them as PNG when Href is empty.</summary>
    public byte[]? Rgba { get; set; }
    /// <summary>Gets or sets the source bitmap width for PNG output.</summary>
    public int SourceWidth { get; set; }
    /// <summary>Gets or sets the source bitmap height for PNG output.</summary>
    public int SourceHeight { get; set; }
    /// <summary>Gets or sets how the source image is placed inside the destination rectangle.</summary>
    public VisualCanvasImageFit Fit { get; set; }

    /// <summary>Gets or sets image opacity from zero to one.</summary>
    public double Opacity {
        get => _opacity;
        set {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0 || value > 1) throw new ArgumentOutOfRangeException(nameof(value), value, "Opacity must be between zero and one.");
            _opacity = value;
        }
    }
}

/// <summary>Compact feature-strip item.</summary>
public sealed class VisualCanvasFeatureItem {
    private string _icon;
    private string _label;

    /// <summary>Initializes a feature strip item.</summary>
    /// <param name="icon">The compact item icon or symbol.</param>
    /// <param name="label">The item label.</param>
    public VisualCanvasFeatureItem(string icon, string label) {
        _icon = icon ?? throw new ArgumentNullException(nameof(icon));
        _label = label ?? throw new ArgumentNullException(nameof(label));
    }

    /// <summary>Gets or sets the compact item icon or symbol.</summary>
    public string Icon { get => _icon; set => _icon = value ?? throw new ArgumentNullException(nameof(value)); }
    /// <summary>Gets or sets the item label.</summary>
    public string Label { get => _label; set => _label = value ?? throw new ArgumentNullException(nameof(value)); }
}

/// <summary>Feature-strip layer.</summary>
public sealed class VisualCanvasFeatureStripLayer : VisualCanvasLayer {
    private readonly List<VisualCanvasFeatureItem> _items = new();

    /// <summary>Initializes a feature strip layer.</summary>
    /// <param name="x">The strip X coordinate.</param>
    /// <param name="y">The strip Y coordinate.</param>
    /// <param name="width">The strip width.</param>
    /// <param name="height">The strip height.</param>
    /// <param name="items">The strip items.</param>
    public VisualCanvasFeatureStripLayer(double x, double y, double width, double height, IEnumerable<VisualCanvasFeatureItem> items) : base(x, y, width, height) {
        if (items == null) throw new ArgumentNullException(nameof(items));
        foreach (var item in items) _items.Add(item ?? throw new ArgumentException("Feature strips cannot contain null items.", nameof(items)));
        if (_items.Count == 0) throw new ArgumentException("Feature strips require at least one item.", nameof(items));
    }

    /// <summary>Gets the feature strip items.</summary>
    public IReadOnlyList<VisualCanvasFeatureItem> Items => _items;
    /// <summary>Gets or sets the feature strip accent color.</summary>
    public ChartColor Accent { get; set; } = ChartColor.FromHex("#2F80FF");
}
