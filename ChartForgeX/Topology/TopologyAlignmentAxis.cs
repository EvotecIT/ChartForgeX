using System;
using System.Collections.Generic;

namespace ChartForgeX.Topology;

/// <summary>One bounded difference-constraint axis; node-center equalities are collapsed once.</summary>
internal sealed class TopologyAlignmentAxis {
    internal readonly struct Edge {
        internal readonly int From, To;
        internal readonly double Distance;
        internal readonly string? DeclarationId;
        internal Edge(int from, int to, double distance, string? declarationId = null) { From = from; To = to; Distance = distance; DeclarationId = declarationId; }
    }
    private readonly int[] parent;
    private readonly List<Edge> edges = new();
    private readonly TopologyAlignmentBudget budget;
    internal int Count => edges.Count;
    internal string? ConflictDeclarationId { get; private set; }
    internal TopologyAlignmentAxis(int variables, TopologyAlignmentBudget budget) {
        this.budget = budget; budget.Work(variables);
        parent = new int[variables];
        for (var i = 0; i < parent.Length; i++) parent[i] = i;
    }
    private int Root(int value) {
        while (parent[value] != value) { budget.Work(); parent[value] = parent[parent[value]]; value = parent[value]; }
        return value;
    }
    internal void Equal(int first, int second) {
        budget.Work(); var a = Root(first); var b = Root(second);
        if (a != b) parent[Math.Max(a, b)] = Math.Min(a, b);
    }
    internal void Add(int from, int to, double distance, string? declarationId = null) {
        budget.Work();
        if (double.IsNaN(distance) || double.IsInfinity(distance) || distance < 0)
            throw new InvalidOperationException("Alignment distances must be non-negative finite native dimensions.");
        edges.Add(new Edge(from, to, distance, declarationId));
    }
    internal void Rollback(int count) {
        budget.Work(edges.Count - count);
        if (edges.Count > count) edges.RemoveRange(count, edges.Count - count);
    }

    /// <summary>Collapses zero-distance SCCs, then solves the stable longest paths of the component DAG.</summary>
    internal double[]? Solve() {
        ConflictDeclarationId = null;
        var n = parent.Length;
        budget.Work((long)n * 8);
        var roots = new int[n];
        var forward = new List<int>?[n]; var reverse = new List<int>?[n];
        for (var i = 0; i < n; i++) roots[i] = Root(i);
        foreach (var edge in edges) {
            budget.Work(); var a = roots[edge.From]; var b = roots[edge.To];
            if (a == b) { if (edge.Distance > 0) { ConflictDeclarationId = edge.DeclarationId; return null; } continue; }
            if (edge.Distance != 0) continue;
            budget.Work(2);
            (forward[a] ??= new List<int>()).Add(b); (reverse[b] ??= new List<int>()).Add(a);
        }
        // Native dimensions, containment and separation distances are strictly positive.
        // Only origin edges are normally zero. Explicit SCC handling also makes a feasible
        // all-zero cycle harmless, rather than misclassifying it as a positive conflict.
        var visited = new bool[n]; var cursor = new int[n]; var order = new List<int>(); var stack = new Stack<int>();
        for (var start = 0; start < n; start++) {
            if (roots[start] != start || visited[start]) continue;
            budget.Work(); visited[start] = true; stack.Push(start);
            while (stack.Count > 0) {
                budget.Work(); var node = stack.Peek(); var adjacent = forward[node];
                if (adjacent != null && cursor[node] < adjacent.Count) {
                    var next = adjacent[cursor[node]++];
                    if (!visited[next]) { budget.Work(); visited[next] = true; stack.Push(next); }
                } else { stack.Pop(); budget.Work(); order.Add(node); }
            }
        }
        var component = new int[n]; for (var i = 0; i < n; i++) component[i] = -1;
        var components = 0;
        for (var i = order.Count - 1; i >= 0; i--) {
            var start = order[i]; if (component[start] >= 0) continue;
            budget.Work(); component[start] = components; stack.Push(start);
            while (stack.Count > 0) {
                budget.Work(); var node = stack.Pop(); var adjacent = reverse[node];
                if (adjacent == null) continue;
                foreach (var next in adjacent) {
                    budget.Work(); if (component[next] >= 0) continue;
                    component[next] = components; stack.Push(next);
                }
            }
            components++;
        }
        budget.Work((long)components * 4);
        var outgoing = new List<Edge>?[components]; var indegree = new int[components]; var values = new double[components];
        foreach (var edge in edges) {
            budget.Work(); var a = component[roots[edge.From]]; var b = component[roots[edge.To]];
            if (a == b) { if (edge.Distance > 0) { ConflictDeclarationId = edge.DeclarationId; return null; } continue; }
            budget.Work(); (outgoing[a] ??= new List<Edge>()).Add(new Edge(a, b, edge.Distance, edge.DeclarationId)); indegree[b]++;
        }
        var ready = new Queue<int>();
        for (var i = 0; i < components; i++) { budget.Work(); if (indegree[i] == 0) ready.Enqueue(i); }
        var seen = 0;
        while (ready.Count > 0) {
            budget.Work(); var node = ready.Dequeue(); seen++;
            if (outgoing[node] == null) continue;
            foreach (var edge in outgoing[node]!) {
                budget.Work(); var value = values[node] + edge.Distance;
                if (double.IsInfinity(value)) throw new InvalidOperationException("Alignment bounds exceed finite native coordinates.");
                if (value > values[edge.To]) values[edge.To] = value;
                if (--indegree[edge.To] == 0) ready.Enqueue(edge.To);
            }
        }
        if (seen != components) {
            foreach (var edge in edges) {
                budget.Work(); if (edge.DeclarationId != null && indegree[component[roots[edge.From]]] > 0 && indegree[component[roots[edge.To]]] > 0) { ConflictDeclarationId = edge.DeclarationId; break; }
            }
            return null;
        }
        budget.Work(n); var result = new double[n];
        for (var i = 0; i < n; i++) result[i] = values[component[roots[i]]];
        foreach (var edge in edges) {
            budget.Work();
            if (result[edge.To] - result[edge.From] + 0.000001 < edge.Distance)
                throw new InvalidOperationException("Solved native alignment distances exceed finite coordinate precision.");
        }
        return result;
    }
}
