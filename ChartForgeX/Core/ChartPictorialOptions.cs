using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using PowerForge.Fluent;

namespace ChartForgeX.Core;

/// <summary>Pictorial symbols, values and artwork. Configure it through <see cref="ChartOptions.Pictorial"/>.</summary>
[Fluent(Copy = nameof(Clone))]
public sealed class ChartPictorialOptions {
    internal ChartPictorialOptions Clone() => (ChartPictorialOptions)MemberwiseClone();
    private ChartPictorialShape _pictorialShape = ChartPictorialShape.Circle;

    private int _pictorialColumns = 12;

    private double? _pictorialMaximum;

    private double? _pictorialValuePerSymbol;

    private double _pictorialSymbolScale = 1.0;

    private double _pictorialEmptyOpacity = 0.22;

    private string? _pictorialSvgPathData;

    private ChartRect _pictorialSvgPathViewBox = new(0, 0, 24, 24);



    /// <summary>
    /// Gets or sets the built-in symbol shape used by pictorial charts.
    /// </summary>
    public ChartPictorialShape Shape {
        get => _pictorialShape;
        set {
            if (!Enum.IsDefined(typeof(ChartPictorialShape), value)) throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown pictorial shape.");
            _pictorialShape = value;
        }
    }


    /// <summary>
    /// Gets or sets the number of repeated symbols used per pictorial chart row.
    /// </summary>
    public int Columns {
        get => _pictorialColumns;
        set {
            if (value < 1 || value > 100) throw new ArgumentOutOfRangeException(nameof(value), value, "Pictorial columns must be between one and one hundred.");
            _pictorialColumns = value;
        }
    }


    /// <summary>
    /// Gets or sets the optional scale maximum used by pictorial chart rows.
    /// </summary>
    public double? Max {
        get => _pictorialMaximum;
        set {
            ChartOptions.ValidateNullableFinite(value, nameof(value));
            if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value), value, "Pictorial maximum must be greater than zero.");
            _pictorialMaximum = value;
        }
    }


    /// <summary>
    /// Gets or sets the data value represented by one full pictorial symbol.
    /// </summary>
    public double? ValuePerSymbol {
        get => _pictorialValuePerSymbol;
        set {
            ChartOptions.ValidateNullableFinite(value, nameof(value));
            if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value), value, "Pictorial value per symbol must be greater than zero.");
            _pictorialValuePerSymbol = value;
        }
    }


    /// <summary>
    /// Gets or sets a value indicating whether numeric value labels are rendered for pictorial rows.
    /// </summary>
    public bool ShowValues { get; set; } = true;


    /// <summary>
    /// Gets or sets the relative pictorial symbol size inside each symbol slot.
    /// </summary>
    public double SymbolScale {
        get => _pictorialSymbolScale;
        set {
            ChartGuards.Finite(value, nameof(value));
            if (value < 0.4 || value > 1.4) throw new ArgumentOutOfRangeException(nameof(value), value, "Pictorial symbol scale must be between 0.4 and 1.4.");
            _pictorialSymbolScale = value;
        }
    }


    /// <summary>
    /// Gets or sets the opacity used by unfilled pictorial symbols.
    /// </summary>
    public double EmptyOpacity {
        get => _pictorialEmptyOpacity;
        set {
            ChartGuards.Finite(value, nameof(value));
            if (value < 0 || value > 1) throw new ArgumentOutOfRangeException(nameof(value), value, "Pictorial empty opacity must be between zero and one.");
            _pictorialEmptyOpacity = value;
        }
    }


    /// <summary>
    /// Gets or sets optional input path data used as the pictorial symbol. Prepared SVG and native PNG output
    /// paint the same numeric geometry compiled from this path.
    /// </summary>
    public string? SvgPathData {
        get => _pictorialSvgPathData;
        set {
            if (value == null) {
                _pictorialSvgPathData = null;
                return;
            }

            var trimmed = value.Trim();
            ChartOptions.ValidateSvgPathData(trimmed, nameof(value));
            _pictorialSvgPathData = trimmed;
        }
    }


    /// <summary>
    /// Gets or sets the viewBox used to scale <see cref="ChartPictorialOptions.SvgPathData"/>.
    /// </summary>
    public ChartRect SvgPathViewBox {
        get => _pictorialSvgPathViewBox;
        set {
            if (value.Width <= 0 || value.Height <= 0) throw new ArgumentOutOfRangeException(nameof(value), "Pictorial SVG path viewBox must have positive dimensions.");
            _pictorialSvgPathViewBox = value;
        }
    }


    /// <summary>Draws symbols from SVG path data drawn in <paramref name="viewBox"/>, in SVG and native PNG output.</summary>
    /// <param name="pathData">The SVG path data.</param>
    /// <param name="viewBox">The view box the path was drawn in.</param>
    /// <returns>The same options.</returns>
    public ChartPictorialOptions SvgPath(string pathData, ChartRect viewBox) {
        SvgPathData = pathData;
        SvgPathViewBox = viewBox;
        return this;
    }

    /// <summary>Draws symbols from SVG path data drawn in a 24-unit square, in SVG and native PNG output.</summary>
    /// <param name="pathData">The SVG path data.</param>
    /// <returns>The same options.</returns>
    public ChartPictorialOptions SvgPath(string pathData) => SvgPath(pathData, new ChartRect(0, 0, 24, 24));

    private ChartPictorialFillDirection _pictorialFillDirection;

    /// <summary>Gets or sets optional immutable image artwork. Null restores the authored custom contour or built-in symbol.</summary>
    /// <remarks>Artwork takes precedence without clearing any symbol, contour, value scale, color or spacing settings. Its own colors are preserved.</remarks>
    public ChartPictorialArtwork? Artwork { get; set; }

    /// <summary>Gets or sets the direction of the nominal partial rectangle for built-in, custom contour and image symbols.</summary>
    /// <remarks>LeftToRight is the unchanged default. Image values clip the centered fitted source rectangle; contours use their existing square symbol bounds.</remarks>
    public ChartPictorialFillDirection FillDirection {
        get => _pictorialFillDirection;
        set { if (!Enum.IsDefined(typeof(ChartPictorialFillDirection), value)) throw new ArgumentOutOfRangeException(nameof(value)); _pictorialFillDirection = value; }
    }
}
