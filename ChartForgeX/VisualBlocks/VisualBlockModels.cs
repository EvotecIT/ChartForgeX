using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Accessibility;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.VisualBlocks;

/// <summary>
/// Generic semantic status used by visual blocks.
/// </summary>
public enum VisualStatus {
    /// <summary>No semantic status.</summary>
    None,
    /// <summary>Neutral informational status.</summary>
    Neutral,
    /// <summary>Positive or healthy status.</summary>
    Positive,
    /// <summary>Warning or attention status.</summary>
    Warning,
    /// <summary>Negative or failed status.</summary>
    Negative,
    /// <summary>Informational status.</summary>
    Info
}

/// <summary>
/// Shared renderer-independent options for visual blocks.
/// </summary>
public sealed class VisualBlockOptions {
    private ChartSize _size = new(520, 300);
    private ChartPadding _padding = new(22, 22, 22, 22);
    private bool _hasExplicitPadding;
    private ChartTheme _theme = ChartTheme.GraphiteLight();
    private int _pngOutputScale = 1;

    /// <summary>Gets the text alternative, language, and decorative metadata used by static exports.</summary>
    public VisualAccessibility Accessibility { get; } = new();

    /// <summary>Gets or sets the rendered block size in pixels.</summary>
    public ChartSize Size {
        get => _size;
        set {
            if (value.Width <= 0 || value.Height <= 0) throw new ArgumentOutOfRangeException(nameof(value), "Visual block size must have positive dimensions.");
            _size = value;
        }
    }

    /// <summary>Gets or sets the inner content padding.</summary>
    public ChartPadding Padding {
        get => HostOwnsFrame ? new ChartPadding(0,0,0,0) : !_hasExplicitPadding && Theme.UseGraphiteLayout ? new ChartPadding(18,16,18,12) : _padding;
        set {
            ValidateNonNegative(value.Left, nameof(value));
            ValidateNonNegative(value.Top, nameof(value));
            ValidateNonNegative(value.Right, nameof(value));
            ValidateNonNegative(value.Bottom, nameof(value));
            _padding = value;
            _hasExplicitPadding = true;
        }
    }

    /// <summary>Gets or sets the visual theme used by renderers.</summary>
    public ChartTheme Theme {
        get => _theme;
        set => _theme = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>Gets or sets whether the full background should stay transparent.</summary>
    public bool TransparentBackground { get; set; }

    /// <summary>Gets or sets whether the outer card surface should be rendered.</summary>
    public bool ShowCard { get; set; } = true;
    /// <summary>Gets or sets whether the host owns the frame, removing the surface and outer padding.</summary>
    public bool HostOwnsFrame { get; set; }

    /// <summary>Gets or sets the output pixel multiplier used by PNG exports.</summary>
    public int PngOutputScale {
        get => _pngOutputScale;
        set {
            if (value < 1 || value > 4) throw new ArgumentOutOfRangeException(nameof(value), value, "PNG output scale must be between one and four.");
            _pngOutputScale = value;
        }
    }

    private static void ValidateNonNegative(double value, string parameterName) {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0) throw new ArgumentOutOfRangeException(parameterName, value, "Padding values must be finite and non-negative.");
    }
}

/// <summary>
/// Common contract for renderable non-chart visual blocks.
/// </summary>
public interface IVisualBlock : ChartForgeX.Rendering.IStaticVisualSource {
    /// <summary>Gets the block title.</summary>
    string Title { get; }

    /// <summary>Gets the block subtitle.</summary>
    string Subtitle { get; }

    /// <summary>Gets shared rendering options.</summary>
    VisualBlockOptions Options { get; }

    /// <summary>Gets a concise accessibility label.</summary>
    string AccessibleName { get; }
}

/// <summary>
/// Base class for fluent visual block models.
/// </summary>
/// <typeparam name="TSelf">The concrete visual block type.</typeparam>
public abstract class VisualBlock<TSelf> : IVisualBlock where TSelf : VisualBlock<TSelf> {
    private string _title = string.Empty;
    private string _subtitle = string.Empty;

    /// <summary>Gets or sets the block title.</summary>
    public string Title {
        get => _title;
        set => _title = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>Gets or sets the block subtitle.</summary>
    public string Subtitle {
        get => _subtitle;
        set => _subtitle = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>Gets shared rendering options.</summary>
    public VisualBlockOptions Options { get; } = new();

    /// <summary>Gets a concise accessibility label.</summary>
    public virtual string AccessibleName => Options.Accessibility.Name ?? (Title.Length == 0 ? GetType().Name : Title);

    /// <summary>Renders the producer-owned static SVG with an embedding identity scope.</summary>
    public virtual string RenderSvg(string idScope) => new SvgVisualBlockRenderer().RenderNative(this, idScope);

    /// <summary>Renders the producer-owned static pixels.</summary>
    public virtual ChartForgeX.Raster.RgbaImage RenderRgba() => new PngVisualBlockRenderer().RenderNativeImage(this);

    /// <summary>Sets the block title.</summary>
    public TSelf WithTitle(string title) { Title = title ?? throw new ArgumentNullException(nameof(title)); return Self(); }

    /// <summary>Sets the block subtitle.</summary>
    public TSelf WithSubtitle(string subtitle) { Subtitle = subtitle ?? throw new ArgumentNullException(nameof(subtitle)); return Self(); }

    /// <summary>Sets the rendered block size.</summary>
    public TSelf WithSize(int width, int height) { Options.Size = new ChartSize(width, height); return Self(); }

    /// <summary>Sets uniform inner content padding.</summary>
    public TSelf WithPadding(double padding) { Options.Padding = new ChartPadding(padding, padding, padding, padding); return Self(); }

    /// <summary>Sets the inner content padding.</summary>
    public TSelf WithPadding(double left, double top, double right, double bottom) { Options.Padding = new ChartPadding(left, top, right, bottom); return Self(); }

    /// <summary>Sets the visual theme.</summary>
    public TSelf WithTheme(ChartTheme theme) { Options.Theme = theme ?? throw new ArgumentNullException(nameof(theme)); return Self(); }

    /// <summary>Lets the embedding host own the surface and removes outer padding.</summary>
    public TSelf WithHostFrame(bool hostOwnsFrame = true) { Options.HostOwnsFrame = hostOwnsFrame; return Self(); }

    /// <summary>Sets whether the full background should stay transparent.</summary>
    public TSelf WithTransparentBackground(bool enabled = true) { Options.TransparentBackground = enabled; return Self(); }

    /// <summary>Sets whether the outer card surface should be rendered.</summary>
    public TSelf WithCard(bool enabled = true) { Options.ShowCard = enabled; return Self(); }

    /// <summary>Sets the output pixel multiplier used by PNG exports.</summary>
    public TSelf WithPngOutputScale(int scale) { Options.PngOutputScale = scale; return Self(); }

    private TSelf Self() => (TSelf)this;
}

internal static class VisualBlockGuards {
    public static void EnumDefined<TEnum>(TEnum value, string parameterName) where TEnum : struct {
        if (!Enum.IsDefined(typeof(TEnum), value)) throw new ArgumentOutOfRangeException(parameterName, value, "Unknown " + typeof(TEnum).Name + " value.");
    }

    public static void PositiveFinite(double value, string parameterName) {
        if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0) throw new ArgumentOutOfRangeException(parameterName, value, "Value must be finite and greater than zero.");
    }
}
