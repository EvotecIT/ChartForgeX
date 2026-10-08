using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Topology;

internal static partial class TopologyDenseRoutePlanner {
    /// <summary>
    /// A sparse grid whose lines run just outside every obstacle and through every terminal stub. It records which
    /// moves an obstacle blocks and how many routes already use each grid segment.
    /// </summary>
    /// <remarks>
    /// The per-point arrays live in a <see cref="GridBuffers"/> that one plan reuses for every grid it builds, so the
    /// repair passes do not allocate a new set of large arrays each time. Only the grid built last may be used.
    /// </remarks>
    private sealed partial class Grid {
        internal const byte BlockedFlag = 1;
        internal const byte NoRightFlag = 2;
        internal const byte NoDownFlag = 4;
        internal const byte BorderRightFlag = 8;
        internal const byte BorderDownFlag = 16;

        private readonly GridBuffers _buffers;
        private readonly byte[] _flags;
        private readonly int[] _usedRight;
        private readonly int[] _usedDown;
        private readonly int[] _crossRight;
        private readonly int[] _crossDown;

        private Grid(double[] xs, double[] ys, GridBuffers buffers) {
            Xs = xs;
            Ys = ys;
            Width = xs.Length;
            Height = ys.Length;
            _buffers = buffers;
            buffers.Prepare(this, Width * Height);
            _flags = buffers.Flags;
            _usedRight = buffers.UsedRight;
            _usedDown = buffers.UsedDown;
            _crossRight = buffers.CrossRight;
            _crossDown = buffers.CrossDown;
            GapX = Gaps(xs);
            GapY = Gaps(ys);
        }

        public double[] Xs { get; }
        public double[] Ys { get; }
        /// <summary>Distance between each column and the next; equal to the length of a horizontal move either way.</summary>
        public double[] GapX { get; }
        /// <summary>Distance between each row and the next; equal to the length of a vertical move either way.</summary>
        public double[] GapY { get; }
        public int Width { get; }
        public int Height { get; }
        internal byte[] Flags => _flags;
        internal int[] UsedRight => _usedRight;
        internal int[] UsedDown => _usedDown;
        internal int[] CrossRight => _crossRight;
        internal int[] CrossDown => _crossDown;
        internal GridBuffers Buffers => _buffers;

        public bool IsBlocked(int cell) => (_flags[cell] & BlockedFlag) != 0;

        public static Grid? Create(Scene scene, List<Request> requests, List<List<ChartPoint>> fixedRoutes, GridBuffers buffers) {
            var region = scene.Region;
            var xs = new SortedSet<double> { region.Left, region.Right };
            var ys = new SortedSet<double> { region.Top, region.Bottom };
            foreach (var route in fixedRoutes) {
                for (var i = 0; i + 1 < route.Count; i++) {
                    var a = route[i];
                    var b = route[i + 1];
                    if (Math.Abs(a.X - b.X) < 0.01) {
                        AddLine(xs, a.X, region.Left, region.Right);
                        AddLine(xs, a.X - 8, region.Left, region.Right);
                        AddLine(xs, a.X + 8, region.Left, region.Right);
                    }
                    if (Math.Abs(a.Y - b.Y) < 0.01) {
                        AddLine(ys, a.Y, region.Top, region.Bottom);
                        AddLine(ys, a.Y - 8, region.Top, region.Bottom);
                        AddLine(ys, a.Y + 8, region.Top, region.Bottom);
                    }
                }
            }
            foreach (var obstacle in scene.Obstacles) {
                var box = obstacle.Box.Expand(Clearance);
                AddLine(xs, box.Left - 1, region.Left, region.Right);
                AddLine(xs, box.Right + 1, region.Left, region.Right);
                AddLine(ys, box.Top - 1, region.Top, region.Bottom);
                AddLine(ys, box.Bottom + 1, region.Top, region.Bottom);
            }

            // A lane on each side of every group border, so a route can run beside a border instead of on it.
            foreach (var group in scene.Groups) {
                AddLine(xs, group.Left - BorderLane, region.Left, region.Right);
                AddLine(xs, group.Left + BorderLane, region.Left, region.Right);
                AddLine(xs, group.Right - BorderLane, region.Left, region.Right);
                AddLine(xs, group.Right + BorderLane, region.Left, region.Right);
                AddLine(ys, group.Top - BorderLane, region.Top, region.Bottom);
                AddLine(ys, group.Top + BorderLane, region.Top, region.Bottom);
                AddLine(ys, group.Bottom - BorderLane, region.Top, region.Bottom);
                AddLine(ys, group.Bottom + BorderLane, region.Top, region.Bottom);
            }

            foreach (var request in requests) {
                foreach (var terminal in request.Starts) { xs.Add(terminal.Stub.X); ys.Add(terminal.Stub.Y); }
                foreach (var terminal in request.Ends) { xs.Add(terminal.Stub.X); ys.Add(terminal.Stub.Y); }
            }

            var stubXs = new HashSet<double> { region.Left, region.Right };
            var stubYs = new HashSet<double> { region.Top, region.Bottom };
            foreach (var request in requests) {
                foreach (var terminal in request.Starts) { stubXs.Add(terminal.Stub.X); stubYs.Add(terminal.Stub.Y); }
                foreach (var terminal in request.Ends) { stubXs.Add(terminal.Stub.X); stubYs.Add(terminal.Stub.Y); }
            }

            // Layouts whose cards do not share rows and columns produce many nearly identical lines. Merging lines that
            // are close together loses little and keeps the search bounded; blocking does not depend on which lines exist.
            var columns = ToArray(xs);
            var rows = ToArray(ys);
            foreach (var merge in LineMergeDistances) {
                if ((long)columns.Length * rows.Length <= MaximumGridPoints) break;
                columns = Thin(ToArray(xs), stubXs, merge);
                rows = Thin(ToArray(ys), stubYs, merge);
            }

            if ((long)columns.Length * rows.Length > MaximumGridPoints) return null;
            var grid = new Grid(columns, rows, buffers);
            foreach (var obstacle in scene.Obstacles) grid.Block(obstacle.Box.Expand(Clearance));
            foreach (var group in scene.Groups) grid.MarkBorder(group);
            return grid;
        }

