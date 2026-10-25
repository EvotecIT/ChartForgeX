using System;
using System.Collections.Generic;
using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;

/// <summary>A real host boundary for opt-in native Polar gestures; the transport and opaque basis store stay with the host.</summary>
public static class PolarNavigationRecipe {
    /// <summary>Prepares one native section and its adaptive alternate for a host-held immutable source chart.</summary>
    public static HtmlChartSection PrepareSection(Chart chart, ChartPolarViewport? viewport = null) =>
        new HtmlInteractiveChartRenderer().PreparePolarSection(chart, viewport, options => {
            options.Interaction.ChartId = "polar-native-navigation"; options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive;
            options.Adaptive.NarrowWidth = 360; options.Adaptive.NarrowHeight = 420;
            options.Interaction.Enable(ChartInteractionFeatures.Zoom | ChartInteractionFeatures.Pan | ChartInteractionFeatures.Export);
        });
    /// <summary>Decodes one browser request against the exact host-retained basis and requested adaptive drawing.</summary>
    /// <remarks>The host verifies its opaque basis, captures the source chart, honors cancellation and returns a new basis with this section.
    /// It retains earlier accepted basis snapshots until outstanding requests settle. This method does not advance a transport session.</remarks>
    public static HtmlChartSection PrepareReply(Chart chart, HtmlChartSection basis, JsonElement request) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        if (basis == null) throw new ArgumentNullException(nameof(basis));
        if (request.GetProperty("coordinateSystem").GetString() != "polar") throw new ArgumentException("A polar native request is required.", nameof(request));
        var drawing = !request.TryGetProperty("drawing", out var selected) ? "wide" : selected.GetString();
        var prepared = drawing == "wide" ? basis.PreparedVisual : drawing == "narrow" ? basis.NarrowPreparedVisual
            : throw new ArgumentException("The request names an unsupported drawing.", nameof(request));
        var projection = prepared?.PolarProjection ?? throw new ArgumentException("The basis has no requested captured polar drawing.", nameof(basis));
        var axes = ChartPolarNavigationAxes.None; var seen = new HashSet<string>();
        foreach (var item in request.GetProperty("axes").EnumerateArray()) {
            var name = item.GetString() ?? string.Empty;
            if (!seen.Add(name)) throw new ArgumentException("A requested polar coordinate is repeated.", nameof(request));
            axes |= name switch {
                "angle" => ChartPolarNavigationAxes.Angle, "radius" => ChartPolarNavigationAxes.Radius,
                "secondaryRadius" => ChartPolarNavigationAxes.SecondaryRadius,
                _ => throw new ArgumentException("The request names an unsupported polar coordinate.", nameof(request))
            };
        }
        if (axes == ChartPolarNavigationAxes.None) throw new ArgumentException("The request requires a polar coordinate.", nameof(request));
        if ((axes & ChartPolarNavigationAxes.Radius) != 0) _ = projection.GetRadiusWindow();
        if ((axes & ChartPolarNavigationAxes.SecondaryRadius) != 0) _ = projection.GetRadiusWindow(ChartAxisSide.Secondary);
        ChartPolarViewport? viewport;
        switch (request.GetProperty("kind").GetString()) {
            case "reset": viewport = null; break;
            case "zoom":
                var factor = request.GetProperty("factor").GetDouble();
                viewport = request.TryGetProperty("anchor", out var anchor)
                    ? projection.Zoom(factor, new ChartPoint(anchor.GetProperty("x").GetDouble(), anchor.GetProperty("y").GetDouble()), axes)
                    : projection.Zoom(factor, axes); break;
            case "pan":
                viewport = projection.Pan(request.GetProperty("clockwiseAngleDelta").GetDouble(), request.GetProperty("outwardRadiusDelta").GetDouble(), axes); break;
            case "window":
                var source = request.GetProperty("sourceViewport");
                foreach (var field in source.EnumerateObject()) if (!seen.Contains(field.Name))
                    throw new ArgumentException("A source window names an unselected coordinate.", nameof(request));
                foreach (var name in seen) if (!source.TryGetProperty(name, out _))
                    throw new ArgumentException("A selected coordinate has no source window.", nameof(request));
                ChartAxisWindow? Window(string name, ChartAxisWindow? retained) => source.TryGetProperty(name, out var window)
                    ? new ChartAxisWindow(window.GetProperty("minimum").GetDouble(), window.GetProperty("maximum").GetDouble(),
                        !window.TryGetProperty("maximumInclusive", out var inclusive) || inclusive.GetBoolean()) : retained;
                viewport = new ChartPolarViewport(Window("angle", projection.Viewport?.Angle),
                    Window("radius", projection.Viewport?.Radius), Window("secondaryRadius", projection.Viewport?.SecondaryRadius)); break;
            default: throw new ArgumentException("The host does not support this native polar request.", nameof(request));
        }
        return PrepareSection(chart, viewport);
    }
}
