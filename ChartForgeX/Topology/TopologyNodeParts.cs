using ChartForgeX.Themes;
using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using PowerForge.Fluent;

namespace ChartForgeX.Topology;

/// <summary>Defines one named attachment point on a topology node boundary.</summary>
public sealed class TopologyNodePort {
    private string _id = string.Empty;
    private TopologyEdgePort _side = TopologyEdgePort.Right;
    private double _offset = 0.5;

    /// <summary>Gets or sets the node-local stable port identifier.</summary>
    public string Id { get => _id; set => _id = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the node boundary side. Auto is not valid for a named port.</summary>
    public TopologyEdgePort Side {
        get => _side;
        set {
            TopologyModelGuards.EnumDefined(value, nameof(value));
            if (value == TopologyEdgePort.Auto) throw new ArgumentOutOfRangeException(nameof(value), value, "Named ports require an explicit node side.");
            _side = value;
        }
    }

    /// <summary>Gets or sets the normalized position along the selected side from zero to one.</summary>
    public double Offset {
        get => _offset;
        set {
            TopologyModelGuards.Finite(value, nameof(value));
            if (value < 0 || value > 1) throw new ArgumentOutOfRangeException(nameof(value), value, "Port offset must be between zero and one.");
            _offset = value;
        }
    }

    /// <summary>Gets or sets an optional visible or host-facing label.</summary>
    public string? Label { get; set; }

    /// <summary>Gets arbitrary metadata for host adapters.</summary>
    public Dictionary<string, string> Metadata { get; } = new();
}

/// <summary>Defines a compact typed detail row shown inside a card-like topology node.</summary>
public sealed partial class TopologyNodeDetail {
    private string _label = string.Empty;
    private ChartForgeX.Typography.TextContent _valueContent = ChartForgeX.Typography.TextContent.Plain(string.Empty);
    private ChartForgeX.Typography.TextContent? _content;

    /// <summary>Gets or sets full-width text instead of the label/value columns.</summary>
    public string? Text { get => _content?.PlainText; set => _content = value == null ? null : ChartForgeX.Typography.TextContent.Plain(value); }

    /// <summary>Gets or sets the row label.</summary>
    public string Label { get => _label; set => _label = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the row value.</summary>
    public string Value { get => _valueContent.PlainText; set => _valueContent = ChartForgeX.Typography.TextContent.Plain(value ?? throw new ArgumentNullException(nameof(value))); }

    /// <summary>Gets or sets an optional icon id retained for capable hosts and future icon-aware renderers.</summary>
    public string? IconId { get; set; }

    /// <summary>Gets or sets an optional semantic status.</summary>
    public TopologyHealthStatus? Status { get; set; }

    /// <summary>Gets or sets an optional row accent color.</summary>
    public ChartColor? Color { get; set; }

    /// <summary>Gets arbitrary metadata for host adapters.</summary>
    public Dictionary<string, string> Metadata { get; } = new();
}

internal static class TopologyModelGuards {
    public static void EnumDefined<TEnum>(TEnum value, string parameterName) where TEnum : struct, Enum {
        if (typeof(TEnum) == typeof(TopologyEdgeLayoutInference)) {
            var mask = TopologyEdgeLayoutInference.SourcePort | TopologyEdgeLayoutInference.TargetPort | TopologyEdgeLayoutInference.RouteLane;
            var typed = (TopologyEdgeLayoutInference)(object)value;
            if ((typed & ~mask) == 0) return;
        }

        if (!Enum.IsDefined(typeof(TEnum), value)) throw new ArgumentOutOfRangeException(parameterName, value, "Unknown " + typeof(TEnum).Name + " value.");
    }

    public static void Finite(double value, string parameterName) {
        if (double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentOutOfRangeException(parameterName, value, "Value must be finite.");
    }

    public static void PositiveFinite(double value, string parameterName) {
        Finite(value, parameterName);
        if (value <= 0) throw new ArgumentOutOfRangeException(parameterName, value, "Value must be greater than zero.");
    }

    public static void NonNegativeFinite(double value, string parameterName) {
        Finite(value, parameterName);
        if (value < 0) throw new ArgumentOutOfRangeException(parameterName, value, "Value must be greater than or equal to zero.");
    }

    public static void Longitude(double value, string parameterName) {
        Finite(value, parameterName);
        if (value < -180 || value > 180) throw new ArgumentOutOfRangeException(parameterName, value, "Longitude must be between -180 and 180 degrees.");
    }

    public static void Latitude(double value, string parameterName) {
        Finite(value, parameterName);
        if (value < -90 || value > 90) throw new ArgumentOutOfRangeException(parameterName, value, "Latitude must be between -90 and 90 degrees.");
    }
}
