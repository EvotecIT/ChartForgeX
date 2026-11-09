using System;
using System.Collections.Generic;

namespace ChartForgeX.Topology;

internal static partial class TopologyDenseRoutePlanner {
    /// <summary>
    /// The obstacles sorted by their left and by their top edge. A question about a strip across one axis then visits
    /// only the obstacles that can reach the strip, instead of every card of a large chart.
    /// </summary>
    private sealed class ObstacleStrips {
        private readonly Box[] _byLeft;
        private readonly double[] _lefts;
        private readonly double _widest;
        private readonly Box[] _byTop;
        private readonly double[] _tops;
        private readonly double _tallest;

        public ObstacleStrips(List<Obstacle> obstacles) {
            var usable = new List<Box>(obstacles.Count);
            // A box with an undefined edge compares false against everything, so it never stands between two runs.
            foreach (var obstacle in obstacles) {
                var box = obstacle.Box;
                if (!double.IsNaN(box.Left + box.Top + box.Right + box.Bottom)) usable.Add(box);
                else HasUndefined = true;
            }

            _byLeft = new Box[usable.Count];
            _lefts = new double[usable.Count];
            _byTop = new Box[usable.Count];
            _tops = new double[usable.Count];
            for (var i = 0; i < usable.Count; i++) {
                var box = usable[i];
                _byLeft[i] = _byTop[i] = box;
                _lefts[i] = box.Left;
                _tops[i] = box.Top;
                _widest = Math.Max(_widest, box.Right - box.Left);
                _tallest = Math.Max(_tallest, box.Bottom - box.Top);
            }

            Array.Sort(_lefts, _byLeft);
            Array.Sort(_tops, _byTop);
        }

        /// <summary>
        /// Returns true when an obstacle lies across the strip between <paramref name="low"/> and <paramref name="high"/>
        /// (x for a vertical strip) and overlaps the stretch from <paramref name="from"/> to <paramref name="to"/> along it.
        /// </summary>
        public bool Between(bool vertical, double low, double high, double from, double to) {
            var boxes = vertical ? _byLeft : _byTop;
            var nears = vertical ? _lefts : _tops;
            // An obstacle reaching past low starts no further back than its own size; one pixel absorbs rounding.
            var index = FirstAtOrAfter(nears, low - (vertical ? _widest : _tallest) - 1);
            for (; index < boxes.Length && nears[index] < high; index++) {
                var box = boxes[index];
                var far = vertical ? box.Right : box.Bottom;
                var boxFrom = vertical ? box.Top : box.Left;
                var boxTo = vertical ? box.Bottom : box.Right;
                if (far > low && boxTo > from && boxFrom < to) return true;
            }

            return false;
        }

        /// <summary>True when an obstacle has an undefined edge; questions whose answer such a box changes scan every obstacle.</summary>
        public bool HasUndefined { get; }

        /// <summary>
        /// The obstacles sorted along the run direction (by top for a vertical run) and the first one whose extent can
        /// reach <paramref name="from"/>. A caller visits them while their start is no later than its stretch's end.
        /// </summary>
        public int Along(bool vertical, double from, out Box[] boxes, out double[] starts) {
            boxes = vertical ? _byTop : _byLeft;
            starts = vertical ? _tops : _lefts;
            return FirstAtOrAfter(starts, from - (vertical ? _tallest : _widest) - 1);
        }

        /// <summary>The obstacles sorted by left edge and the first one whose width can reach <paramref name="x"/>.</summary>
        public int Across(double x, out Box[] boxes, out double[] lefts) {
            boxes = _byLeft;
            lefts = _lefts;
            return FirstAtOrAfter(lefts, x - _widest - 1);
        }

        private static int FirstAtOrAfter(double[] values, double value) {
            int low = 0, high = values.Length;
            while (low < high) {
                var middle = low + (high - low) / 2;
                if (values[middle] < value) low = middle + 1;
                else high = middle;
            }

            return low;
        }
    }
}
