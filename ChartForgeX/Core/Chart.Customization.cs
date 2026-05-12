using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>
    /// Configures style overrides for a chart text role.
    /// </summary>
    /// <param name="role">The text role to style.</param>
    /// <param name="configure">The style configuration callback.</param>
    /// <returns>The current chart.</returns>
    public Chart WithTextStyle(ChartTextRole role, System.Action<ChartTextStyle> configure) {
        if (configure == null) throw new System.ArgumentNullException(nameof(configure));
        configure(Options.GetTextStyle(role));
        return this;
    }

    /// <summary>
    /// Configures chart title text styling.
    /// </summary>
    public Chart WithTitleStyle(System.Action<ChartTextStyle> configure) => WithTextStyle(ChartTextRole.Title, configure);

    /// <summary>
    /// Configures chart subtitle text styling.
    /// </summary>
    public Chart WithSubtitleStyle(System.Action<ChartTextStyle> configure) => WithTextStyle(ChartTextRole.Subtitle, configure);

    /// <summary>
    /// Configures axis title text styling.
    /// </summary>
    public Chart WithAxisTitleStyle(System.Action<ChartTextStyle> configure) => WithTextStyle(ChartTextRole.AxisTitle, configure);

    /// <summary>
    /// Configures axis tick and category label text styling.
    /// </summary>
    public Chart WithTickLabelStyle(System.Action<ChartTextStyle> configure) => WithTextStyle(ChartTextRole.TickLabel, configure);

    /// <summary>
    /// Configures legend label text styling.
    /// </summary>
    public Chart WithLegendStyle(System.Action<ChartTextStyle> configure) => WithTextStyle(ChartTextRole.Legend, configure);

    /// <summary>
    /// Configures data-label text styling.
    /// </summary>
    public Chart WithDataLabelStyle(System.Action<ChartTextStyle> configure) => WithTextStyle(ChartTextRole.DataLabel, configure);

    /// <summary>
    /// Sets an optional override color for data-label connector lines.
    /// </summary>
    /// <param name="color">The connector color, or null to use the renderer default. Pie and donut callouts use the matching slice color by default.</param>
    /// <returns>The current chart.</returns>
    public Chart WithDataLabelConnectorColor(ChartColor? color) { Options.DataLabelConnectorColor = color; return this; }

    /// <summary>
    /// Sets an override color for data-label connector lines from hex notation.
    /// </summary>
    /// <param name="hex">The connector color in #RGB, #RGBA, #RRGGBB, or #RRGGBBAA notation.</param>
    /// <returns>The current chart.</returns>
    public Chart WithDataLabelConnectorColor(string hex) { Options.DataLabelConnectorColor = ChartColor.FromHex(hex); return this; }

    /// <summary>
    /// Sets the opacity used by data-label connector lines.
    /// </summary>
    /// <param name="opacity">The opacity from zero to one.</param>
    /// <returns>The current chart.</returns>
    public Chart WithDataLabelConnectorOpacity(double opacity) { Options.DataLabelConnectorOpacity = opacity; return this; }

    /// <summary>
    /// Sets the stroke width used by data-label connector lines.
    /// </summary>
    /// <param name="width">The connector stroke width in pixels.</param>
    /// <returns>The current chart.</returns>
    public Chart WithDataLabelConnectorStrokeWidth(double width) { Options.DataLabelConnectorStrokeWidth = width; return this; }

    /// <summary>
    /// Sets the connector line shape used by outside and side data labels.
    /// </summary>
    /// <param name="style">The connector style.</param>
    /// <returns>The current chart.</returns>
    public Chart WithDataLabelConnectorStyle(ChartDataLabelConnectorStyle style) { Options.DataLabelConnectorStyle = style; return this; }

    /// <summary>
    /// Sets where the legend is placed relative to the plot area.
    /// </summary>
    /// <param name="position">The legend position.</param>
    /// <returns>The current chart.</returns>
    public Chart WithLegendPosition(ChartLegendPosition position) { Options.LegendPosition = position; return this; }

    /// <summary>
    /// Sets whether heatmap scale legends should be rendered.
    /// </summary>
    /// <param name="visible">True to render heatmap scale legends; otherwise false.</param>
    /// <returns>The current chart.</returns>
    public Chart WithHeatmapScaleLegend(bool visible = true) { Options.ShowHeatmapScale = visible; return this; }

    /// <summary>
    /// Sets whether heatmap column labels should be rendered.
    /// </summary>
    /// <param name="visible">True to render heatmap column labels; otherwise false.</param>
    /// <returns>The current chart.</returns>
    public Chart WithHeatmapColumnLabels(bool visible = true) { Options.ShowHeatmapColumnLabels = visible; return this; }

    /// <summary>
    /// Sets the heatmap cell gap in pixels. Pass null to use the automatic dashboard-safe gap.
    /// </summary>
    /// <param name="gap">The cell gap in pixels, or null for automatic sizing.</param>
    /// <returns>The current chart.</returns>
    public Chart WithHeatmapCellGap(double? gap) { Options.HeatmapCellGap = gap; return this; }

    /// <summary>
    /// Sets the heatmap cell corner radius in pixels. Pass null to use the automatic radius.
    /// </summary>
    /// <param name="radius">The cell corner radius in pixels, or null for automatic sizing.</param>
    /// <returns>The current chart.</returns>
    public Chart WithHeatmapCellRadius(double? radius) { Options.HeatmapCellRadius = radius; return this; }

    /// <summary>
    /// Sets when heatmap cell values should render as text.
    /// </summary>
    /// <param name="mode">The heatmap value text mode.</param>
    /// <returns>The current chart.</returns>
    public Chart WithHeatmapValueTextMode(ChartHeatmapValueTextMode mode) { Options.HeatmapValueTextMode = mode; return this; }

    /// <summary>
    /// Sets whether region labels should be rendered on map charts.
    /// </summary>
    /// <param name="visible">True to render map region labels; otherwise false.</param>
    /// <returns>The current chart.</returns>
    public Chart WithMapLabels(bool visible = true) { Options.ShowMapLabels = visible; return this; }

    /// <summary>
    /// Sets whether map scale legends should be rendered.
    /// </summary>
    /// <param name="visible">True to render map scale legends; otherwise false.</param>
    /// <returns>The current chart.</returns>
    public Chart WithMapScaleLegend(bool visible = true) { Options.ShowMapScaleLegend = visible; return this; }

    /// <summary>
    /// Sets where map scale legends should be rendered.
    /// </summary>
    /// <param name="position">The map scale legend position.</param>
    /// <returns>The current chart.</returns>
    public Chart WithMapScaleLegendPosition(ChartMapScaleLegendPosition position) { Options.MapScaleLegendPosition = position; return this; }

    /// <summary>
    /// Sets whether map charts render their own background surface.
    /// </summary>
    /// <param name="visible">True to render the map surface; otherwise false.</param>
    /// <returns>The current chart.</returns>
    public Chart WithMapSurface(bool visible = true) { Options.ShowMapSurface = visible; return this; }

    /// <summary>
    /// Sets the stroke rendered around data regions on map charts.
    /// </summary>
    /// <param name="color">The stroke color. Pass null to use the theme card background.</param>
    /// <param name="width">The stroke width in rendered pixels.</param>
    /// <returns>The current chart.</returns>
    public Chart WithMapRegionStroke(ChartColor? color, double width = 1.1) {
        ChartGuards.Finite(width, nameof(width));
        if (width < 0) throw new ArgumentOutOfRangeException(nameof(width), width, "Map region stroke width must not be negative.");
        Options.MapRegionStrokeColor = color;
        Options.MapRegionStrokeWidth = width;
        return this;
    }

    /// <summary>
    /// Sets the source-coordinate viewport used to frame region maps.
    /// </summary>
    /// <param name="bounds">The source coordinate bounds to render.</param>
    /// <returns>The current chart.</returns>
    public Chart WithRegionMapBounds(ChartRect bounds) {
        ChartGuards.Finite(bounds.Left, nameof(bounds));
        ChartGuards.Finite(bounds.Top, nameof(bounds));
        ChartGuards.Finite(bounds.Width, nameof(bounds));
        ChartGuards.Finite(bounds.Height, nameof(bounds));
        if (bounds.Width <= 0 || bounds.Height <= 0) throw new ArgumentOutOfRangeException(nameof(bounds), "Region map bounds must have positive width and height.");
        Options.RegionMapBounds = bounds;
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
    public Chart WithRegionMapCoordinateBounds(double minimumLongitude, double maximumLongitude, double minimumLatitude, double maximumLatitude) {
        ValidateRegionMapLongitude(minimumLongitude, nameof(minimumLongitude));
        ValidateRegionMapLongitude(maximumLongitude, nameof(maximumLongitude));
        ValidateRegionMapLatitude(minimumLatitude, nameof(minimumLatitude));
        ValidateRegionMapLatitude(maximumLatitude, nameof(maximumLatitude));
        if (maximumLongitude <= minimumLongitude) throw new ArgumentOutOfRangeException(nameof(maximumLongitude), maximumLongitude, "Maximum longitude must be greater than minimum longitude.");
        if (maximumLatitude <= minimumLatitude) throw new ArgumentOutOfRangeException(nameof(maximumLatitude), maximumLatitude, "Maximum latitude must be greater than minimum latitude.");
        Options.RegionMapBounds = new ChartRect(minimumLongitude, -maximumLatitude, maximumLongitude - minimumLongitude, maximumLatitude - minimumLatitude);
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
        Options.MapBaseLayers.Add(new ChartMapLayer(definition, fillColor, strokeColor, strokeWidth, "map-base-layer"));
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
        Options.MapOverlayLayers.Add(new ChartMapLayer(definition, fillColor, strokeColor, strokeWidth, "map-boundary-layer"));
        return this;
    }

    /// <summary>
    /// Sets the longitude/latitude viewport used by dotted map charts.
    /// </summary>
    /// <param name="viewport">The map viewport to render.</param>
    /// <returns>The current chart.</returns>
    public Chart WithMapViewport(ChartMapViewport viewport) { Options.MapViewport = viewport; return this; }

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
        Options.MapConnectors.Add(new ChartMapConnector(label, fromLongitude, fromLatitude, toLongitude, toLatitude, color));
        return this;
    }

    /// <summary>
    /// Adds a route line between two longitude/latitude points on capable map charts.
    /// </summary>
    /// <param name="label">The route label.</param>
    /// <param name="fromLongitude">The source longitude in degrees.</param>
    /// <param name="fromLatitude">The source latitude in degrees.</param>
    /// <param name="toLongitude">The target longitude in degrees.</param>
    /// <param name="toLatitude">The target latitude in degrees.</param>
    /// <param name="color">An optional route color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddMapRoute(string label, double fromLongitude, double fromLatitude, double toLongitude, double toLatitude, ChartColor? color = null) {
        return AddMapConnector(label, fromLongitude, fromLatitude, toLongitude, toLatitude, color);
    }

    /// <summary>
    /// Adds a connector line between two existing dotted-map point labels.
    /// </summary>
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

    /// <summary>
    /// Adds a route line between two existing dotted-map point labels.
    /// </summary>
    /// <param name="label">The route label.</param>
    /// <param name="fromPointLabel">The source dotted-map point label.</param>
    /// <param name="toPointLabel">The target dotted-map point label.</param>
    /// <param name="color">An optional route color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddMapRouteBetweenPoints(string label, string fromPointLabel, string toPointLabel, ChartColor? color = null) {
        return AddMapConnectorBetweenPoints(label, fromPointLabel, toPointLabel, color);
    }

    private ChartPoint ResolveDottedMapPoint(string pointLabel, string parameterName) {
        if (string.IsNullOrWhiteSpace(pointLabel)) throw new System.ArgumentException("Map point labels must not be empty.", parameterName);
        ChartSeries? dottedMap = null;
        foreach (var series in Series) {
            if (series.Kind == ChartSeriesKind.DottedMap) {
                dottedMap = series;
                break;
            }
        }

        if (dottedMap == null) throw new System.InvalidOperationException("Map routes between points require AddDottedMap to be called before the route is added.");
        var trimmed = pointLabel.Trim();
        var count = System.Math.Min(dottedMap.Points.Count, Options.XAxisLabels.Count);
        for (var i = 0; i < count; i++) {
            if (string.Equals(Options.XAxisLabels[i].Text, trimmed, System.StringComparison.OrdinalIgnoreCase)) return dottedMap.Points[i];
        }

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

    /// <summary>
    /// Sets the built-in pictorial symbol shape and clears any custom SVG path.
    /// </summary>
    /// <param name="shape">The built-in shape used by SVG and PNG output.</param>
    /// <returns>The current chart.</returns>
    public Chart WithPictorialShape(ChartPictorialShape shape) {
        Options.PictorialShape = shape;
        Options.PictorialSvgPathData = null;
        return this;
    }

    /// <summary>
    /// Sets the number of repeated symbols used per pictorial chart row.
    /// </summary>
    /// <param name="columns">The symbol count from one to one hundred.</param>
    /// <returns>The current chart.</returns>
    public Chart WithPictorialColumns(int columns) { Options.PictorialColumns = columns; return this; }

    /// <summary>
    /// Sets the optional scale maximum used by pictorial chart rows.
    /// </summary>
    /// <param name="maximum">The maximum value represented by a fully filled row. Pass null to scale to the largest item.</param>
    /// <returns>The current chart.</returns>
    public Chart WithPictorialMaximum(double? maximum) { Options.PictorialMaximum = maximum; return this; }

    /// <summary>
    /// Sets the data value represented by one full pictorial symbol.
    /// </summary>
    /// <param name="valuePerSymbol">The value represented by one symbol. Pass null to use proportional scaling.</param>
    /// <returns>The current chart.</returns>
    public Chart WithPictorialValuePerSymbol(double? valuePerSymbol) { Options.PictorialValuePerSymbol = valuePerSymbol; return this; }

    /// <summary>
    /// Sets whether numeric value labels are rendered for pictorial rows.
    /// </summary>
    /// <param name="visible">True to render value labels; otherwise false.</param>
    /// <returns>The current chart.</returns>
    public Chart WithPictorialValues(bool visible = true) { Options.ShowPictorialValues = visible; return this; }

    /// <summary>
    /// Sets the relative pictorial symbol size inside each symbol slot.
    /// </summary>
    /// <param name="scale">The symbol scale from 0.4 to 1.4.</param>
    /// <returns>The current chart.</returns>
    public Chart WithPictorialSymbolScale(double scale) { Options.PictorialSymbolScale = scale; return this; }

    /// <summary>
    /// Sets the opacity used by unfilled pictorial symbols.
    /// </summary>
    /// <param name="opacity">The opacity from zero to one.</param>
    /// <returns>The current chart.</returns>
    public Chart WithPictorialEmptyOpacity(double opacity) { Options.PictorialEmptyOpacity = opacity; return this; }

    /// <summary>
    /// Sets the shared maximum for progress-bar chart rows.
    /// </summary>
    /// <param name="maximum">The maximum progress value.</param>
    /// <returns>The current chart.</returns>
    public Chart WithProgressMaximum(double maximum) { Options.ProgressMaximum = maximum; return this; }

    /// <summary>
    /// Sets whether progress-bar rows should display value labels.
    /// </summary>
    /// <param name="visible">True to show value labels; otherwise false.</param>
    /// <returns>The current chart.</returns>
    public Chart WithProgressValues(bool visible = true) { Options.ShowProgressValues = visible; return this; }

    /// <summary>
    /// Sets whether progress-bar rows should render slider handles.
    /// </summary>
    /// <param name="visible">True to render handles; otherwise false.</param>
    /// <returns>The current chart.</returns>
    public Chart WithProgressHandles(bool visible = true) { Options.ShowProgressHandles = visible; return this; }

    /// <summary>
    /// Sets the progress-bar thickness as a ratio of each row height.
    /// </summary>
    /// <param name="ratio">The thickness ratio from 0.16 to 0.72.</param>
    /// <returns>The current chart.</returns>
    public Chart WithProgressBarThickness(double ratio) { Options.ProgressBarThicknessRatio = ratio; return this; }

    /// <summary>
    /// Sets the opacity used by progress-bar tracks.
    /// </summary>
    /// <param name="opacity">The opacity from zero to one.</param>
    /// <returns>The current chart.</returns>
    public Chart WithProgressTrackOpacity(double opacity) { Options.ProgressTrackOpacity = opacity; return this; }

    /// <summary>
    /// Sets whether donut charts should display center total and series labels.
    /// </summary>
    /// <param name="visible">True to show center labels; otherwise false.</param>
    /// <returns>The current chart.</returns>
    public Chart WithDonutCenterLabel(bool visible = true) { Options.ShowDonutCenterLabel = visible; return this; }

    /// <summary>
    /// Sets custom center text for donut charts.
    /// </summary>
    /// <param name="value">The primary center text. Pass null or whitespace to use the formatted total.</param>
    /// <param name="label">The secondary center text. Pass null or whitespace to use the series name.</param>
    /// <returns>The current chart.</returns>
    public Chart WithDonutCenterText(string? value, string? label = null) {
        Options.DonutCenterValue = value;
        Options.DonutCenterLabel = label;
        return this;
    }

    /// <summary>
    /// Sets the donut hole radius as a ratio of the outer radius.
    /// </summary>
    /// <param name="ratio">The inner radius ratio from 0.35 to 0.82.</param>
    /// <returns>The current chart.</returns>
    public Chart WithDonutInnerRadiusRatio(double ratio) { Options.DonutInnerRadiusRatio = ratio; return this; }

    /// <summary>
    /// Sets the text rendered for pie and donut slice data labels.
    /// </summary>
    /// <param name="content">The slice label content mode.</param>
    /// <returns>The current chart.</returns>
    public Chart WithPieSliceLabelContent(ChartPieSliceLabelContent content) { Options.PieSliceLabelContent = content; return this; }

    /// <summary>
    /// Sets a custom formatter for pie and donut slice data labels.
    /// </summary>
    /// <param name="formatter">The slice label formatter. Pass null to use <see cref="ChartOptions.PieSliceLabelContent"/>.</param>
    /// <returns>The current chart.</returns>
    public Chart WithPieSliceLabelFormatter(System.Func<ChartPieSliceLabelContext, string?>? formatter) { Options.PieSliceLabelFormatter = formatter; return this; }

    /// <summary>
    /// Sets the side-lane distance for outside pie and donut labels as a ratio of the outer radius.
    /// </summary>
    /// <param name="ratio">The distance ratio from 0.9 to 1.8.</param>
    /// <returns>The current chart.</returns>
    public Chart WithPieOutsideLabelDistance(double ratio) { Options.PieOutsideLabelDistanceRatio = ratio; return this; }

    /// <summary>
    /// Sets whether radial-bar charts should display center average and series labels.
    /// </summary>
    /// <param name="visible">True to show center labels; otherwise false.</param>
    /// <returns>The current chart.</returns>
    public Chart WithRadialBarCenterLabel(bool visible = true) { Options.ShowRadialBarCenterLabel = visible; return this; }

    /// <summary>
    /// Sets the relative radius used by circle charts.
    /// </summary>
    /// <param name="scale">The radius scale from 0.65 to 1.35.</param>
    /// <returns>The current chart.</returns>
    public Chart WithCircleRadiusScale(double scale) { Options.CircleRadiusScale = scale; return this; }

    /// <summary>
    /// Sets the relative stroke thickness used by circle charts.
    /// </summary>
    /// <param name="scale">The stroke scale from 0.55 to 1.8.</param>
    /// <returns>The current chart.</returns>
    public Chart WithCircleStrokeScale(double scale) { Options.CircleStrokeScale = scale; return this; }

    /// <summary>
    /// Sets the relative outer radius used by radial-bar charts.
    /// </summary>
    /// <param name="scale">The radius scale from 0.65 to 1.35.</param>
    /// <returns>The current chart.</returns>
    public Chart WithRadialBarRadiusScale(double scale) { Options.RadialBarRadiusScale = scale; return this; }

    /// <summary>
    /// Sets the relative stroke thickness used by radial-bar charts.
    /// </summary>
    /// <param name="scale">The stroke scale from 0.55 to 1.8.</param>
    /// <returns>The current chart.</returns>
    public Chart WithRadialBarStrokeScale(double scale) { Options.RadialBarStrokeScale = scale; return this; }

    /// <summary>
    /// Sets whether circle charts should display status marker and status labels.
    /// </summary>
    /// <param name="visible">True to show status labels; otherwise false.</param>
    /// <returns>The current chart.</returns>
    public Chart WithCircleStatusLabel(bool visible = true) { Options.ShowCircleStatusLabel = visible; return this; }

    /// <summary>
    /// Sets custom SVG path data for pictorial symbols using a 24 by 24 viewBox.
    /// </summary>
    /// <param name="pathData">The SVG path data.</param>
    /// <param name="pngFallbackShape">The built-in shape used by PNG output.</param>
    /// <returns>The current chart.</returns>
    public Chart WithPictorialSvgPath(string pathData, ChartPictorialShape pngFallbackShape = ChartPictorialShape.Circle) =>
        WithPictorialSvgPath(pathData, new ChartRect(0, 0, 24, 24), pngFallbackShape);

    /// <summary>
    /// Sets custom SVG path data for pictorial symbols.
    /// </summary>
    /// <param name="pathData">The SVG path data.</param>
    /// <param name="viewBox">The SVG path viewBox used to scale the path into each symbol slot.</param>
    /// <param name="pngFallbackShape">The built-in shape used by PNG output.</param>
    /// <returns>The current chart.</returns>
    public Chart WithPictorialSvgPath(string pathData, ChartRect viewBox, ChartPictorialShape pngFallbackShape = ChartPictorialShape.Circle) {
        Options.SetPictorialSvgPath(pathData, viewBox, pngFallbackShape);
        return this;
    }

    /// <summary>
    /// Sets the minimum and maximum font sizes used by word cloud terms.
    /// </summary>
    /// <param name="minimum">The smallest term font size.</param>
    /// <param name="maximum">The largest term font size.</param>
    /// <returns>The current chart.</returns>
    public Chart WithWordCloudFontRange(double minimum, double maximum) {
        Options.SetWordCloudFontRange(minimum, maximum);
        return this;
    }

    /// <summary>
    /// Sets the deterministic rotation angles used by word cloud terms.
    /// </summary>
    /// <param name="angles">One or more angles in degrees. Terms cycle through the supplied values.</param>
    /// <returns>The current chart.</returns>
    public Chart WithWordCloudAngles(params double[] angles) {
        Options.WordCloudAngles = angles;
        return this;
    }

    /// <summary>
    /// Sets the optional maximum number of positive-weight terms rendered in a word cloud.
    /// </summary>
    /// <param name="maximumTerms">The maximum term count. Pass null to render every positive-weight term that fits.</param>
    /// <returns>The current chart.</returns>
    public Chart WithWordCloudMaximumTerms(int? maximumTerms) {
        Options.WordCloudMaximumTerms = maximumTerms;
        return this;
    }

    /// <summary>
    /// Sets the word cloud packing density from 0.5 to 2.0.
    /// </summary>
    /// <param name="density">Lower values keep more breathing room; higher values pack more terms into the plot area.</param>
    /// <returns>The current chart.</returns>
    public Chart WithWordCloudDensity(double density) {
        Options.WordCloudDensity = density;
        return this;
    }
}
