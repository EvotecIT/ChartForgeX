using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualMapCompiler {
    /// <summary>Captures one retained map dataset's independent scalar/color facts for shared geometry and keys.</summary>
    internal sealed class MapDataset {
        internal MapDataset(Chart chart, int index, ChartColorPresentation? presentation = null) {
            Chart = chart; Index = index; Series = chart.Series[index];
            Scale = Series.ColorScale ?? (Series.Kind is ChartSeriesKind.RegionMap or ChartSeriesKind.TileMap ? chart.Options.MapColorScale : null);
            Presentation = presentation ?? new ChartColorPresentation(Series.ColorDimension.HasOverrides
                ? Series.ColorDimension : chart.Options.MapColorDimension, chart.Options.Labels, chart.Options.Labels.Value, chart.Options.ValueFormat);
            ColorValues = Series.Kind is ChartSeriesKind.RegionMap or ChartSeriesKind.TileMap ? Series.RegionValues.Select(item => (double?)item.Value).ToArray()
                : Series.MapPoints.Select(item => item.ColorValue).ToArray();
            var values = ColorValues.Where(value => value.HasValue).Select(value => value!.Value).ToArray();
            Minimum = values.Length > 0 ? values.Min() : 0; Maximum = values.Length > 0 ? values.Max() : 0;
            HasValues = values.Length > 0;
            Missing = Series.Kind == ChartSeriesKind.RegionMap ? Series.RegionValues.Count < Series.RegionMapDefinition!.Regions.Count
                : Series.Kind == ChartSeriesKind.TileMap ? Series.RegionValues.Count < Series.TileMapDefinition!.Regions.Count : values.Length < ColorValues.Length;
            Weights = Series.MapPoints.Select(item => item.Weight).ToArray();
            var weights = Weights.Where(value => value.HasValue).Select(value => value!.Value).ToArray();
            MinimumWeight = weights.Length > 0 ? weights.Min() : 0; MaximumWeight = weights.Length > 0 ? weights.Max() : 0;
            if (Series.Kind == ChartSeriesKind.DottedMap) SizeScale = new ChartMapPointSizeScale(chart, Series, MinimumWeight, MaximumWeight, Weights);
        }
        private readonly List<MapCluster> _clusters = new();
        private Dictionary<int, MapCluster> _clusterSources = new();
        internal IReadOnlyList<MapCluster> Clusters => _clusters;
        internal bool IsClustered(int source) => _clusterSources.ContainsKey(source);
        internal MapCluster? ClusterFor(int source) => _clusterSources.TryGetValue(source, out var cluster) ? cluster : null;

        /// <summary>Captures deterministic map-origin grid membership without changing the raw dataset or scalar domains.</summary>
        internal void CaptureClusters(ChartRect map, Func<ChartPoint, ChartPoint> project, Func<ChartPoint, bool> visible) {
            _clusters.Clear(); _clusterSources.Clear();
            var policy = Series.MapClustering;
            if (policy == null) return;
            var cells = new Dictionary<(double X, double Y), List<int>>();
            var projected = new Dictionary<int, ChartPoint>();
            for (var index = 0; index < Series.RenderPoints.Count; index++) {
                var source = Series.RenderPoints[index];
                if (!visible(source)) continue;
                var point = project(source);
                var x = Math.Floor((point.X - map.Left) / policy.CellSize);
                var y = Math.Floor((point.Y - map.Top) / policy.CellSize);
                if (!Finite(x) || !Finite(y) || Math.Abs(x) > 9007199254740991d || Math.Abs(y) > 9007199254740991d) throw new InvalidOperationException("Map clustering cells must map into finite logical coordinates.");
                var key = (x, y);
                if (!cells.TryGetValue(key, out var members)) cells.Add(key, members = new List<int>());
                members.Add(index); projected.Add(index, point);
            }
            foreach (var cell in cells.OrderBy(cell => cell.Key.Y).ThenBy(cell => cell.Key.X)) {
                if (cell.Value.Count < policy.MinimumCount) continue;
                // Geometry order is independent of source ordering; contributors retain their actual source indices.
                var ordered = cell.Value.OrderBy(index => projected[index].X).ThenBy(index => projected[index].Y).ToArray();
                var x = map.Left; var y = map.Top;
                foreach (var index in ordered) { x += (projected[index].X - map.Left) / ordered.Length; y += (projected[index].Y - map.Top) / ordered.Length; }
                var cluster = new MapCluster(N(cell.Key.X) + ":" + N(cell.Key.Y), new ChartPoint(x, y), cell.Value.ToArray());
                _clusters.Add(cluster); foreach (var index in cluster.Sources) _clusterSources.Add(index, cluster);
            }
        }

        internal Chart Chart { get; }
        internal ChartSeries Series { get; }
        internal int Index { get; }
        internal ChartColorScale? Scale { get; }
        private ChartColorScale? _keyScale;
        internal ChartColorScale? ResolveKeyScale(VisualThemeColors colors) {
            if (Scale != null) return Scale;
            if (Series.Kind is not (ChartSeriesKind.RegionMap or ChartSeriesKind.TileMap)) return null;
            if (_keyScale != null) return _keyScale;
            var low = ChartHeatmapSurface.MapScaleValue(Chart, Minimum, Maximum, 0);
            var high = ChartHeatmapSurface.MapScaleValue(Chart, Minimum, Maximum, 1);
            ChartColor Color(double value) => ChartHeatmapSurface.MapBlend(Chart, colors, null, High(Series, colors), value, Minimum, Maximum).Color;
            var stops = new List<ChartColorStop>();
            for (var index = 0; index < 5; index++) {
                var value = ChartMath.InterpolateRange(low, high, index / 4d);
                if (stops.Count == 0 || value > stops[stops.Count - 1].Value) stops.Add(new ChartColorStop(value, Color(value)));
            }
            _keyScale = stops.Count < 2 ? ChartColorScale.Sequential(Color(low), Color(low)) : ChartColorScale.Sequential(stops);
            return _keyScale;
        }
        internal ChartColorPresentation Presentation { get; }
        internal ChartMapPointSizeScale? SizeScale { get; }
        internal double?[] ColorValues { get; }
        internal double?[] Weights { get; }
        internal double Minimum { get; }
        internal double Maximum { get; }
        internal double MinimumWeight { get; }
        internal double MaximumWeight { get; }
        internal bool HasValues { get; }
        internal bool Missing { get; }
        internal bool HasDomain => ChartColorScaleSurface.HasDomain(Scale, HasValues ? Minimum : null, HasValues ? Maximum : null);
        internal string Prefix => "series-" + N(Index);

        internal Dictionary<string, int> RegionIndices() => Values(Series,
            key => Series.Kind == ChartSeriesKind.TileMap
                ? Series.TileMapDefinition!.TryResolveRegion(key, out var tileCode) ? tileCode : null
                : Series.RegionMapDefinition!.TryResolveRegion(key, out var code) ? code : null);

        internal ChartColorBlend RegionFill(VisualThemeColors colors, int? index) {
            if (!index.HasValue) return ChartColorScaleSurface.NoData(Scale, colors);
            var point = index.Value;
            var authored = point < Series.PointColors.Count && Series.PointColors[point].HasValue ? Series.PointColors[point] : Series.RegionValues[point].Color;
            return authored.HasValue ? ChartColorBlend.Solid(authored.Value, SvgColorRole.Series)
                : Scale?.BlendFor(ColorValues[point]!.Value, Minimum, Maximum) ?? ChartHeatmapSurface.ColorBlend(Chart, colors,
                    High(Series, colors), ColorValues[point]!.Value, Minimum, Maximum, VisualChartPaint.SeriesRole(Series));
        }

        internal (ChartColor Color, SvgPaint Paint) PointPaint(int index, VisualThemeColors colors) {
            var authored = index < Series.PointColors.Count ? Series.PointColors[index] : null;
            authored ??= Series.MapPoints[index].Color;
            if (Scale == null || authored.HasValue) return DottedPaint(Series, index, colors);
            var blend = ColorValues[index].HasValue ? Scale.BlendFor(ColorValues[index]!.Value, Minimum, Maximum) : ChartColorScaleSurface.NoData(Scale, colors);
            return (blend.Color, blend.Paint);
        }

        internal Dictionary<string, string> RegionSource(string code, string name, int? source, string full) {
            var value = source.HasValue ? ColorValues[source.Value] : null;
            var metadata = new Dictionary<string, string> {
                ["data-cfx-series"] = N(Index), ["data-cfx-region"] = code, ["data-cfx-region-name"] = name, ["data-cfx-label"] = name,
                ["data-cfx-point"] = source.HasValue ? N(source.Value) : "-1", ["data-cfx-empty"] = source.HasValue ? "false" : "true",
                ["data-cfx-value"] = value.HasValue ? N(value.Value) : "", ["data-cfx-formatted-value"] = full, ["aria-label"] = name + " (" + code + "): " + full
            };
            if (Presentation.HasOverrides) metadata["data-cfx-label-value"] = Presentation.Name;
            if (!value.HasValue) metadata["data-cfx-status"] = "empty";
            else {
                var ratio = Scale == null ? ChartHeatmapSurface.MapRatio(Chart, value.Value, Minimum, Maximum)
                    : Scale.Mode == ChartColorScaleMode.Discrete ? Scale.Bands.Count == 1 ? 0 : Scale.BandIndex(value.Value) / (double)(Scale.Bands.Count - 1)
                    : Scale.DomainRatio(value.Value, Minimum, Maximum);
                metadata["data-cfx-level"] = N(ChartHeatmapSurface.Level(ratio));
                if (Scale?.Mode == ChartColorScaleMode.Discrete) {
                    var band = Scale.BandIndex(value.Value); metadata["data-cfx-band"] = N(band);
                    if (Scale.Bands[band].Label != null) metadata["data-cfx-band-label"] = Scale.Bands[band].Label!;
                }
                var status = Scale == null ? ChartHeatmapSurface.CellStatus(Chart, ratio) : null;
                if (status != null) metadata["data-cfx-status"] = status;
            }
            return metadata;
        }

        internal void AddColorFacts(Dictionary<string, string> metadata, int index) {
            if (Scale == null && !ColorValues[index].HasValue) return;
            metadata["data-cfx-label-color"] = Presentation.Name;
            if (ColorValues[index] is double value) {
                metadata["data-cfx-color-value"] = N(value);
                metadata["data-cfx-formatted-color-value"] = Presentation.Value(value);
            } else metadata["data-cfx-color-missing"] = "true";
        }
    }
}
