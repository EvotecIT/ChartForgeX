using System;

namespace ChartForgeX.Core;

public sealed partial class ChartOptions {
    private ChartDataLabelOptions _dataLabels = new();

    /// <summary>Gets or sets the data label visibility, placement and leader-line connectors.</summary>
    public ChartDataLabelOptions DataLabels {
        get => _dataLabels;
        set => _dataLabels = value ?? throw new ArgumentNullException(nameof(value));
    }

    private ChartBarOptions _bars = new();

    /// <summary>Gets or sets the bar layout, stacking and styling.</summary>
    public ChartBarOptions Bars {
        get => _bars;
        set => _bars = value ?? throw new ArgumentNullException(nameof(value));
    }

    private ChartLineOptions _lines = new();

    /// <summary>Gets or sets the line styling, markers and orientation.</summary>
    public ChartLineOptions Lines {
        get => _lines;
        set => _lines = value ?? throw new ArgumentNullException(nameof(value));
    }

    private ChartHeatmapOptions _heatmap = new();

    /// <summary>Gets or sets the heatmap scales, cells and labels.</summary>
    public ChartHeatmapOptions Heatmap {
        get => _heatmap;
        set => _heatmap = value ?? throw new ArgumentNullException(nameof(value));
    }

    private ChartMapOptions _map = new();

    /// <summary>Gets or sets the map viewport, layers, labels and colour scales.</summary>
    public ChartMapOptions Map {
        get => _map;
        set => _map = value ?? throw new ArgumentNullException(nameof(value));
    }

    private ChartPictorialOptions _pictorial = new();

    /// <summary>Gets or sets the pictorial symbols, values and artwork.</summary>
    public ChartPictorialOptions Pictorial {
        get => _pictorial;
        set => _pictorial = value ?? throw new ArgumentNullException(nameof(value));
    }

    private ChartProgressOptions _progress = new();

    /// <summary>Gets or sets the progress bar and ring presentation.</summary>
    public ChartProgressOptions Progress {
        get => _progress;
        set => _progress = value ?? throw new ArgumentNullException(nameof(value));
    }

    private ChartCircleOptions _circle = new();

    /// <summary>Gets or sets the circle and status-ring presentation.</summary>
    public ChartCircleOptions Circle {
        get => _circle;
        set => _circle = value ?? throw new ArgumentNullException(nameof(value));
    }

    private ChartPieOptions _pie = new();

    /// <summary>Gets or sets the pie and donut slices, labels and centre text.</summary>
    public ChartPieOptions Pie {
        get => _pie;
        set => _pie = value ?? throw new ArgumentNullException(nameof(value));
    }

    private ChartWordCloudOptions _wordCloud = new();

    /// <summary>Gets or sets the word cloud sizing, angles and density.</summary>
    public ChartWordCloudOptions WordCloud {
        get => _wordCloud;
        set => _wordCloud = value ?? throw new ArgumentNullException(nameof(value));
    }

    private ChartRasterOptions _png = new();

    /// <summary>Gets or sets the fonts, hinting and scale of raster output (PNG, JPEG, BMP, TIFF and PPM).</summary>
    public ChartRasterOptions Raster {
        get => _png;
        set => _png = value ?? throw new ArgumentNullException(nameof(value));
    }

    private ChartGanttOptions _gantt = new();

    /// <summary>Gets or sets the gantt markers, ticks and open ends.</summary>
    public ChartGanttOptions Gantt {
        get => _gantt;
        set => _gantt = value ?? throw new ArgumentNullException(nameof(value));
    }

    private ChartPolarLayoutOptions _polar = new();

    /// <summary>Gets or sets the polar axis and grid layout.</summary>
    public ChartPolarLayoutOptions Polar {
        get => _polar;
        set => _polar = value ?? throw new ArgumentNullException(nameof(value));
    }

    private ChartStateTimelineOptions _stateTimeline = new();

    /// <summary>Gets or sets the state timeline categories and summary.</summary>
    public ChartStateTimelineOptions StateTimeline {
        get => _stateTimeline;
        set => _stateTimeline = value ?? throw new ArgumentNullException(nameof(value));
    }
}