        private static double[] Gaps(double[] lines) {
            var gaps = new double[Math.Max(0, lines.Length - 1)];
            for (var i = 0; i < gaps.Length; i++) gaps[i] = Math.Abs(lines[i + 1] - lines[i]);
            return gaps;
        }

        // Keeps every required line and drops the others that follow a kept line too closely.
        private static double[] Thin(double[] lines, HashSet<double> required, double distance) {
            var kept = new List<double>(lines.Length);
            foreach (var line in lines) {
                if (required.Contains(line) || kept.Count == 0 || line - kept[kept.Count - 1] >= distance) kept.Add(line);
            }

            return kept.ToArray();
        }

        private static double[] ToArray(SortedSet<double> values) {
            var array = new double[values.Count];
            values.CopyTo(array);
            return array;
        }

        private static void AddLine(SortedSet<double> lines, double value, double min, double max) {
            if (value < min || value > max) return;
            lines.Add(Math.Round(value * 2) / 2);
        }

        private void Block(Box box) {
            var firstColumn = LowerBound(Xs, box.Left);
            var lastColumn = UpperBound(Xs, box.Right) - 1;
            var firstRow = LowerBound(Ys, box.Top);
            var lastRow = UpperBound(Ys, box.Bottom) - 1;
            for (var row = firstRow; row <= lastRow; row++) {
                for (var column = firstColumn; column <= lastColumn; column++) _flags[row * Width + column] |= BlockedFlag;
                // A move is blocked when it enters, leaves, or jumps across the box.
                for (var column = Math.Max(0, firstColumn - 1); column <= lastColumn && column + 1 < Width; column++) _flags[row * Width + column] |= NoRightFlag;
            }

            for (var column = firstColumn; column <= lastColumn; column++) {
                for (var row = Math.Max(0, firstRow - 1); row <= lastRow && row + 1 < Height; row++) _flags[row * Width + column] |= NoDownFlag;
            }
        }

        private void MarkBorder(Box group) {
            var firstColumn = LowerBound(Xs, group.Left - BorderReach);
            var lastColumn = UpperBound(Xs, group.Right + BorderReach) - 1;
            var firstRow = LowerBound(Ys, group.Top - BorderReach);
            var lastRow = UpperBound(Ys, group.Bottom + BorderReach) - 1;
            for (var row = firstRow; row <= lastRow; row++) {
                if (Math.Abs(Ys[row] - group.Top) > BorderReach && Math.Abs(Ys[row] - group.Bottom) > BorderReach) continue;
                for (var column = firstColumn; column < lastColumn; column++) _flags[row * Width + column] |= BorderRightFlag;
            }

            for (var column = firstColumn; column <= lastColumn; column++) {
                if (Math.Abs(Xs[column] - group.Left) > BorderReach && Math.Abs(Xs[column] - group.Right) > BorderReach) continue;
                for (var row = firstRow; row < lastRow; row++) _flags[row * Width + column] |= BorderDownFlag;
            }
        }

