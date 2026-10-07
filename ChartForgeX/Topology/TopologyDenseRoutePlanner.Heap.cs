using System;

namespace ChartForgeX.Topology;

internal static partial class TopologyDenseRoutePlanner {
    /// <summary>
    /// A four-ary min-heap of search states ordered by priority, then by state index; framework priority queues are
    /// unavailable on net472 and netstandard2.0.
    /// </summary>
    /// <remarks>
    /// The order is total for distinct states, so the heap pops states in exactly the order a binary heap with the same
    /// comparison would: equal keys can only be two entries of one state, and the search keeps one of them and skips the
    /// other whichever comes first. Entries are kept in parallel arrays and the heap is reused by every search of a plan.
    /// </remarks>
    internal sealed class MinHeap {
        private int[] _states = new int[256];
        private double[] _priorities = new double[256];
        private double[] _costs = new double[256];

        public int Count { get; private set; }

        public void Clear() => Count = 0;

        public void Push(int state, double priority, double cost) {
            if (Count == _states.Length) Grow();
            var states = _states;
            var priorities = _priorities;
            var costs = _costs;
            var index = Count++;
            while (index > 0) {
                var parent = (index - 1) >> 2;
                var parentPriority = priorities[parent];
                if (parentPriority < priority || parentPriority == priority && states[parent] <= state) break;
                states[index] = states[parent];
                priorities[index] = parentPriority;
                costs[index] = costs[parent];
                index = parent;
            }

            states[index] = state;
            priorities[index] = priority;
            costs[index] = cost;
        }

        /// <summary>Removes the smallest entry. The heap must not be empty.</summary>
        public void Pop(out int state, out double priority, out double cost) {
            var states = _states;
            var priorities = _priorities;
            var costs = _costs;
            state = states[0];
            priority = priorities[0];
            cost = costs[0];
            var count = --Count;
            if (count == 0) return;
            var lastState = states[count];
            var lastPriority = priorities[count];
            var lastCost = costs[count];
            var index = 0;
            while (true) {
                var first = (index << 2) + 1;
                if (first >= count) break;
                var smallest = first;
                var smallestPriority = priorities[first];
                var smallestState = states[first];
                var end = Math.Min(first + 4, count);
                for (var child = first + 1; child < end; child++) {
                    var childPriority = priorities[child];
                    if (childPriority < smallestPriority || childPriority == smallestPriority && states[child] < smallestState) {
                        smallest = child;
                        smallestPriority = childPriority;
                        smallestState = states[child];
                    }
                }

                if (lastPriority < smallestPriority || lastPriority == smallestPriority && lastState <= smallestState) break;
                // Move the hole down instead of swapping two complete entries at every level.
                states[index] = smallestState;
                priorities[index] = smallestPriority;
                costs[index] = costs[smallest];
                index = smallest;
            }

            states[index] = lastState;
            priorities[index] = lastPriority;
            costs[index] = lastCost;
        }

        private void Grow() {
            var size = checked(_states.Length * 2);
            Array.Resize(ref _states, size);
            Array.Resize(ref _priorities, size);
            Array.Resize(ref _costs, size);
        }
    }
}
