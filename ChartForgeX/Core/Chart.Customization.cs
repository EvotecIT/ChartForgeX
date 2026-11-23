using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {

    /// <summary>
    /// Configures style overrides for a chart text role.
    /// </summary>
    /// <param name="role">The text role to style.</param>
    /// <param name="configure">The style configuration callback.</param>
    /// <returns>The current chart.</returns>
    public Chart TextStyle(ChartTextRole role, System.Action<TextStyleOverride> configure) {
        if (configure == null) throw new System.ArgumentNullException(nameof(configure));
        Options.SetTextStyle(role, ChartOptionConfiguration.Configure(Options.GetTextStyle(role), configure, style => style.Clone()));
        return this;
    }

    /// <summary>
    /// Sets the longitude/latitude viewport used to frame equirectangular GeoJSON region maps.
    /// </summary>
    /// <param name="minimumLongitude">The minimum longitude in degrees.</param>
    /// <param name="maximumLongitude">The maximum longitude in degrees.</param>
    /// <param name="minimumLatitude">The minimum latitude in degrees.</param>
    /// <param name="maximumLatitude">The maximum latitude in degrees.</param>
    /// <returns>The current chart.</returns>
    public Chart RegionMapCoordinateBounds(double minimumLongitude, double maximumLongitude, double minimumLatitude, double maximumLatitude) {
        ValidateRegionMapLongitude(minimumLongitude, nameof(minimumLongitude));
        ValidateRegionMapLongitude(maximumLongitude, nameof(maximumLongitude));
        ValidateRegionMapLatitude(minimumLatitude, nameof(minimumLatitude));
        ValidateRegionMapLatitude(maximumLatitude, nameof(maximumLatitude));
        if (maximumLongitude <= minimumLongitude) throw new ArgumentOutOfRangeException(nameof(maximumLongitude), maximumLongitude, "Maximum longitude must be greater than minimum longitude.");
        if (maximumLatitude <= minimumLatitude) throw new ArgumentOutOfRangeException(nameof(maximumLatitude), maximumLatitude, "Maximum latitude must be greater than minimum latitude.");
        Options.Map.RegionBounds = new ChartRect(minimumLongitude, -maximumLatitude, maximumLongitude - minimumLongitude, maximumLatitude - minimumLatitude);
        return this;
    }

    /// <summary>
    /// Adds a non-data geography layer behind region maps.
    /// </summary>
    /// <param name="definition">The map geometry definition.</param>
    /// <param name="fillColor">The fill color for the layer.</param>
    /// <param name="strokeColor">The optional stroke color.</param>
    /// <param name="strokeWidth">The stroke width in rendered pixels.</param>
    /// <returns>The current chart.</returns>
    public Chart AddMapBaseLayer(ChartMapDefinition definition, ChartColor fillColor, ChartColor? strokeColor = null, double strokeWidth = 0.8) {
        Options.Map.BaseLayers.Add(new ChartMapLayer(definition, fillColor, strokeColor, strokeWidth, "map-base-layer"));
        return this;
    }

    /// <summary>
    /// Adds a non-data boundary layer above region maps.
    /// </summary>
    /// <param name="definition">The map geometry definition.</param>
    /// <param name="strokeColor">The boundary stroke color.</param>
    /// <param name="strokeWidth">The stroke width in rendered pixels.</param>
    /// <param name="fillColor">The optional fill color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddMapBoundaryLayer(ChartMapDefinition definition, ChartColor strokeColor, double strokeWidth = 1.4, ChartColor? fillColor = null) {
        Options.Map.OverlayLayers.Add(new ChartMapLayer(definition, fillColor, strokeColor, strokeWidth, "map-boundary-layer"));
        return this;
    }

    /// <summary>
    /// Adds a connector line between two longitude/latitude points on capable map charts.
    /// </summary>
    /// <param name="label">The connector label.</param>
    /// <param name="fromLongitude">The source longitude in degrees.</param>
    /// <param name="fromLatitude">The source latitude in degrees.</param>
    /// <param name="toLongitude">The target longitude in degrees.</param>
    /// <param name="toLatitude">The target latitude in degrees.</param>
    /// <param name="color">An optional connector color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddMapConnector(string label, double fromLongitude, double fromLatitude, double toLongitude, double toLatitude, ChartColor? color = null) {
        Options.Map.Connectors.Add(new ChartMapConnector(label, fromLongitude, fromLatitude, toLongitude, toLatitude, color));
        return this;
    }

    /// <summary>
    /// Adds a connector line through ordered longitude/latitude route points on capable map charts.
    /// </summary>
    /// <param name="label">The connector label.</param>
    /// <param name="routePoints">The ordered route points, including the source and target points.</param>
    /// <param name="color">An optional connector color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddMapConnector(string label, IEnumerable<ChartMapPoint> routePoints, ChartColor? color = null) {
        Options.Map.Connectors.Add(new ChartMapConnector(label, routePoints, color));
        return this;
    }

    /// <summary>
    /// Adds a connector line through ordered longitude/latitude route points on capable map charts.
    /// </summary>
    /// <param name="label">The connector label.</param>
    /// <param name="routePoints">The ordered route points, including the source and target points.</param>
    /// <returns>The current chart.</returns>
    public Chart AddMapConnector(string label, params ChartMapPoint[] routePoints) {
        return AddMapConnector(label, (IEnumerable<ChartMapPoint>)routePoints);
    }

    /// <summary>
    /// Adds a connector line between two existing dotted-map point labels.
    /// </summary>
    /// <remarks>Repeated labels are accepted when every match has exactly the same longitude and latitude.
    /// Use explicit coordinates when a label names different locations.</remarks>
    /// <param name="label">The connector label.</param>
    /// <param name="fromPointLabel">The source dotted-map point label.</param>
    /// <param name="toPointLabel">The target dotted-map point label.</param>
    /// <param name="color">An optional connector color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddMapConnectorBetweenPoints(string label, string fromPointLabel, string toPointLabel, ChartColor? color = null) {
        var from = ResolveDottedMapPoint(fromPointLabel, nameof(fromPointLabel));
        var to = ResolveDottedMapPoint(toPointLabel, nameof(toPointLabel));
        return AddMapConnector(label, from.X, from.Y, to.X, to.Y, color);
    }

    private ChartPoint ResolveDottedMapPoint(string pointLabel, string parameterName) {
        if (string.IsNullOrWhiteSpace(pointLabel)) throw new System.ArgumentException("Map point labels must not be empty.", parameterName);
        ChartPoint? found = null;
        var hasDottedMap = false;
        var trimmed = pointLabel.Trim();
        foreach (var series in Series) {
            if (series.Kind == ChartSeriesKind.DottedMap) {
                hasDottedMap = true;
                foreach (var point in series.MapPoints) if (string.Equals(point.Label, trimmed, System.StringComparison.OrdinalIgnoreCase)) {
                    if (found.HasValue && (found.Value.X != point.Longitude || found.Value.Y != point.Latitude))
                        throw new System.ArgumentException("Map point labels must identify one geographic location across the datasets: " + trimmed + ".", parameterName);
                    found = new ChartPoint(point.Longitude, point.Latitude);
                }
            }
        }
        if (!hasDottedMap) throw new System.InvalidOperationException("Map routes between points require AddDottedMap to be called before the route is added.");
        if (found.HasValue) return found.Value;
        throw new System.ArgumentException("Map point label was not found in the current dotted map: " + trimmed + ".", parameterName);
    }

    private static void ValidateRegionMapLongitude(double value, string parameterName) {
        ChartGuards.Finite(value, parameterName);
        if (value < -180 || value > 180) throw new ArgumentOutOfRangeException(parameterName, value, "Longitude must be between -180 and 180 degrees.");
    }

    private static void ValidateRegionMapLatitude(double value, string parameterName) {
        ChartGuards.Finite(value, parameterName);
        if (value < -90 || value > 90) throw new ArgumentOutOfRangeException(parameterName, value, "Latitude must be between -90 and 90 degrees.");
    }
}