        /// <summary>Returns the grid point at the given position, or -1 when no grid lines meet there.</summary>
        public int Cell(ChartPoint point) {
            var column = Array.BinarySearch(Xs, point.X);
            var row = Array.BinarySearch(Ys, point.Y);
            return column < 0 || row < 0 ? -1 : row * Width + column;
        }

        /// <summary>Returns how many earlier routes pass through a grid point across the given direction of travel.</summary>
        public int CrossingsAt(int cell, int direction) {
            if (direction < 2) {
                var above = cell >= Width ? _usedDown[cell - Width] : 0;
                return Math.Min(MaximumSharedRoutes, Math.Max(above, _usedDown[cell]));
            }

            var before = cell % Width > 0 ? _usedRight[cell - 1] : 0;
            return Math.Min(MaximumSharedRoutes, Math.Max(before, _usedRight[cell]));
        }

        /// <summary>Starts a search: costs, heuristics and route ends recorded by earlier searches no longer count.</summary>
        public int BeginSearch() {
            if (!ReferenceEquals(_buffers.Owner, this)) throw new InvalidOperationException("A newer grid of the plan reuses this grid's buffers.");
            return ++_buffers.Search;
        }

        public double CostOf(int state) {
            ref var node = ref _buffers.Nodes[state];
            return node.Stamp == _buffers.Search ? node.Cost : double.PositiveInfinity;
        }

        public int PreviousOf(int state) {
            ref var node = ref _buffers.Nodes[state];
            return node.Stamp == _buffers.Search ? node.Previous : -1;
        }

        public void SetCost(int state, double cost, int previous) {
            ref var node = ref _buffers.Nodes[state];
            node.Stamp = _buffers.Search;
            node.Cost = cost;
            node.Previous = previous;
        }

        private static int LowerBound(double[] values, double value) {
            var index = Array.BinarySearch(values, value);
            return index >= 0 ? index : ~index;
        }

        private static int UpperBound(double[] values, double value) {
            var index = Array.BinarySearch(values, value);
            return index >= 0 ? index + 1 : ~index;
        }
    }

    /// <summary>The cost, predecessor and search stamp of one grid point and direction of arrival.</summary>
    internal struct SearchNode {
        public double Cost;
        public int Previous;
        public int Stamp;
    }

    /// <summary>
    /// The large per-point arrays of a plan's grids, reused by every grid and search the plan makes. Search stamps keep
    /// counting across grids, so a reused cost, heuristic or route-end entry never looks current to a later search.
    /// </summary>
    internal sealed class GridBuffers {
        public byte[] Flags = Array.Empty<byte>();
        public int[] UsedRight = Array.Empty<int>();
        public int[] UsedDown = Array.Empty<int>();
        public int[] CrossRight = Array.Empty<int>();
        public int[] CrossDown = Array.Empty<int>();
        public SearchNode[] Nodes = Array.Empty<SearchNode>();
        public double[] Heuristic = Array.Empty<double>();
        public int[] HeuristicStamp = Array.Empty<int>();
        public int[] EndStamp = Array.Empty<int>();
        public int[] EndSlot = Array.Empty<int>();
        public MinHeap Queue { get; } = new();
        public int Search;
        public object? Owner;

        /// <summary>Hands the buffers to a new grid of <paramref name="points"/> points with no blocks or routes.</summary>
        public void Prepare(object owner, int points) {
            Owner = owner;
            if (Flags.Length < points) {
                Flags = new byte[points];
                UsedRight = new int[points];
                UsedDown = new int[points];
                CrossRight = new int[points];
                CrossDown = new int[points];
                Heuristic = new double[points];
                HeuristicStamp = new int[points];
                EndStamp = new int[points];
                EndSlot = new int[points];
                Nodes = new SearchNode[checked(points * 4)];
                return;
            }

            Array.Clear(Flags, 0, points);
            Array.Clear(UsedRight, 0, points);
            Array.Clear(UsedDown, 0, points);
            Array.Clear(CrossRight, 0, points);
            Array.Clear(CrossDown, 0, points);
        }
    }
}
