using ChartForgeX.Primitives;
using PowerForge.Fluent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System;

namespace ChartForgeX.Core;

/// <summary>Hand-written fluent methods of <see cref="ChartSeries"/> that do more than set one property.</summary>
public static partial class ChartSeriesFluent {
    /// <summary>Atomically replaces raw group identities with exactly one unique identity per group.</summary>
    public static global::ChartForgeX.Core.ChartSeries BoxPlotGroupIds(this global::ChartForgeX.Core.ChartSeries chartSeries, global::System.Collections.Generic.IEnumerable<global::ChartForgeX.Core.ChartObservationId> ids)
        => (chartSeries ?? throw new System.ArgumentNullException(nameof(chartSeries))).SetBoxPlotGroupIds(ids);

    /// <summary>Atomically captures one bullet row's numeric bounds, scale, reversal and caption policy.</summary>
    public static global::ChartForgeX.Core.ChartSeries BulletAxis(this global::ChartForgeX.Core.ChartSeries chartSeries, global::System.Action<global::ChartForgeX.Core.ChartAxis> configure)
        => (chartSeries ?? throw new System.ArgumentNullException(nameof(chartSeries))).SetBulletAxis(configure);

    /// <summary>Configures this numeric color dataset's existing presentation options.</summary>
    /// <param name="chartSeries">The instance to configure.</param>
    /// <param name="configure">Edits this series' existing options.</param>
    /// <returns>The current series.</returns>
    public static global::ChartForgeX.Core.ChartSeries ColorDimension(this global::ChartForgeX.Core.ChartSeries chartSeries, global::System.Action<global::ChartForgeX.Core.ChartColorDimensionOptions> configure)
        => (chartSeries ?? throw new System.ArgumentNullException(nameof(chartSeries))).SetColorDimension(configure);

    /// <summary>
    /// Draws the series against an additional value axis created by <see cref="Chart.AddValueAxis"/>.
    /// </summary>
    /// <param name="chartSeries">The instance to configure.</param>
    /// <param name="axis">The additional axis of the chart this series belongs to.</param>
    /// <returns>The current series.</returns>
    public static global::ChartForgeX.Core.ChartSeries ValueAxis(this global::ChartForgeX.Core.ChartSeries chartSeries, global::ChartForgeX.Core.ChartAdditionalValueAxis axis)
        => (chartSeries ?? throw new System.ArgumentNullException(nameof(chartSeries))).SetValueAxis(axis);

    /// <summary>Configures endpoint presentation using detached callback state. A callback failure leaves the current settings unchanged.</summary>
    /// <param name="chartSeries">The instance to configure.</param>
    /// <param name="configure">Configures StartMarker and EndMarker overrides; unspecified settings inherit the shared series markers.</param>
    /// <returns>The current series.</returns>
    /// <exception cref="InvalidOperationException">The series is not a dumbbell.</exception>
    public static global::ChartForgeX.Core.ChartSeries Dumbbell(this global::ChartForgeX.Core.ChartSeries chartSeries, global::System.Action<global::ChartForgeX.Core.ChartDumbbellOptions> configure)
        => (chartSeries ?? throw new System.ArgumentNullException(nameof(chartSeries))).SetDumbbell(configure);

    /// <summary>Configures every financial observation from its source values and ordinal, installing all detached styles atomically.</summary>
    /// <param name="chartSeries">The instance to configure.</param>
    /// <param name="configure">Receives the authored prices, zero-based observation index and a detached copy of its current overrides.</param>
    /// <returns>The current series.</returns>
    /// <remarks>The callback runs once per existing observation during this call and is not retained. A failed callback or invalid OHLC style
    /// leaves all previous styles unchanged. Item updates preserve styles; complete typed replacement clears them.</remarks>
    public static global::ChartForgeX.Core.ChartSeries FinancialValues(this global::ChartForgeX.Core.ChartSeries chartSeries, global::System.Action<global::ChartForgeX.Core.ChartCandlestick, int, global::ChartForgeX.Core.ChartFinancialItemOptions> configure)
        => (chartSeries ?? throw new System.ArgumentNullException(nameof(chartSeries))).SetFinancialValues(configure);

