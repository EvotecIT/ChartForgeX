---
description: "Every ChartForgeX 4.x method and property that 5.0 renamed or moved, and what to write instead."
---

# Fluent API map (4.x to 5.0)

ChartForgeX 5.0 generates fluent methods named after the model properties. This page lists every 4.x member that was renamed, regrouped or replaced, by type. Read [the migration notes](migration.md#fluent-methods-named-after-properties) first; they explain the groups and the behaviour that changed. In the 5.0 column, a lambda such as `.Legend(l => l.Position(position))` configures a settings object, and `(...)` stands for the original arguments of a method that kept its parameters.

## ActivityTimelineBlock

| 4.x | 5.0 |
| --- | --- |
| `WithEventSurfaces(Boolean)` | `.ShowEventSurfaces(enabled)` |

## BlockLayoutBlock

| 4.x | 5.0 |
| --- | --- |
| `WithColumns(Int32)` | `.Columns(columns)` |
| `WithEdges(Boolean)` | `.ShowEdges(visible)` |

## Chart

| 4.x | 5.0 |
| --- | --- |
| `AsDecorative` | `.Accessibility(a => a.Decorative(decorative))` |
| `ConfigureAccessibility` | `.Accessibility(configure)` |
| `ConfigureAxisTitleStyle` | `.AxisTitleStyle(configure)` |
| `ConfigureBarVisualStyle` | `.Bars(bars => bars.VisualStyle(configure))` |
| `ConfigureBoxPlot` | `.BoxPlot(configure)` |
| `ConfigureBubble` | `.Bubble(configure)` |
| `ConfigureBullet` | `.Bullet(configure)` |
| `ConfigureCalendar` | `.Calendar(configure)` |
| `ConfigureChord` | `.Chord(configure)` |
| `ConfigureDataLabelStyle` | `.DataLabelStyle(configure)` |
| `ConfigureFunnel` | `.Funnel(configure)` |
| `ConfigureGauge` | `.Gauge(configure)` |
| `ConfigureGridStyle` | `.GridLineStyle(configure)` |
| `ConfigureHeatmapColorDimension` | `.Heatmap(heatmap => heatmap.ColorDimension(configure))` |
| `ConfigureLabels` | `.Labels(configure)` |
| `ConfigureLegendStyle` | `.LegendStyle(configure)` |
| `ConfigureLineVisualStyle` | `.Lines(lines => lines.VisualStyle(configure))` |
| `ConfigureMapColorDimension` | `.Map(map => map.ColorDimension(configure))` |
| `ConfigurePyramid` | `.Pyramid(configure)` |
| `ConfigureRangeBar` | `.RangeBar(configure)` |
| `ConfigureSankey` | `.Sankey(configure)` |
| `ConfigureSecondaryXAxis` | `.SecondaryXAxis(configure)` |
| `ConfigureSecondaryYAxis` | `.SecondaryYAxis(configure)` |
| `ConfigureSubtitleStyle` | `.SubtitleStyle(configure)` |
| `ConfigureSunburst` | `.Sunburst(configure)` |
| `ConfigureTextStyle` | `.TextStyle(...)` |
| `ConfigureTickLabelStyle` | `.TickLabelStyle(configure)` |
| `ConfigureTitleStyle` | `.TitleStyle(configure)` |
| `ConfigureTreemap` | `.Treemap(configure)` |
| `ConfigureWaterfall` | `.Waterfall(configure)` |
| `ConfigureXAxis` | `.XAxis(configure)` |
| `ConfigureYAxis` | `.YAxis(configure)` |
| `SecondaryXAxisTitle (property)` | `Options.SecondaryXAxis.Title` |
| `SecondaryYAxisTitle (property)` | `Options.SecondaryYAxis.Title` |
| `WithAutomaticRadialBands` | `.RadialBandAllocation(allocation)` |
| `WithAutomaticSecondaryXAxisBounds` | `.SecondaryXAxis(s => s.Min(null)).SecondaryXAxis(s => s.Max(null))` |
| `WithAutomaticSecondaryYAxisBounds` | `.SecondaryYAxis(s => s.Min(null)).SecondaryYAxis(s => s.Max(null))` |
| `WithAutomaticXAxisBounds` | `.XAxis(x => x.Min(null)).XAxis(x => x.Max(null))` |
| `WithAutomaticYAxisBounds` | `.YAxis(y => y.Min(null)).YAxis(y => y.Max(null))` |
| `WithAxes` | `.Axes(...)` |
| `WithAxisLines` | `.ShowAxisLines(visible)` |
| `WithBarMode` | `.Bars(b => b.Mode(mode))` |
| `WithBarStyle` | `.Bars(b => b.Style(style))` |
| `WithBarVisualStyle` | `.Bars(b => b.VisualStyle(style))` |
| `WithCard` | `.ShowCard(visible)` |
| `WithCircleRadiusScale(Double)` | `.Circle(c => c.RadiusScale(scale))` |
| `WithCircleStatusLabel` | `.Circle(c => c.ShowStatusLabel(visible))` |
| `WithCircleStrokeScale(Double)` | `.Circle(c => c.StrokeScale(scale))` |
| `WithCulture` | `.Culture(...)` |
| `WithDashboardBarPanelStyle` | `.DashboardBarPanelStyle(...)` |
| `WithDashboardBarStyle` | `.DashboardBarStyle(...)` |
| `WithDashboardCartesianStyle` | `.DashboardCartesianStyle(...)` |
| `WithDashboardPanelStyle` | `.DashboardPanelStyle(...)` |
| `WithDashboardStackedRowStyle` | `.DashboardStackedRowStyle(...)` |
| `WithDashboardTrendFocus` | `.DashboardTrendFocus(...)` |
| `WithDashboardTrendPanelStyle` | `.DashboardTrendPanelStyle(...)` |
| `WithDataLabelConnectorColor(ChartColor?)` | `.DataLabels(d => d.ConnectorColor(color))` |
| `WithDataLabelConnectorColor(string)` | `.DataLabels(d => d.ConnectorColor(ChartColor.FromHex(hex)))` |
| `WithDataLabelConnectorOpacity(Double)` | `.DataLabels(d => d.ConnectorOpacity(opacity))` |
| `WithDataLabelConnectorStrokeWidth(Double)` | `.DataLabels(d => d.ConnectorStrokeWidth(width))` |
| `WithDataLabelConnectorStyle(ChartDataLabelConnectorStyle)` | `.DataLabels(d => d.ConnectorStyle(style))` |
| `WithDataLabelPlacement` | `.DataLabels(d => d.Placement(placement))` |
| `WithDataLabels` | `.DataLabels(visible)` |
| `WithDesignTokens` | `.DesignTokens(...)` |
| `WithDonutCenterLabel` | `.Pie(p => p.ShowCenterLabel(visible))` |
| `WithDonutCenterText` | `.Pie(p => p.CenterValue(value)).Pie(p => p.CenterLabel(label))` |
| `WithDonutInnerRadiusRatio` | `.Pie(p => p.InnerRadiusRatio(ratio))` |
| `WithFocusedXAxisCategory` | `.FocusXAxisCategory(...)` |
| `WithFontFamily` | `.FontFamily(...)` |
| `WithGanttLaneNow` | `.GanttLaneNow(...)` |
| `WithGanttOpenEndStyle(ChartGanttOpenEndStyle)` | `.Gantt(g => g.OpenEndStyle(style))` |
| `WithGanttTickInterval(ChartTimeTickInterval)` | `.Gantt(g => g.TickInterval(interval))` |
| `WithGanttToday(DateTime?)` | `.GanttToday(...)` |
| `WithGanttToday(double?)` | `.Gantt(g => g.Today(value))` |
| `WithGrid` | `.ShowGrid(visible)` |
| `WithGridStyle` | `.GridLineStyle(style)` |
| `WithGroupedBars` | `.Bars(b => b.Grouped())` |
| `WithHeader` | `.ShowHeader(visible)` |
| `WithHeatmapCategoryLegend` | `.Heatmap(h => h.ShowCategoryLegend(visible))` |
| `WithHeatmapCellGap(Double?)` | `.Heatmap(h => h.CellGap(gap))` |
| `WithHeatmapCellRadius(Double?)` | `.Heatmap(h => h.CellRadius(radius))` |
| `WithHeatmapColorScale(ChartColorScale)` | `.Heatmap(h => h.ColorScale(scale))` |
| `WithHeatmapColumnLabels` | `.Heatmap(h => h.ShowColumnLabels(visible))` |
| `WithHeatmapColumnWindow(ChartHeatmapColumnWindow)` | `.Heatmap(h => h.ColumnWindow(window))` |
| `WithHeatmapNaturalHeight(Boolean)` | `.Heatmap(h => h.NaturalHeight(enabled))` |
| `WithHeatmapScale(ChartHeatmapScale)` | `.Heatmap(h => h.Scale(scale))` |
| `WithHeatmapScaleStepLabels` | `.Heatmap(h => h.ShowScaleStepLabels(visible))` |
| `WithHeatmapValueTextMode(ChartHeatmapValueTextMode)` | `.Heatmap(h => h.ValueTextMode(mode))` |
| `WithHighlightedXAxisLabel` | `.HighlightXAxisLabel(...)` |
| `WithHighlightedXAxisRange` | `.HighlightXAxisRange(...)` |
| `WithHostFrame` | `.HostOwnsFrame(hostOwnsFrame)` |
| `WithLegend` | `.Legend(visible)` |
| `WithLegendBudget` | `.Legend(l => l.MaxHeightFraction(maximumHeightFraction)).Legend(l => l.MaxRows(maximumRows))` |
| `WithLegendFormatter` | `.Legend(l => l.LabelBy(formatter))` |
| `WithLegendKey` | `.Legend(l => l.KeyShape(shape)).Legend(l => l.KeySize(size))` |
| `WithLegendPosition` | `.Legend(l => l.Position(position))` |
| `WithLineMarkers` | `.Lines(l => l.MarkerMode(mode))` |
| `WithLineVisualStyle` | `.Lines(l => l.VisualStyle(style))` |
| `WithLuminousLineStyle` | `.Lines(l => l.LuminousStyle())` |
| `WithMapColorScale(ChartColor,ChartColor)` | `.Map(m => m.ColorScale(ChartColorScale.Sequential(lowColor, highColor)))` |
| `WithMapColorScale(ChartColor,ChartColor,ChartColor,double?)` | `.Map(m => m.ColorScale(ChartColorScale.Diverging(lowColor, midpointColor, highColor, midpointValue)))` |
| `WithMapColorScale(ChartColorScale?)` | `.Map(m => m.ColorScale(scale))` |
| `WithMapLabels` | `.Map(m => m.ShowLabels(visible))` |
| `WithMapRegionStroke` | `.Map(m => m.RegionStrokeColor(color)).Map(m => m.RegionStrokeWidth(width))` |
| `WithMapScaleLegendPosition(ChartMapScaleLegendPosition)` | `.Map(m => m.ScaleLegendPosition(position))` |
| `WithMapSurface` | `.Map(m => m.ShowSurface(visible))` |
| `WithMapViewport(ChartMapViewport)` | `.Map(m => m.Viewport(viewport))` |
| `WithMarkBackdrop` | `.MarkBackdrop(backdrop)` |
| `WithMaximumBarWidth` | `.Bars(b => b.MaxWidth(pixels))` |
| `WithOverlay` | `.Overlay(...)` |
| `WithPadding` | `.Padding(...)` |
| `WithPictorialArtwork(ChartPictorialArtwork)` | `.Pictorial(p => p.Artwork(artwork))` |
| `WithPictorialColumns(Int32)` | `.Pictorial(p => p.Columns(columns))` |
| `WithPictorialEmptyOpacity(Double)` | `.Pictorial(p => p.EmptyOpacity(opacity))` |
| `WithPictorialFillDirection(ChartPictorialFillDirection)` | `.Pictorial(p => p.FillDirection(direction))` |
| `WithPictorialMaximum(Double?)` | `.Pictorial(p => p.Max(maximum))` |
| `WithPictorialShape(ChartPictorialShape)` | `.Pictorial(p => p.Shape(shape)).Pictorial(p => p.SvgPathData(null))` |
| `WithPictorialSvgPath(string,ChartPictorialShape)` | `.Pictorial(p => p.SvgPath(pathData, pngFallbackShape))` |
| `WithPictorialSvgPath(string,ChartRect,ChartPictorialShape)` | `.Pictorial(p => p.SvgPath(pathData, viewBox, pngFallbackShape))` |
| `WithPictorialSymbolScale(Double)` | `.Pictorial(p => p.SymbolScale(scale))` |
| `WithPictorialValuePerSymbol(Double?)` | `.Pictorial(p => p.ValuePerSymbol(valuePerSymbol))` |
| `WithPictorialValues` | `.Pictorial(p => p.ShowValues(visible))` |
| `WithPieOutsideLabelDistance` | `.Pie(p => p.OutsideLabelDistanceRatio(ratio))` |
| `WithPieSliceLabelContent(ChartPieSliceLabelContent)` | `.Pie(p => p.SliceLabelContent(content))` |
| `WithPieSliceLabelFormatter(String})` | `.Pie(p => p.SliceLabelBy(formatter))` |
| `WithPlainLineStyle` | `.Lines(l => l.PlainStyle())` |
| `WithPlotBackground` | `.ShowPlotBackground(visible)` |
| `WithPlotClipping` | `.ClipMarksToPlot(enabled)` |
| `WithPngFont(string?,int?,string?)` | `.Png(p => p.FontPath(fontPath)).Png(p => p.FontCollectionIndex(collectionIndex)).Png(p => p.FontFaceName(faceName))` |
| `WithPngFont(string?,string?)` | `.Png(p => p.FontPath(fontPath)).Png(p => p.FontFaceName(faceName))` |
| `WithPngOutputScale(ChartPngOutputScale)` | `.Png(p => p.OutputScale((int)scale))` |
| `WithPngOutputScale(Int32)` | `.Png(p => p.OutputScale(scale))` |
| `WithPngSupersampling(Int32)` | `.Png(p => p.SupersamplingScale(scale))` |
| `WithPointLegend` | `.Legend(l => l.ShowPoints(visible))` |
| `WithPolarGridShape` | `.Polar(p => p.GridShape(shape))` |
| `WithProgressBarThickness` | `.Progress(p => p.BarThicknessRatio(ratio))` |
| `WithProgressHandles` | `.Progress(p => p.ShowHandles(visible))` |
| `WithProgressMaximum(Double)` | `.Progress(p => p.Max(maximum))` |
| `WithProgressRingCenterLabel` | `.Progress(p => p.ShowRingCenterLabel(visible))` |
| `WithProgressTrackOpacity(Double)` | `.Progress(p => p.TrackOpacity(opacity))` |
| `WithProgressValues` | `.Progress(p => p.ShowValues(visible))` |
| `WithRadialGeometry` | `.RadialGeometry(geometry)` |
| `WithRadialProgressRadiusScale` | `.Progress(p => p.RadialRadiusScale(scale))` |
| `WithRadialProgressStrokeScale` | `.Progress(p => p.RadialStrokeScale(scale))` |
| `WithRegionMapBounds` | `.Map(m => m.RegionBounds(bounds))` |
| `WithRegionMapCoordinateBounds` | `.RegionMapCoordinateBounds(...)` |
| `WithSecondaryXAxis` | `.SecondaryXAxis(s => s.Title(title)).SecondaryXAxis(s => s.LabelBy(formatter))` |
| `WithSecondaryXAxisBounds` | `.SecondaryXAxis(s => s.Bounds(minimum, maximum))` |
| `WithSecondaryXAxisScale` | `.SecondaryXAxis(s => s.Scale(scale))` |
| `WithSecondaryYAxis` | `.SecondaryYAxis(s => s.Title(title)).SecondaryYAxis(s => s.LabelBy(formatter))` |
| `WithSecondaryYAxisBounds` | `.SecondaryYAxis(s => s.Bounds(minimum, maximum))` |
| `WithSecondaryYAxisIntegerTicks` | `.SecondaryYAxis(s => s.IntegerTicks(enabled))` |
| `WithSecondaryYAxisScale` | `.SecondaryYAxis(s => s.Scale(scale))` |
| `WithSeriesColorAssignment` | `.SeriesColorAssignment(assignment)` |
| `WithSize` | `.Size(...)` |
| `WithSparkline` | `.Sparkline(...)` |
| `WithStackTotals` | `.Bars(b => b.ShowTotals(visible))` |
| `WithStackedBars` | `.Bars(b => b.Stacked())` |
| `WithStackedHorizontalBars` | `.Bars(b => b.Stacked())` |
| `WithStateCategories` | `.StateCategories(...)` |
| `WithStateColorsPinnedInForcedColors` | `.StateTimeline(s => s.PinColorsInForcedColors(pin))` |
| `WithSubtitle` | `.Subtitle(subtitle)` |
| `WithSvgColorVariables` | `.SvgColorVariables(variables)` |
| `WithTheme` | `.Theme(theme).ThemeMode(mode)` |
| `WithThemeMode(ThemeMode)` | `.ThemeMode(mode)` |
| `WithTickCount` | `.TickCount(count)` |
| `WithTitle` | `.Title(title)` |
| `WithTransparentBackground` | `.TransparentBackground(transparent)` |
| `WithValueFormat` | `.ValueFormat(format)` |
| `WithValueFormatter` | `.FormatValuesBy(formatter)` |
| `WithValueUnit` | `.ValueUnit(unit)` |
| `WithWordCloudAngles(Double[])` | `.WordCloud(w => w.Angles(angles))` |
| `WithWordCloudDensity(Double)` | `.WordCloud(w => w.Density(density))` |
| `WithWordCloudFontRange` | `.WordCloud(w => w.FontRange(minimum, maximum))` |
| `WithWordCloudMaximumTerms` | `.WordCloud(w => w.MaxTerms(maximumTerms))` |
| `WithXAxis` | `.XAxis(x => x.Title(title))` |
| `WithXAxisBounds` | `.XAxis(x => x.Bounds(minimum, maximum))` |
| `WithXAxisIntegerTicks` | `.XAxis(x => x.IntegerTicks(enabled))` |
| `WithXAxisLabelAngle` | `.XAxis(x => x.LabelAngle(degrees))` |
| `WithXAxisLabelDensity` | `.XAxis(x => x.LabelDensity(density))` |
| `WithXAxisScale` | `.XAxis(x => x.Scale(scale))` |
| `WithXAxisTimeScale` | `.XAxis(x => x.TimeScale(timeZone: timeZone, showTimeZone: showTimeZone, label: label))` |
| `WithXAxisValueFormatter` | `.XAxis(x => x.LabelBy(formatter))` |
| `WithXAxisVisible` | `.XAxis(x => x.Visible(visible))` |
| `WithXDateLabels` | `.XDateLabels(...)` |
| `WithXLabels` | `.XLabels(...)` |
| `WithYAxis` | `.YAxis(y => y.Title(title))` |
| `WithYAxisBounds` | `.YAxis(y => y.Bounds(minimum, maximum))` |
| `WithYAxisIntegerTicks` | `.YAxis(y => y.IntegerTicks(enabled))` |
| `WithYAxisLabelDensity` | `.YAxis(y => y.LabelDensity(density))` |
| `WithYAxisScale` | `.YAxis(y => y.Scale(scale))` |
| `WithYAxisVisible` | `.YAxis(y => y.Visible(visible))` |
| `XAxisTitle (property)` | `Options.XAxis.Title` |
| `YAxisTitle (property)` | `Options.YAxis.Title` |

## ChartAdditionalValueAxis

| 4.x | 5.0 |
| --- | --- |
| `Configure` | `.Axis(configure)` |
| `WithColor(ChartColor?)` | `.Color(color)` |

## ChartAxis

| 4.x | 5.0 |
| --- | --- |
| `ConfigureGrid` | `.Grid(configure)` |
| `Maximum (property)` | `Max` |
| `Minimum (property)` | `Min` |
| `WithAutomaticBounds` | `.Min(null).Max(null)` |
| `WithAutomaticLabelAngle` | `.AutomaticLabelAngle(...)` |
| `WithAutomaticTickCount` | `.AutomaticTickCount(...)` |
| `WithAutomaticTicks` | `.AutomaticTicks(...)` |
| `WithBounds` | `.Bounds(...)` |
| `WithCrossAt(Double?)` | `.CrossAt(value)` |
| `WithGrid(ChartAxisGridStyle)` | `.Grid(style)` |
| `WithIntegerTicks(Boolean)` | `.IntegerTicks(enabled)` |
| `WithLabelFormatter(String})` | `.LabelBy(formatter)` |
| `WithLogarithmicScale` | `.LogarithmicScale(...)` |
| `WithMinorTicks(Int32)` | `.MinorTickCount(count)` |
| `WithReversal(Boolean)` | `.Reversed(reversed)` |
| `WithRuleCrossAt(Double?)` | `.RuleCrossAt(value)` |
| `WithScale(ChartScaleKind)` | `.Scale(scale)` |
| `WithTickSpacing` | `.TickSpacing(...)` |
| `WithTickStep` | `.TickStep(...)` |
| `WithTickValues` | `.TickValues(...)` |
| `WithTimeScale` | `.TimeScale(...)` |
| `WithTimeSessions` | `.TimeSessions(...)` |
| `WithTimeSpacing` | `.TimeSpacing(...)` |
| `WithValueFormat(ChartValueFormat)` | `.ValueFormat(format)` |
| `WithVisibility(Boolean)` | `.Visible(visible)` |

## ChartAxisGridStyle

| 4.x | 5.0 |
| --- | --- |
| `WithBands(Boolean)` | `.ShowBands(visible)` |
| `WithBands(ChartColor, Double?)` | `.BandOpacity(opacity).BandColor(color).ShowBands(true)` |
| `WithColor(ChartColor?)` | `.Color(color)` |
| `WithDash` | `.Dash(...)` |
| `WithInheritedDash` | `.InheritDash(...)` |
| `WithLines(Boolean?)` | `.ShowLines(visible)` |
| `WithMinorOpacity(Double?)` | `.MinorOpacity(opacity)` |
| `WithOpacity(Double?)` | `.Opacity(opacity)` |
| `WithStrokeWidth(Double?)` | `.StrokeWidth(width)` |

## ChartBarVisualStyle

| 4.x | 5.0 |
| --- | --- |
| `WithBodyOpacity(Double)` | `.BodyOpacity(opacity)` |
| `WithCapHighlight(Double)` | `.CapHighlightOpacity(opacity)` |
| `WithCapInset(Double)` | `.CapInset(inset)` |
| `WithCapOpacity(Double)` | `.CapOpacity(opacity)` |
| `WithCapShadow(Double, Double)` | `.CapShadowOpacity(opacity).CapShadowOffset(offset)` |
| `WithCapShadow(Double, Double, Double)` | `.CapShadowOpacity(opacity).CapShadowOffset(offset).CapShadowSpread(spread)` |
| `WithCapThickness(Double)` | `.CapThickness(thickness)` |
| `WithCornerRadius(Double)` | `.CornerRadius(radius)` |
| `WithKind(ChartBarStyle)` | `.Kind(kind)` |

## ChartBubbleOptions

| 4.x | 5.0 |
| --- | --- |
| `UseAutomaticSizeDomain` | `.AutomaticSizeDomain(...)` |
| `WithSizeDomain` | `.SizeDomain(...)` |

## ChartExtensions

| 4.x | 5.0 |
| --- | --- |
| `ConfigureTheme` | `.Theme(...)` |
| `WithPalette` | `.Palette(...)` |
| `WithPngOutputScale(ChartPngOutputScale)` | `.PngOutputScale((int)scale)` |

## ChartGrid

| 4.x | 5.0 |
| --- | --- |
| `ConfigureSubtitleStyle(TextStyleOverride})` | `.SubtitleStyle(configure)` |
| `ConfigureTitleStyle(TextStyleOverride})` | `.TitleStyle(configure)` |
| `WithAutomaticPanelSize()` | `.PanelSize(null)` |
| `WithColumns(Int32)` | `.Columns(columns)` |
| `WithGap(Int32)` | `.Gap(gap)` |
| `WithPadding(Int32)` | `.Padding(padding)` |
| `WithPanelFit(VisualPanelFit)` | `.PanelFit(fit)` |
| `WithPanelSize` | `.PanelSize(...)` |
| `WithPanelSpan` | `.PanelSpan(...)` |
| `WithPanelThemes(Boolean)` | `.UsePanelThemes(enabled)` |
| `WithPngOutputScale(ChartPngOutputScale)` | `.PngOutputScale((int)scale)` |
| `WithPngOutputScale(Int32)` | `.PngOutputScale(scale)` |
| `WithSharedAxes` | `.ShareAxes(...)` |
| `WithSharedXAxis` | `.ShareXAxis(...)` |
| `WithSharedYAxis` | `.ShareYAxis(...)` |
| `WithSubtitle(String)` | `.Subtitle(subtitle)` |
| `WithSvgColorVariables(SvgColorVariables)` | `.SvgColorVariables(variables)` |
| `WithTheme` | `.Theme(theme).ThemeMode(mode)` |
| `WithThemeMode(ThemeMode)` | `.ThemeMode(mode)` |
| `WithTitle(String)` | `.Title(title)` |

## ChartGridLineStyle

| 4.x | 5.0 |
| --- | --- |
| `WithDash` | `.Dash(...)` |
| `WithHorizontalLines(Boolean)` | `.ShowHorizontalLines(visible)` |
| `WithHorizontalOpacity(Double)` | `.HorizontalOpacity(opacity)` |
| `WithStrokeWidth(Double)` | `.StrokeWidth(width)` |
| `WithVerticalLines(Boolean)` | `.ShowVerticalLines(visible)` |
| `WithVerticalOpacity(Double)` | `.VerticalOpacity(opacity)` |

## ChartInteractionScenarioExtensions

| 4.x | 5.0 |
| --- | --- |
| `WithActiveScenario` | `.ActiveScenario(...)` |
| `WithColor(ChartInteractionScenario, String)` | `.Color(color)` |
| `WithDeepLinkState` | `.DeepLinkState(...)` |
| `WithDescription(ChartInteractionScenario, String)` | `.Description(description)` |
| `WithDuration(ChartInteractionScenarioStep, Int32)` | `.DurationMilliseconds(durationMilliseconds)` |
| `WithFocusMode(ChartInteractionScenario, ChartInteractionScenarioFocusMode)` | `.FocusMode(focusMode)` |
| `WithMetadata` | `.Metadata(...)` |
| `WithPlayback(ChartInteractionScenario, Int32, Boolean, Boolean)` | `.PlaybackDelayMilliseconds(stepDurationMilliseconds).LoopPlayback(loop).AutoPlay(autoPlay)` |
| `WithoutActiveScenario(ChartInteractionOptions)` | `.ActiveScenarioId(null)` |

## ChartLineVisualStyle

| 4.x | 5.0 |
| --- | --- |
| `WithAmbientHalo(Double, Double)` | `.AmbientHaloOpacity(opacity).AmbientHaloStrokeExtra(strokeExtra)` |
| `WithHalo(Double, Double)` | `.HaloOpacity(opacity).HaloStrokeExtra(strokeExtra)` |
| `WithHighlight(Double, Double)` | `.HighlightOpacity(opacity).HighlightStrokeRatio(strokeRatio)` |

## ChartList

| 4.x | 5.0 |
| --- | --- |
| `WithDenseMode(Boolean)` | `.Dense(enabled)` |
| `WithMarker(VisualListMarker)` | `.Marker(marker)` |

## ChartMapGeoJsonOptions

| 4.x | 5.0 |
| --- | --- |
| `WithCoordinateBounds(Double, Double, Double, Double, Boolean)` | `.MinimumLongitude(minimumLongitude).MaximumLongitude(maximumLongitude).MinimumLatitude(minimumLatitude).MaximumLatitude(maximumLatitude).IncludeFeaturesIntersectingCoordinateBounds(includeIntersections)` |

## ChartMapPointSizeOptions

| 4.x | 5.0 |
| --- | --- |
| `UseAutomaticSizeDomain` | `.AutomaticSizeDomain(...)` |
| `WithSizeDomain` | `.SizeDomain(...)` |

## ChartOptions

| 4.x | 5.0 |
| --- | --- |
| `BarMode (property)` | `Bars.Mode` |
| `BarStyle (property)` | `Bars.Style` |
| `BarVisualStyle (property)` | `Bars.VisualStyle` |
| `CircleRadiusScale (property)` | `Circle.RadiusScale` |
| `CircleStrokeScale (property)` | `Circle.StrokeScale` |
| `ClearSecondaryYAxisBounds (property)` | `SecondaryYAxis.AutomaticBounds` |
| `ClearXAxisBounds (property)` | `XAxis.AutomaticBounds` |
| `ClearYAxisBounds (property)` | `YAxis.AutomaticBounds` |
| `ConfigureBarVisualStyle (property)` | `Bars.ConfigureVisualStyle` |
| `DataLabelConnectorColor (property)` | `DataLabels.ConnectorColor` |
| `DataLabelConnectorOpacity (property)` | `DataLabels.ConnectorOpacity` |
| `DataLabelConnectorStrokeWidth (property)` | `DataLabels.ConnectorStrokeWidth` |
| `DataLabelConnectorStyle (property)` | `DataLabels.ConnectorStyle` |
| `DataLabelPlacement (property)` | `DataLabels.Placement` |
| `DataLabelVisibility (property)` | `DataLabels.Visibility` |
| `DonutCenterLabel (property)` | `Pie.CenterLabel` |
| `DonutCenterValue (property)` | `Pie.CenterValue` |
| `DonutInnerRadiusRatio (property)` | `Pie.InnerRadiusRatio` |
| `GanttOpenEndStyle (property)` | `Gantt.OpenEndStyle` |
| `GanttTickInterval (property)` | `Gantt.TickInterval` |
| `GanttToday (property)` | `Gantt.Today` |
| `GanttTodayStyle (property)` | `Gantt.TodayStyle` |
| `HasDataLabelConnectorStyle (property)` | `DataLabels.HasConnectorStyle` |
| `HasExplicitLegend (property)` | `Legend.HasExplicitVisibility` |
| `HasExplicitLegendBudget (property)` | `Legend.HasExplicitBudget` |
| `HasExplicitLegendPosition (property)` | `Legend.HasExplicitPosition` |
| `HasExplicitMapScaleLegendPosition (property)` | `Map.HasExplicitScaleLegendPosition` |
| `HasExplicitMapViewport (property)` | `Map.HasExplicitViewport` |
| `HasPreparedBarCornerRadius (property)` | `Bars.HasPreparedCornerRadius` |
| `HeatmapCellGap (property)` | `Heatmap.CellGap` |
| `HeatmapCellRadius (property)` | `Heatmap.CellRadius` |
| `HeatmapColorDimension (property)` | `Heatmap.ColorDimension` |
| `HeatmapColorScale (property)` | `Heatmap.ColorScale` |
| `HeatmapColumnWindow (property)` | `Heatmap.ColumnWindow` |
| `HeatmapNaturalHeight (property)` | `Heatmap.NaturalHeight` |
| `HeatmapRelativeScale (property)` | `Heatmap.RelativeScale` |
| `HeatmapScale (property)` | `Heatmap.Scale` |
| `HeatmapValueTextMode (property)` | `Heatmap.ValueTextMode` |
| `LaneSummaryHeader (property)` | `StateTimeline.SummaryHeader` |
| `LegendMaximumHeightFraction (property)` | `Legend.MaxHeightFraction` |
| `LegendMaximumRows (property)` | `Legend.MaxRows` |
| `LegendPosition (property)` | `Legend.Position` |
| `LineMarkerMode (property)` | `Lines.MarkerMode` |
| `LineOrientation (property)` | `Lines.Orientation` |
| `LineVisualStyle (property)` | `Lines.VisualStyle` |
| `MapBaseLayers (property)` | `Map.BaseLayers` |
| `MapCallouts (property)` | `Map.Callouts` |
| `MapColorDimension (property)` | `Map.ColorDimension` |
| `MapColorScale (property)` | `Map.ColorScale` |
| `MapConnectors (property)` | `Map.Connectors` |
| `MapOverlayLayers (property)` | `Map.OverlayLayers` |
| `MapRegionStrokeColor (property)` | `Map.RegionStrokeColor` |
| `MapRegionStrokeWidth (property)` | `Map.RegionStrokeWidth` |
| `MapScaleLegendPosition (property)` | `Map.ScaleLegendPosition` |
| `MapViewport (property)` | `Map.Viewport` |
| `MaximumBarWidth (property)` | `Bars.MaxWidth` |
| `MaximumPieSlices (property)` | `Pie.MaxSlices` |
| `PictorialArtwork (property)` | `Pictorial.Artwork` |
| `PictorialColumns (property)` | `Pictorial.Columns` |
| `PictorialEmptyOpacity (property)` | `Pictorial.EmptyOpacity` |
| `PictorialFillDirection (property)` | `Pictorial.FillDirection` |
| `PictorialMaximum (property)` | `Pictorial.Max` |
| `PictorialPngFallbackShape (property)` | `Pictorial.PngFallbackShape` |
| `PictorialShape (property)` | `Pictorial.Shape` |
| `PictorialSvgPathData (property)` | `Pictorial.SvgPathData` |
| `PictorialSvgPathViewBox (property)` | `Pictorial.SvgPathViewBox` |
| `PictorialSymbolScale (property)` | `Pictorial.SymbolScale` |
| `PictorialValuePerSymbol (property)` | `Pictorial.ValuePerSymbol` |
| `PieInsideLabelRadiusRatio (property)` | `Pie.InsideLabelRadiusRatio` |
| `PieLegendContent (property)` | `Pie.LegendContent` |
| `PieOuterOutline (property)` | `Pie.OuterOutline` |
| `PieOutsideLabelDistanceRatio (property)` | `Pie.OutsideLabelDistanceRatio` |
| `PiePreserveSourceOrder (property)` | `Pie.PreserveSourceOrder` |
| `PieSliceLabelContent (property)` | `Pie.SliceLabelContent` |
| `PieSliceLabelFormatter (property)` | `Pie.SliceLabelFormatter` |
| `PieSliceOpacity (property)` | `Pie.SliceOpacity` |
| `PieSliceOutline (property)` | `Pie.SliceOutline` |
| `PinStateColorsInForcedColors (property)` | `StateTimeline.PinColorsInForcedColors` |
| `PngFontCollectionIndex (property)` | `Png.FontCollectionIndex` |
| `PngFontFaceName (property)` | `Png.FontFaceName` |
| `PngFontPath (property)` | `Png.FontPath` |
| `PngOutputScale (property)` | `Png.OutputScale` |
| `PngSupersamplingScale (property)` | `Png.SupersamplingScale` |
| `PngTextHinting (property)` | `Png.TextHinting` |
| `PolarAxisLabelFactor (property)` | `Polar.AxisLabelFactor` |
| `PolarAxisScaleFactor (property)` | `Polar.AxisScaleFactor` |
| `PolarAxisStroke (property)` | `Polar.AxisStroke` |
| `PolarGridFill (property)` | `Polar.GridFill` |
| `PolarGridFillOpacity (property)` | `Polar.GridFillOpacity` |
| `PolarGridRingCount (property)` | `Polar.GridRingCount` |
| `PolarGridShape (property)` | `Polar.GridShape` |
| `PolarGridStroke (property)` | `Polar.GridStroke` |
| `PolarRotationDegrees (property)` | `Polar.RotationDegrees` |
| `ProgressBarThicknessRatio (property)` | `Progress.BarThicknessRatio` |
| `ProgressMaximum (property)` | `Progress.Max` |
| `ProgressTrackOpacity (property)` | `Progress.TrackOpacity` |
| `RadialProgressRadiusScale (property)` | `Progress.RadialRadiusScale` |
| `RadialProgressStrokeScale (property)` | `Progress.RadialStrokeScale` |
| `RegionMapBounds (property)` | `Map.RegionBounds` |
| `ResolvePreparedBarVisualStyle (property)` | `Bars.ResolvePreparedVisualStyle` |
| `ResolvePreparedLineVisualStyle (property)` | `Lines.ResolvePreparedVisualStyle` |
| `SecondaryYAxisMaximum (property)` | `SecondaryYAxis.Max` |
| `SecondaryYAxisMinimum (property)` | `SecondaryYAxis.Min` |
| `SecondaryYAxisValueFormatter (property)` | `SecondaryYAxis.LabelFormatter` |
| `SetDefaultLegendPosition (property)` | `Legend.SetDefaultPosition` |
| `SetPictorialSvgPath (property)` | `Pictorial.SvgPath` |
| `SetSecondaryYAxisBounds (property)` | `SecondaryYAxis.Bounds` |
| `SetWordCloudFontRange (property)` | `WordCloud.FontRange` |
| `SetXAxisBounds (property)` | `XAxis.Bounds` |
| `SetYAxisBounds (property)` | `YAxis.Bounds` |
| `ShowCircleStatusLabel (property)` | `Circle.ShowStatusLabel` |
| `ShowDataLabels (property)` | `DataLabels.Visible` |
| `ShowDonutCenterLabel (property)` | `Pie.ShowCenterLabel` |
| `ShowHeatmapCategoryLegend (property)` | `Heatmap.ShowCategoryLegend` |
| `ShowHeatmapColumnLabels (property)` | `Heatmap.ShowColumnLabels` |
| `ShowHeatmapScaleStepLabels (property)` | `Heatmap.ShowScaleStepLabels` |
| `ShowLegend (property)` | `Legend.Visible` |
| `ShowMapLabels (property)` | `Map.ShowLabels` |
| `ShowMapSurface (property)` | `Map.ShowSurface` |
| `ShowPictorialValues (property)` | `Pictorial.ShowValues` |
| `ShowPointLegend (property)` | `Legend.ShowPoints` |
| `ShowProgressHandles (property)` | `Progress.ShowHandles` |
| `ShowProgressRingCenterLabel (property)` | `Progress.ShowRingCenterLabel` |
| `ShowProgressValues (property)` | `Progress.ShowValues` |
| `ShowStackTotals (property)` | `Bars.ShowTotals` |
| `ShowXAxis (property)` | `XAxis.Visible` |
| `ShowYAxis (property)` | `YAxis.Visible` |
| `StateCategories (property)` | `StateTimeline.Categories` |
| `StateTimelineSummaryHeader (property)` | `StateTimeline.SummaryHeader` |
| `WordCloudAngles (property)` | `WordCloud.Angles` |
| `WordCloudDensity (property)` | `WordCloud.Density` |
| `WordCloudMaxFontSize (property)` | `WordCloud.MaxFontSize` |
| `WordCloudMaximumTerms (property)` | `WordCloud.MaxTerms` |
| `WordCloudMinFontSize (property)` | `WordCloud.MinFontSize` |
| `XAxisLabelAngle (property)` | `XAxis.LabelAngle` |
| `XAxisLabelDensity (property)` | `XAxis.LabelDensity` |
| `XAxisLabels (property)` | `XAxis.Labels` |
| `XAxisMaximum (property)` | `XAxis.Max` |
| `XAxisMinimum (property)` | `XAxis.Min` |
| `XAxisValueFormatter (property)` | `XAxis.LabelFormatter` |
| `YAxisLabelDensity (property)` | `YAxis.LabelDensity` |
| `YAxisMaximum (property)` | `YAxis.Max` |
| `YAxisMinimum (property)` | `YAxis.Min` |

## ChartRadialLayer

| 4.x | 5.0 |
| --- | --- |
| `WithAngles(Double, Double)` | `.StartAngleDegrees(startAngleDegrees).SweepAngleDegrees(sweepAngleDegrees)` |
| `WithColor(ChartColor?)` | `.Color(color)` |
| `WithGeometry(Double, Double)` | `.RadiusRatio(radiusRatio).StrokeRatio(strokeRatio)` |
| `WithLineCap(ChartRadialLayerCap)` | `.LineCap(lineCap)` |
| `WithName(String)` | `.Name(name)` |
| `WithOpacity(Double)` | `.Opacity(opacity)` |
| `WithScale` | `.Scale(...)` |
| `WithSeparators(Int32, ChartColor?, Double, Double)` | `.SeparatorCount(count).SeparatorColor(color).SeparatorStrokeWidth(strokeWidth).SeparatorInsetRatio(insetRatio)` |
| `WithValue(Double)` | `.Value(value)` |

## ChartSeries

| 4.x | 5.0 |
| --- | --- |
| `ConfigureBoxPlotStatistics` | `.BoxPlotStatistics(...)` |
| `ConfigureBulletAxis` | `.BulletAxis(...)` |
| `ConfigureColorDimension` | `.ColorDimension(...)` |
| `ConfigureDataLabelStyle(TextStyleOverride})` | `.DataLabelStyle(configure)` |
| `ConfigureDifferenceArea(ChartDifferenceAreaOptions})` | `.DifferenceArea(configure)` |
| `ConfigureDumbbell` | `.Dumbbell(...)` |
| `ConfigureFinancial(ChartFinancialOptions})` | `.Financial(configure)` |
| `ConfigureFinancialValue` | `.FinancialValue(...)` |
| `ConfigureFinancialValues` | `.FinancialValues(...)` |
| `ConfigureMapPointSize` | `.MapPointSize(...)` |
| `ConfigureMarkers(ChartMarkerOptions})` | `.Markers(configure)` |
| `ConfigurePointDataLabelStyle` | `.PointDataLabelStyle(...)` |
| `ConfigurePointMarkers` | `.PointMarkers(...)` |
| `ConfigurePolar(ChartPolarOptions})` | `.Polar(configure)` |
| `ConfigureRadar(ChartRadarOptions})` | `.Radar(configure)` |
| `ConfigureTrendLine` | `.TrendLine(...)` |
| `UseChartDataLabelPlacement()` | `.DataLabelPlacement(null)` |
| `UseChartDataLabels()` | `.ShowDataLabels(null)` |
| `UseDefaultSliceOffset` | `.PointSliceOffset(pointIndex, 0)` |
| `UseDefaultSliceRadius` | `.ResetPointSliceRadius(...)` |
| `UseFormattedPointLabel` | `.ResetPointLabel(...)` |
| `UsePrimaryValueAxis` | `.PrimaryAxis()` |
| `UseSecondaryValueAxis` | `.SecondaryAxis()` |
| `UseSeriesColor` | `.PointColor(pointIndex, (ChartColor?)null)` |
| `UseSeriesDataLabelStyle` | `.ResetPointDataLabelStyle(...)` |
| `UseSeriesFillPattern` | `.PointFillPattern(pointIndex, null)` |
| `UseSeriesFinancialStyle` | `.ResetFinancialValue(...)` |
| `UseSeriesMarkers` | `.ResetPointMarkers(...)` |
| `UseSeriesStrokeColor` | `.PointStrokeColor(pointIndex, null)` |
| `UseSolidFill` | `.SolidFill()` |
| `UseThemeColor` | `.ThemeColor()` |
| `UseThemeMarkerRadius()` | `.MarkerRadius(null)` |
| `UseValueAxis` | `.ValueAxis(...)` |
| `WithBoundaryInterpolation` | `.BoundaryInterpolation(...)` |
| `WithBoxPlotGroupId` | `.BoxPlotGroupId(...)` |
| `WithBoxPlotGroupIds` | `.BoxPlotGroupIds(...)` |
| `WithColor(ChartColor?)` | `.Color(color)` |
| `WithColorScale(ChartColorScale)` | `.ColorScale(scale)` |
| `WithDataLabelFormatter` | `.DataLabelBy(formatter)` |
| `WithDataLabelPlacement(ChartDataLabelPlacement)` | `.DataLabelPlacement(placement)` |
| `WithDataLabels(Boolean)` | `.ShowDataLabels(visible)` |
| `WithFill(ChartFill)` | `.Fill(fill)` |
| `WithFillPattern(ChartFillPattern)` | `.FillPattern(pattern)` |
| `WithFlowState` | `.FlowState(...)` |
| `WithFlowStyle` | `.FlowStyle(...)` |
| `WithFunnelConnectorStyle` | `.FunnelConnectorStyle(...)` |
| `WithInterpolation` | `.Interpolation(...)` |
| `WithItemStyler` | `.ItemStyleBy(...)` |
| `WithLegendEntry(Boolean)` | `.ShowInLegend(visible)` |
| `WithMapClustering` | `.MapClustering(...)` |
| `WithMapInset` | `.MapInset(...)` |
| `WithMarkOptions(ChartMarkOptions)` | `.MarkOptions(options)` |
| `WithMarkerRadius(Double)` | `.MarkerRadius(radius)` |
| `WithName(String)` | `.Name(name)` |
| `WithNodeState` | `.NodeState(...)` |
| `WithNormalization(Double?)` | `.NormalizedTo(target)` |
| `WithObservationId` | `.ObservationId(...)` |
| `WithObservationIds` | `.ObservationIds(...)` |
| `WithPointColor` | `.PointColor(...)` |
| `WithPointColorRange` | `.PointColorRange(...)` |
| `WithPointFillPattern` | `.PointFillPattern(...)` |
| `WithPointLabel` | `.PointLabel(...)` |
| `WithPointSliceOffset` | `.PointSliceOffset(...)` |
| `WithPointSliceRadiusRatio` | `.PointSliceRadiusRatio(...)` |
| `WithPointStrokeColor` | `.PointStrokeColor(...)` |
| `WithRadialBand(ChartRadialBand)` | `.RadialBand(band)` |
| `WithRowOptions(ChartMarkOptions)` | `.RowOptions(options)` |
| `WithSemanticRole(String)` | `.SemanticRole(role)` |
| `WithSmooth(Boolean)` | `.Smooth(smooth)` |
| `WithStackGroup(String)` | `.StackGroup(group)` |
| `WithStackedAreaMissingData(ChartStackedAreaMissingDataPolicy)` | `.StackedAreaMissingData(policy)` |
| `WithState(ChartSeriesState)` | `.StateRole(state)` |
| `WithStrokeStyle(ChartStrokeStyle)` | `.StrokeStyle(style)` |
| `WithStrokeStyleFrom` | `.StrokeStyleFrom(...)` |
| `WithStrokeWidth(Double)` | `.StrokeWidth(width)` |
| `WithTimeBuckets` | `.TimeBuckets(...)` |
| `WithTooltipFormatter` | `.TooltipBy(formatter)` |

## ChartTable

| 4.x | 5.0 |
| --- | --- |
| `ConfigureRow` | `.UpdateRow(...)` |
| `WithColumns` | `.Columns(...)` |
| `WithDenseMode(Boolean)` | `.Dense(enabled)` |
| `WithHeader(Boolean)` | `.ShowHeader(enabled)` |
| `WithRowStriping(Boolean)` | `.RowStriping(enabled)` |
| `WithStatusColumn` | `.StatusColumn(...)` |

## ChartTableCell

| 4.x | 5.0 |
| --- | --- |
| `WithBadge` | `.Badge(...)` |
| `WithMiniBars` | `.MiniBars(...)` |
| `WithSparkline` | `.Sparkline(...)` |

## DateStripBlock

| 4.x | 5.0 |
| --- | --- |
| `WithHeader(String)` | `.Header(header)` |

## FishboneDiagramBlock

| 4.x | 5.0 |
| --- | --- |
| `WithNaturalSize` | `.NaturalSize(...)` |

## FlowArtifact

| 4.x | 5.0 |
| --- | --- |
| `ConfigureConnector` | `.UpdateConnector(...)` |
| `ConfigureStep` | `.UpdateStep(...)` |
| `WithSize` | `.Width(width).Height(height).Padding(padding)` |
| `WithSubtitle(String)` | `.Subtitle(subtitle)` |
| `WithTitle(String)` | `.Title(title)` |

## FontSpec

| 4.x | 5.0 |
| --- | --- |
| `WithColorPalette(Int32)` | `.ColorPaletteIndex(index)` |
| `WithVariation` | `.Variation(...)` |

## GitGraphBlock

| 4.x | 5.0 |
| --- | --- |
| `WithBranchLabels(Boolean)` | `.ShowBranchLabels(visible)` |
| `WithCommitLabels(Boolean)` | `.ShowCommitLabels(visible)` |
| `WithDirection(GitGraphDirection)` | `.Direction(direction)` |
| `WithNaturalSize` | `.NaturalSize(...)` |
| `WithParallelCommits(Boolean)` | `.ParallelCommits(parallel)` |
| `WithRotatedCommitLabels(Boolean)` | `.RotateCommitLabels(rotated)` |

## GraphSceneSuperTopologyExtensions

| 4.x | 5.0 |
| --- | --- |
| `UseSuperTopologyDefaults` | `.SuperTopologyDefaults(...)` |

## HeatmapInsightCard

| 4.x | 5.0 |
| --- | --- |
| `WithColorKey` | `.Minimum(minimum).Maximum(maximum).LowColor(lowColor).HighColor(highColor).ColorKeyLabel(label)` |
| `WithColumns` | `.Columns(...)` |
| `WithInsightTitle(String)` | `.InsightTitle(title)` |
| `WithPeriodLabel(String)` | `.PeriodLabel(label)` |

## MetricCard

| 4.x | 5.0 |
| --- | --- |
| `WithBadgePlacement(MetricCardBadgePlacement)` | `.BadgePlacement(placement)` |
| `WithCaption(String)` | `.Caption(caption)` |
| `WithIcon(Icon)` | `.Icon(icon)` |
| `WithMetric` | `.Metric(...)` |
| `WithMicroVisualPlacement(MetricCardMicroVisualPlacement)` | `.MicroVisualPlacement(placement)` |
| `WithMicroVisualSurface(MetricCardMicroVisualSurface)` | `.MicroVisualSurface(surface)` |
| `WithMiniBars` | `.MiniBars(...)` |
| `WithMiniSparkline` | `.MiniSparkline(...)` |
| `WithMiniSparklineStyle(ChartLineAreaForm)` | `.MiniSparklineStyle(style)` |
| `WithSecondaryMiniSparkline` | `.SecondaryMiniSparkline(...)` |
| `WithSecondarySparkline` | `.SecondarySparkline(...)` |
| `WithSparkline` | `.Sparkline(...)` |
| `WithStatus(Tone)` | `.Status(status)` |
| `WithSymbol(String)` | `.Symbol(symbol)` |
| `WithTrend(String)` | `.Trend(trend)` |
| `WithUnit(String)` | `.Unit(unit)` |
| `WithoutMiniBars` | `.ClearMiniBars(...)` |
| `WithoutMiniSparkline` | `.ClearMiniSparkline(...)` |

## PacketLayoutBlock

| 4.x | 5.0 |
| --- | --- |
| `WithBitNumbers(Boolean)` | `.ShowBitNumbers(visible)` |
| `WithBitOrder(PacketBitOrder)` | `.BitOrder(order)` |
| `WithBitWidth(Double?)` | `.BitWidth(width)` |
| `WithBitsPerRow(Int32)` | `.BitsPerRow(bitsPerRow)` |
| `WithFieldGap(Double?)` | `.FieldGap(gap)` |
| `WithRowGap(Double?)` | `.RowGap(gap)` |
| `WithRowHeight(Double?)` | `.RowHeight(height)` |

## RadialMetricCard

| 4.x | 5.0 |
| --- | --- |
| `WithIcon(Icon)` | `.Icon(icon)` |
| `WithLayers` | `.Layers(...)` |
| `WithMetric` | `.Metric(...)` |

## RailroadDiagramBlock

| 4.x | 5.0 |
| --- | --- |
| `WithNaturalSize` | `.NaturalSize(...)` |

## ScheduleTimelineBlock

| 4.x | 5.0 |
| --- | --- |
| `WithCurrentTime(Double?)` | `.CurrentTime(value)` |
| `WithGrid(Boolean)` | `.ShowGrid(enabled)` |
| `WithTimeRange(Double, Double, Double)` | `.Start(start).End(end).TickInterval(tickInterval)` |

## SegmentedMetricBlock

| 4.x | 5.0 |
| --- | --- |
| `WithCaption(String)` | `.Caption(caption)` |
| `WithHeaderSymbol(String)` | `.HeaderSymbol(symbol)` |
| `WithMetric` | `.Metric(...)` |
| `WithStyle(SegmentedMetricStyle)` | `.Style(style)` |

## SegmentedMetricItem

| 4.x | 5.0 |
| --- | --- |
| `WithColor(ChartColor?)` | `.Color(color)` |
| `WithDelta(String)` | `.Delta(delta)` |
| `WithDisplayValue` | `.DisplayValue(...)` |
| `WithPattern(ChartFillPattern)` | `.Pattern(pattern)` |
| `WithProgress(Double, Int32)` | `.Maximum(maximum).Segments(segments)` |
| `WithStatus(Tone)` | `.Status(status)` |
| `WithSymbol(String)` | `.Symbol(symbol)` |

## SequenceArtifact

| 4.x | 5.0 |
| --- | --- |
| `WithSize(Double, Double)` | `.Width(width).Height(height)` |
| `WithSubtitle(String)` | `.Subtitle(subtitle)` |
| `WithTitle(String)` | `.Title(title)` |

## Sparkline

| 4.x | 5.0 |
| --- | --- |
| `WithColor(ChartColor?)` | `.Color(color)` |
| `WithStyle(SparklineStyle, Boolean)` | `.Style(style).Smooth(smooth)` |
| `WithValueFormat(ChartValueFormat)` | `.ValueFormat(format)` |

## StorySourceText

| 4.x | 5.0 |
| --- | --- |
| `WithLanguage` | `.Language(language)` |

## TableArtifact

| 4.x | 5.0 |
| --- | --- |
| `ConfigureRow` | `.UpdateRow(...)` |
| `WithCapabilities` | `.Capabilities(capabilities)` |
| `WithSubtitle(String)` | `.Subtitle(subtitle)` |
| `WithTitle(String)` | `.Title(title)` |

## TerminalStory

| 4.x | 5.0 |
| --- | --- |
| `WithDialect` | `.Dialect(...)` |
| `WithFinalPrompt(Boolean)` | `.ShowFinalPrompt(visible)` |
| `WithInitialTab` | `.InitialTab(...)` |
| `WithPlaybackSpeed` | `.PlaybackSpeed(...)` |
| `WithPngOutputScale(Int32)` | `.PngOutputScale(scale)` |
| `WithStyle(TerminalStyleOptions)` | `.Style(style)` |
| `WithTabHold(Double)` | `.TabHoldSeconds(seconds)` |
| `WithTheme` | `.Theme(theme).ThemeMode(mode)` |
| `WithThemeMode(ThemeMode)` | `.ThemeMode(mode)` |
| `WithTiming` | `.InitialDelaySeconds(initialDelaySeconds).CharactersPerSecond(charactersPerSecond).LineDelaySeconds(lineDelaySeconds)` |
| `WithTitle(String)` | `.Title(title)` |
| `WithTypography` | `.Typography(...)` |
| `WithWidth(Int32)` | `.Width(width)` |
| `WithWindowStyle(TerminalWindowStyle)` | `.WindowStyle(style)` |
| `WithWorkingDirectory(String)` | `.WorkingDirectory(workingDirectory)` |

## TerminalStoryAnimationOptions

| 4.x | 5.0 |
| --- | --- |
| `WithEndHold(Double)` | `.EndHoldSeconds(seconds)` |
| `WithFramesPerSecond(Int32)` | `.FramesPerSecond(framesPerSecond)` |
| `WithLoop(Boolean)` | `.Loop(loop)` |
| `WithMaximumFrames(Int32)` | `.MaximumFrames(maximumFrames)` |
| `WithOutputScale(Int32)` | `.OutputScale(scale)` |

## TerminalTable

| 4.x | 5.0 |
| --- | --- |
| `WithColumns` | `.Columns(...)` |

## TextStyleOverride

| 4.x | 5.0 |
| --- | --- |
| `WithBaseline(TextBaseline)` | `.Baseline(baseline)` |
| `WithColor(ChartColor)` | `.Color(color)` |
| `WithColor(string)` | `.Color(ChartColor.FromHex(hex))` |
| `WithColorPalette(Int32)` | `.ColorPaletteIndex(index)` |
| `WithFontFamily(String)` | `.FontFamily(fontFamily)` |
| `WithFontSize(Double)` | `.FontSize(fontSize)` |
| `WithItalic(Boolean)` | `.Italic(enabled)` |
| `WithOpenTypeLanguage(String)` | `.OpenTypeLanguageTag(languageTag)` |
| `WithStrikethrough(TextDecorationStyle)` | `.StrikethroughStyle(style)` |
| `WithStrikethrough(bool)` | `.Strikethrough(enabled)` |
| `WithSubscript` | `.Baseline(TextBaseline.Subscript)` |
| `WithSuperscript` | `.Baseline(TextBaseline.Superscript)` |
| `WithTextCase(TextCaseTransform)` | `.TextCase(textCase)` |
| `WithUnderline(TextDecorationStyle)` | `.UnderlineStyle(style)` |
| `WithUnderline(bool)` | `.Underline(enabled)` |
| `WithVariation` | `.Variation(...)` |
| `WithWeight(String)` | `.FontWeight(fontWeight)` |

## TopologyChart

| 4.x | 5.0 |
| --- | --- |
| `AsDecorative(Boolean)` | `.Accessibility(a => a.Decorative(decorative))` |
| `ConfigureAccessibility(VisualAccessibility})` | `.Accessibility(configure)` |
| `ConfigureLabels(TopologyLabels})` | `.Labels(configure)` |

## TopologyChartExtensions

| 4.x | 5.0 |
| --- | --- |
| `ConfigureTheme` | `.Theme(...)` |
| `WithColor(TopologyScenario, String)` | `.Color(color)` |
| `WithDescription(TopologyScenario, String)` | `.Description(description)` |
| `WithDuration(TopologyScenarioStep, Int32)` | `.DurationMilliseconds(durationMilliseconds)` |
| `WithEdgeColor` | `.UpdateEdge(edgeId, e => e.Color(color))` |
| `WithEdgeEmphasis` | `.UpdateEdge(edgeId, e => e.Emphasis(emphasis))` |
| `WithEdgeEndpointLabels` | `.UpdateEdge(edgeId, e => e.SourceLabel(sourceLabel).TargetLabel(targetLabel))` |
| `WithEdgeEvidence` | `.UpdateEdge(edgeId, e => e.Evidence(evidence).EvidenceText(text))` |
| `WithEdgeLabelAnchor` | `.UpdateEdge(id, e => e.LabelAnchor(point))` |
| `WithEdgeLabelAnchorNode` | `.UpdateEdge(id, e => e.LabelAnchorNodeId(nodeId))` |
| `WithEdgeLabelAnnotationOffset` | `.UpdateEdge(id, e => e.LabelOffsetX(x).LabelOffsetY(y).LabelAnchor(null))` |
| `WithEdgeLabelOffset` | `.UpdateEdge(edgeId, e => e.LabelOffsetX(x).LabelOffsetY(y))` |
| `WithEdgeLayoutHints` | `.UpdateEdge(edgeId, e => e.PreferredLength(preferredLength).MinimumRankSpan(minimumRankSpan).RoutingPriority(routingPriority))` |
| `WithEdgeLineStyle` | `.UpdateEdge(edgeId, e => e.LineStyle(lineStyle))` |
| `WithEdgeMarkers` | `.UpdateEdge(edgeId, e => e.SourceMarker(source).TargetMarker(target))` |
| `WithEdgeMuted` | `.UpdateEdge(edgeId, e => e.Muted(isMuted))` |
| `WithEdgeNamedPorts` | `.UpdateEdge(edgeId, e => e.SourcePortId(sourcePortId).TargetPortId(targetPortId))` |
| `WithEdgePorts` | `.UpdateEdge(edgeId, e => e.SourcePort(sourcePort).TargetPort(targetPort))` |
| `WithEdgeRouteBundle` | `.EdgeRouteBundle(...)` |
| `WithEdgeRouteLane` | `.UpdateEdge(id, e => e.RouteLane(...))` |
| `WithEdgeStroke` | `.EdgeStroke(...)` |
| `WithEdgeUplink` | `.UpdateEdge(edgeId, e => e.UplinkEnd(childEnd))` |
| `WithEdgeWaypoints` | `.EdgeWaypoints(...)` |
| `WithEdgesOfKind` | `.UpdateEdges(kind, e => e.LineStyle(lineStyle).Emphasis(emphasis).Muted(isMuted).Color(color))` |
| `WithGroupColor` | `.UpdateGroup(groupId, g => g.Color(color))` |
| `WithGroupCoordinates` | `.UpdateGroup(groupId, g => g.Longitude(longitude).Latitude(latitude))` |
| `WithGroupIcon` | `.GroupIcon(...)` |
| `WithGroupLayout` | `.UpdateGroup(groupId, g => g.LayoutPolicy(layoutPolicy))` |
| `WithGroupPosition` | `.UpdateGroup(groupId, g => g.X(x).Y(y))` |
| `WithGroupSymbol` | `.UpdateGroup(groupId, g => g.Symbol(symbol))` |
| `WithId` | `.Id(id)` |
| `WithLayout(TopologyLayoutMode)` | `.LayoutMode(layoutMode)` |
| `WithLayout(TopologyLayoutMode,TopologyLayoutDirection)` | `.LayoutMode(layoutMode).LayoutDirection(layoutDirection)` |
| `WithLayoutDirection` | `.LayoutDirection(layoutDirection)` |
| `WithLegend(TopologyChart, TopologyLegend)` | `.Legend(legend)` |
| `WithMapViewport(TopologyChart, ChartMapViewport)` | `.MapViewport(viewport)` |
| `WithMetadata` | `.Metadata(...)` |
| `WithNodeArtwork` | `.NodeArtwork(...)` |
| `WithNodeBackground` | `.UpdateNode(nodeId, n => n.BackgroundColor(backgroundColor))` |
| `WithNodeBadge` | `.UpdateNode(nodeId, n => n.Badge(badge))` |
| `WithNodeColor` | `.UpdateNode(nodeId, n => n.Color(color))` |
| `WithNodeCoordinates` | `.UpdateNode(nodeId, n => n.Longitude(longitude).Latitude(latitude))` |
| `WithNodeDisplay` | `.UpdateNode(nodeId, n => n.DisplayMode(displayMode).Badge(badge))` |
| `WithNodeHealthRing` | `.UpdateNode(nodeId, n => n.ShowHealthRing(show))` |
| `WithNodeIcon` | `.NodeIcon(...)` |
| `WithNodesDisplay` | `.UpdateNodes(kind, n => n.DisplayMode(displayMode).Badge(badge))` |
| `WithNodesOfKind` | `.UpdateNodes(kind, n => n.Color(color).BackgroundColor(backgroundColor))` |
| `WithNodesOfKindIcon` | `.NodesOfKindIcon(...)` |
| `WithNodesOfKindStyle` | `.NodesOfKindStyle(...)` |
| `WithPayload(TopologyScenarioStep, String, TopologyScenarioPayloadFormat)` | `.Payload(payload).PayloadFormat(format)` |
| `WithPlayback(TopologyScenario, Int32, Boolean, Boolean)` | `.PlaybackDelayMilliseconds(stepDurationMilliseconds).LoopPlayback(loop).AutoPlay(autoPlay)` |
| `WithReciprocalEdgeRouteBundles` | `.ReciprocalEdgeRouteBundles(...)` |
| `WithRenderOptions` | `.DefaultRenderOptions(options)` |
| `WithSiblingGrouping` | `.SiblingGrouping(new TopologySiblingGrouping { MinimumCount = minimumCount, StaggerCount = staggerCount, MaximumRows = maximumRows, CollapseCount = collapseCount, TitleFormatter = titleFormatter })` |
| `WithSpotlight(TopologyScenario, Boolean)` | `.Spotlight(enabled)` |
| `WithSubtitle(TopologyChart, String)` | `.Subtitle(subtitle)` |
| `WithTheme` | `.Theme(...)` |
| `WithThemeMode(TopologyChart, ThemeMode)` | `.ThemeMode(mode)` |
| `WithTitle(TopologyChart, String)` | `.Title(title)` |
| `WithViewport` | `.Viewport(new TopologyViewport(width, height, padding))` |
| `WithoutNodeArtwork` | `.ClearNodeArtwork(...)` |

## TopologyHierarchyItem

| 4.x | 5.0 |
| --- | --- |
| `WithMetadata` | `.Metadata(...)` |

## TopologyIconArtwork

| 4.x | 5.0 |
| --- | --- |
| `WithPreserveAspectRatio` | `.PreserveAspectRatio(preserveAspectRatio)` |

## TopologyIconDefinition

| 4.x | 5.0 |
| --- | --- |
| `WithArtwork(TopologyIconArtwork)` | `.Artwork(artwork)` |
| `WithMetadata` | `.Metadata(...)` |
| `WithTags` | `.AddTags(...)` |

## TopologyIconPack

| 4.x | 5.0 |
| --- | --- |
| `WithIconTags` | `.AddIconTags(...)` |
| `WithMetadata` | `.Metadata(...)` |
| `WithTags` | `.AddTags(...)` |

## TopologyMotionOptions

| 4.x | 5.0 |
| --- | --- |
| `WithDuration` | `.DurationSeconds(seconds)` |
| `WithEndpointPulses(Boolean)` | `.PulseRouteEndpoints(enabled)` |
| `WithFrameLimit` | `.MaximumRasterFrames(maximumFrames)` |
| `WithFrameRate` | `.FramesPerSecond(framesPerSecond)` |
| `WithMarker` | `.MarkerRadius(radius).MarkerColor(color)` |
| `WithMarkerColor(String)` | `.MarkerColor(color)` |

## TopologyRenderOptions

| 4.x | 5.0 |
| --- | --- |
| `EnableHtmlExportControls (property)` | `Html.ExportControls` |
| `EnableHtmlFindControls (property)` | `Html.FindControls` |
| `EnableHtmlForceGraphControls (property)` | `Html.ForceGraphControls` |
| `EnableHtmlFullscreenControl (property)` | `Html.FullscreenControl` |
| `EnableHtmlInteractions (property)` | `Html.Interactions` |
| `EnableHtmlScenarioControls (property)` | `Html.ScenarioControls` |
| `EnableHtmlScenarioPanel (property)` | `Html.ScenarioPanel` |
| `EnableHtmlScenarioSequence (property)` | `Html.ScenarioSequence` |
| `EnableHtmlScenarioUrlState (property)` | `Html.ScenarioUrlState` |
| `EnableHtmlSelectionPanel (property)` | `Html.SelectionPanel` |
| `EnableHtmlSynchronizedState (property)` | `Html.SynchronizedState` |
| `EnableHtmlViewportControls (property)` | `Html.ViewportControls` |
| `GeographicCalloutMaxItems (property)` | `Geography.CalloutMaxItems` |
| `GeographicRegionHullMaxRadius (property)` | `Geography.RegionHullMaxRadius` |
| `GeographicRegionHullMinRadius (property)` | `Geography.RegionHullMinRadius` |
| `GeographicRegionHullPadding (property)` | `Geography.RegionHullPadding` |
| `HtmlControlPlacement (property)` | `Html.ControlPlacement` |
| `HtmlNarrowMinimumHeight (property)` | `Html.NarrowMinimumHeight` |
| `HtmlNonce (property)` | `Html.Nonce` |
| `HtmlOpeningView (property)` | `Html.OpeningView` |
| `HtmlScenarioControlMode (property)` | `Html.ScenarioControlMode` |
| `HtmlSyncGroupName (property)` | `Html.SyncGroupName` |
| `HtmlWheelZoom (property)` | `Html.WheelZoom` |
| `IncludeGeographicCallouts (property)` | `Geography.IncludeCallouts` |
| `IncludeGeographicRegionHulls (property)` | `Geography.IncludeRegionHulls` |
| `PreferGeographicCalloutMapEdges (property)` | `Geography.PreferCalloutMapEdges` |

## TopologyRenderOptionsExtensions

| 4.x | 5.0 |
| --- | --- |
| `WithActiveScenario` | `.ActiveScenarioId(scenarioId)` |
| `WithEdgeMetricLabels(TopologyRenderOptions, String, String, String)` | `.IncludeEdgeLabels(true).EdgeLabelMetricKey(primaryMetricKey).EdgeSecondaryLabelMetricKey(secondaryMetricKey).EdgeTertiaryLabelMetricKey(tertiaryMetricKey)` |
| `WithFitContentToViewport(TopologyRenderOptions)` | `.FitContentToViewport(true)` |
| `WithForceGraphStyle` | `.ForceGraphStyle(...)` |
| `WithForceLayout` | `.ForceLayoutProfile(profile).ForceLayoutIterations(iterations)` |
| `WithHtmlControlPlacement` | `.Html(h => h.ControlPlacement(placement))` |
| `WithHtmlFindControls` | `.Html(html => html.FindControls(...))` |
| `WithHtmlForceGraphControls` | `.Html(html => html.ForceGraphControls(...))` |
| `WithHtmlFullscreenControl` | `.Html(html => html.FullscreenControl(...))` |
| `WithHtmlNarrowMinimumHeight(TopologyRenderOptions, Double)` | `.Html(h => h.NarrowMinimumHeight(height))` |
| `WithHtmlOpeningView` | `.Html(h => h.OpeningView(view))` |
| `WithHtmlScenarioCheckboxes` | `.Html(html => html.ScenarioControlMode(TopologyHtmlScenarioControlMode.Checkboxes))` |
| `WithHtmlScenarioControlMode` | `.Html(html => html.ScenarioControlMode(...))` |
| `WithHtmlScenarioControls` | `.Html(html => html.ScenarioControls(...))` |
| `WithHtmlScenarioPanel` | `.Html(html => html.ScenarioPanel(...))` |
| `WithHtmlScenarioSequence` | `.Html(html => html.ScenarioSequence(...))` |
| `WithHtmlScenarioUrlState` | `.Html(html => html.ScenarioUrlState(...))` |
| `WithHtmlSelectionPanel` | `.Html(html => html.SelectionPanel(...))` |
| `WithHtmlWheelZoom` | `.Html(h => h.WheelZoom(wheelZoom))` |
| `WithLuminousTopologyEdges` | `.LuminousEdges(...)` |
| `WithMindMapStyle` | `.MindMapStyle(...)` |
| `WithMonitoringDashboardStyle` | `.MonitoringDashboardStyle(...)` |
| `WithMultilineNodeLabels` | `.AllowMultilineNodeLabels(true).WrapNodeLabels(wrap).MaxNodeLabelLines(maxLabelLines).MaxNodeSubtitleLines(maxSubtitleLines)` |
| `WithNeutralGroupSurfaces(TopologyRenderOptions)` | `.GroupSurfaceStyle(global::ChartForgeX.Topology.TopologyGroupSurfaceStyle.Neutral)` |
| `WithPlainTopologyEdges` | `.EdgeVisualStyle(ChartLineVisualStyle.Plain())` |
| `WithRelationshipOverviewStyle` | `.RelationshipOverviewStyle(...)` |
| `WithRelationshipRadialFocus` | `.RelationshipRadialFocus(...)` |
| `WithRequiredIcons(TopologyRenderOptions, Boolean)` | `.RequireResolvedIcons(required)` |
| `WithSelectedEdge` | `.SelectedEdgeId(...)` |
| `WithSelectedGroup` | `.SelectedGroupId(...)` |
| `WithSelectedNode` | `.SelectedNodeId(...)` |
| `WithStateColorsPinnedInForcedColors(TopologyRenderOptions, Boolean)` | `.PinStateColorsInForcedColors(pin)` |
| `WithTopologyEdgeVisualStyle(TopologyRenderOptions, ChartLineVisualStyle)` | `.EdgeVisualStyle(style)` |
| `WithoutActiveScenario(TopologyRenderOptions)` | `.ActiveScenarioId(null)` |

## TreeViewBlock

| 4.x | 5.0 |
| --- | --- |
| `WithNaturalSize` | `.NaturalSize(...)` |

## VisualAccessibility

| 4.x | 5.0 |
| --- | --- |
| `AsDecorative(Boolean)` | `.Decorative(decorative)` |
| `WithTextAlternative` | `.TextAlternative(...)` |

## VisualBlock

| 4.x | 5.0 |
| --- | --- |
| `ConfigureTheme` | `.Theme(...)` |
| `WithPadding` | `.Padding(...)` |
| `WithSize` | `.Size(...)` |
| `WithTheme` | `.Theme(theme).ThemeMode(mode)` |

## VisualBlock`1

| 4.x | 5.0 |
| --- | --- |
| `WithCard(Boolean)` | `.ShowCard(enabled)` |
| `WithHostFrame(Boolean)` | `.HostOwnsFrame(hostOwnsFrame)` |
| `WithPngOutputScale(Int32)` | `.PngOutputScale(scale)` |
| `WithSubtitle(String)` | `.Subtitle(subtitle)` |
| `WithSvgColorVariables(SvgColorVariables)` | `.SvgColorVariables(variables)` |
| `WithThemeMode(ThemeMode)` | `.ThemeMode(mode)` |
| `WithTitle(String)` | `.Title(title)` |
| `WithTransparentBackground(Boolean)` | `.TransparentBackground(enabled)` |

## VisualCanvas

| 4.x | 5.0 |
| --- | --- |
| `AsDecorative(Boolean)` | `.Accessibility(a => a.Decorative(decorative))` |
| `ConfigureAccessibility(VisualAccessibility})` | `.Accessibility(configure)` |
| `ConfigureStyle(VisualCanvasStyleOptions})` | `.Style(configure)` |
| `WithBackdrop(VisualCanvasBackdropStyle)` | `.BackdropStyle(style)` |
| `WithBackground(ChartColor)` | `.BackgroundTop(color).BackgroundBottom(color)` |
| `WithBackground(ChartColor, ChartColor)` | `.BackgroundTop(top).BackgroundBottom(bottom)` |
| `WithFixedLayout` | `.FixedLayout(...)` |
| `WithOutputSize(Int32, Int32)` | `.Width(width).Height(height)` |
| `WithPngOutputScale(Int32)` | `.PngOutputScale(scale)` |
| `WithResponsiveLayout` | `.ResponsiveLayout(...)` |
| `WithStyle(VisualCanvasStyleOptions)` | `.Style(style)` |
| `WithTheme` | `.Theme(theme).ThemeMode(mode)` |
| `WithThemeMode(ThemeMode)` | `.ThemeMode(mode)` |
| `WithTitle(String)` | `.Title(title)` |

## VisualCanvasInfoTileLayer

| 4.x | 5.0 |
| --- | --- |
| `WithMiniChart` | `.MiniChart(...)` |
| `WithSparkline` | `.Sparkline(...)` |
| `WithTextFitPolicy(VisualCanvasTextFitPolicy)` | `.TextFitPolicy(policy)` |

## VisualGrid

| 4.x | 5.0 |
| --- | --- |
| `WithAdaptiveRowHeights(Boolean)` | `.AdaptiveRowHeights(enabled)` |
| `WithColumns(Int32)` | `.Columns(columns)` |
| `WithFrame(Boolean)` | `.FrameVisible(visible)` |
| `WithGap(Int32)` | `.Gap(gap)` |
| `WithPadding(Int32)` | `.Padding(padding)` |
| `WithPanelFit(VisualPanelFit)` | `.PanelFit(fit)` |
| `WithPanelSize` | `.PanelSize(...)` |
| `WithPngOutputScale(Int32)` | `.PngOutputScale(scale)` |
| `WithSubtitle(String)` | `.Subtitle(subtitle)` |
| `WithTheme` | `.Theme(theme).ThemeMode(mode)` |
| `WithThemeMode(ThemeMode)` | `.ThemeMode(mode)` |
| `WithTitle(String)` | `.Title(title)` |

## VisualMotionCue

| 4.x | 5.0 |
| --- | --- |
| `WithDistance(Double)` | `.DistancePixels(pixels)` |
| `WithEasing(VisualMotionEasing)` | `.Easing(easing)` |
| `WithTiming` | `.Timing(...)` |

## VisualSeriesDefaults

| 4.x | 5.0 |
| --- | --- |
| `ConfigureDataLabelStyle(TextStyleOverride})` | `.DataLabelStyle(configure)` |
| `ConfigureMarkers(ChartMarkerOptions})` | `.Markers(configure)` |

## VisualStory

| 4.x | 5.0 |
| --- | --- |
| `WithDescription(String)` | `.Description(description)` |
| `WithFormat` | `.Format(...)` |
| `WithPanelReflow(Boolean)` | `.ReflowPanels(enabled)` |
| `WithSize` | `.Size(...)` |
| `WithStyle(VisualStoryStyleOptions)` | `.Style(style)` |
| `WithTheme` | `.Theme(theme).ThemeMode(mode)` |
| `WithThemeMode(ThemeMode)` | `.ThemeMode(mode)` |

## VisualStoryAnimationOptions

| 4.x | 5.0 |
| --- | --- |
| `WithEndHold(Double)` | `.EndHoldSeconds(seconds)` |
| `WithFramesPerSecond(Int32)` | `.FramesPerSecond(framesPerSecond)` |
| `WithLoop(Boolean)` | `.Loop(loop)` |
| `WithMaximumFrames(Int32)` | `.MaximumFrames(maximumFrames)` |
| `WithOutputScale(Int32)` | `.OutputScale(scale)` |
| `WithTransition(Double)` | `.TransitionSeconds(seconds)` |

## VisualThemeBuilder

| 4.x | 5.0 |
| --- | --- |
| `ConfigureSeries` | `.Series(...)` |

## VisualWatermarkDecoration

| 4.x | 5.0 |
| --- | --- |
| `WithWatermarks` | `.AddWatermarks(...)` |

## VisualsExtensions

| 4.x | 5.0 |
| --- | --- |
| `ConfigureTheme` | `.Theme(...)` |
| `WithPngOutputScale` | `.PngOutputScale((int)scale)` |

## WardleyMapBlock

| 4.x | 5.0 |
| --- | --- |
| `WithAnnotationsBox` | `.AnnotationsBox(...)` |
| `WithStageBoundaries` | `.StageBoundaries(...)` |
| `WithStyle(WardleyMapStyle)` | `.Style(style)` |
| `WithoutAnnotationsBox()` | `.AnnotationsBox((WardleyMapAnnotationBox?)null)` |

## WorkloadListBlock

| 4.x | 5.0 |
| --- | --- |
| `WithDividers(Boolean)` | `.ShowDividers(enabled)` |
| `WithProgressRails(Boolean)` | `.ShowProgressRails(enabled)` |
