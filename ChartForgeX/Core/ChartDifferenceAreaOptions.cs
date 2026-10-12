using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

/// <summary>Names and fill overrides for a paired difference area. Polarity always compares authored First with Second.</summary>
public sealed class ChartDifferenceAreaOptions {
    private string _firstName = "First", _secondName = "Second";
    private ChartColor? _positiveFill, _negativeFill;
    private double? _positiveFillOpacity, _negativeFillOpacity;

    /// <summary>Gets or sets the first observation's readout name. The default is First.</summary>
    public string FirstName { get => _firstName; set { _firstName = Name(value); IsConfigured = true; } }
    /// <summary>Gets or sets the second observation's readout name. The default is Second.</summary>
    public string SecondName { get => _secondName; set { _secondName = Name(value); IsConfigured = true; } }
    /// <summary>Gets or sets fill where First is greater than Second. Null inherits series or palette paint.</summary>
    public ChartColor? PositiveFill { get => _positiveFill; set { _positiveFill = value; IsConfigured = true; } }
    /// <summary>Gets or sets fill where First is less than Second. Null inherits explicit series paint or the next palette color.</summary>
    public ChartColor? NegativeFill { get => _negativeFill; set { _negativeFill = value; IsConfigured = true; } }
    /// <summary>Gets or sets the positive fill alpha multiplier from zero to one. Null inherits the area's theme opacity.</summary>
    public double? PositiveFillOpacity { get => _positiveFillOpacity; set { Opacity(value); _positiveFillOpacity = value; IsConfigured = true; } }
    /// <summary>Gets or sets the negative fill alpha multiplier from zero to one. Null inherits the area's theme opacity.</summary>
    public double? NegativeFillOpacity { get => _negativeFillOpacity; set { Opacity(value); _negativeFillOpacity = value; IsConfigured = true; } }

    internal bool IsConfigured { get; private set; }
    private static string Name(string value) => !string.IsNullOrWhiteSpace(value) ? value
        : throw new ArgumentException("A difference observation name must not be empty.", nameof(value));
    private static void Opacity(double? value) {
        if (value.HasValue) ChartGuards.UnitInterval(value.Value, nameof(value));
    }
}