    /// <summary>Replaces source observation identities in source order with one identity per authored observation.</summary>
    /// <remarks>The sequence is captured and validated before any existing identity changes. Decimated series accept the complete source sequence.</remarks>
    /// <param name="chartSeries">The instance to configure.</param>
    /// <param name="ids">Unique identities in source observation order.</param>
    /// <returns>The current series.</returns>
    public static global::ChartForgeX.Core.ChartSeries ObservationIds(this global::ChartForgeX.Core.ChartSeries chartSeries, global::System.Collections.Generic.IEnumerable<global::ChartForgeX.Core.ChartObservationId> ids)
        => (chartSeries ?? throw new System.ArgumentNullException(nameof(chartSeries))).SetObservationIds(ids);

    /// <summary>Sets interpolation and, for a step, its transition position.</summary>
    /// <param name="chartSeries">The instance to configure.</param>
    /// <param name="interpolation">The interpolation applied to each connected source segment.</param>
    /// <param name="stepPosition">The step transition position; other modes require End.</param>
    /// <returns>The current series.</returns>
    public static global::ChartForgeX.Core.ChartSeries Interpolation(this global::ChartForgeX.Core.ChartSeries chartSeries, global::ChartForgeX.Core.ChartInterpolation interpolation, global::ChartForgeX.Core.ChartStepPosition stepPosition = global::ChartForgeX.Core.ChartStepPosition.End)
        => (chartSeries ?? throw new System.ArgumentNullException(nameof(chartSeries))).SetInterpolation(interpolation, stepPosition);

    /// <summary>Replaces this DottedMap dataset's clustering policy without changing any observation or its identity.</summary>
    /// <param name="chartSeries">The instance to configure.</param>
    /// <param name="clustering">The immutable policy, or null to restore individual source marks.</param>
    /// <returns>The current series.</returns>
    public static global::ChartForgeX.Core.ChartSeries MapClustering(this global::ChartForgeX.Core.ChartSeries chartSeries, global::ChartForgeX.Core.ChartMapClustering? clustering)
        => (chartSeries ?? throw new System.ArgumentNullException(nameof(chartSeries))).SetMapClustering(clustering);

    /// <summary>Places a TileMap inside a multi-map scene without changing its tile coordinates or observation identity.</summary>
    /// <param name="chartSeries">The instance to configure.</param>
    /// <param name="bounds">A finite positive rectangle contained in [0,1] on both axes, or null to clear the placement.</param>
    /// <returns>The current series.</returns>
    /// <remarks>Insets require at least one RegionMap or DottedMap peer. Standalone TileMaps retain their existing layout.</remarks>
    public static global::ChartForgeX.Core.ChartSeries MapInset(this global::ChartForgeX.Core.ChartSeries chartSeries, global::ChartForgeX.Primitives.ChartRect? bounds)
        => (chartSeries ?? throw new System.ArgumentNullException(nameof(chartSeries))).SetMapInset(bounds);

    /// <summary>Configures one dotted-map dataset's marker sizes atomically, independently of its geographic coordinates and numeric color.</summary>
    /// <param name="chartSeries">The instance to configure.</param>
    /// <param name="configure">Edits detached size options; a thrown callback leaves the installed options unchanged.</param>
    /// <returns>The current series.</returns>
    public static global::ChartForgeX.Core.ChartSeries MapPointSize(this global::ChartForgeX.Core.ChartSeries chartSeries, global::System.Action<global::ChartForgeX.Core.ChartMapPointSizeOptions> configure)
        => (chartSeries ?? throw new System.ArgumentNullException(nameof(chartSeries))).SetMapPointSize(configure);
}
