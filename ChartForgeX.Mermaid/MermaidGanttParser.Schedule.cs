using System;
using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidGanttParser {
    // Resolve start/end values separately: until needs a referenced start, not its end.
    // A queue avoids recursion and preserves source row order independently of date dependencies.
    private static void ResolveSchedule(MermaidGanttDocument document, List<TaskDefinition> tasks,
        Dictionary<string, TaskDefinition> taskIds, MermaidParseResult<MermaidDocument> result) {
        var slots = new List<DateSlot>();
        foreach (var task in tasks) {
            slots.Add(task.Start);
            slots.Add(task.End);
            if (string.IsNullOrWhiteSpace(task.StartSpec)) {
                if (task.Index == 0) Fail(task.Start, result, "The first Gantt task must declare an explicit start date.");
                else AddDependency(task.Start, tasks[task.Index - 1].End);
            } else if (StartsWithKeyword(task.StartSpec!, "after")) {
                AddReferences(task.Start, task.StartSpec!, "after", task.AfterIds, taskIds, result);
            }
            AddDependency(task.End, task.Start);
            if (StartsWithKeyword(task.EndSpec, "until")) {
                AddReferences(task.End, task.EndSpec, "until", task.UntilIds, taskIds, result);
            }
        }

        var ready = new Queue<DateSlot>();
        foreach (var slot in slots) if (slot.Pending == 0 || slot.Failed) ready.Enqueue(slot);
        while (ready.Count > 0) {
            var slot = ready.Dequeue();
            if (slot.Processed) continue;
            foreach (var dependency in slot.Dependencies) if (dependency.Failed) slot.Failed = true;
            if (!slot.Failed) ResolveDate(slot, document, result);
            slot.Processed = true;
            foreach (var dependent in slot.Dependents) {
                if (slot.Failed) dependent.Failed = true;
                if (--dependent.Pending == 0 || dependent.Failed) ready.Enqueue(dependent);
            }
        }

        var indexes = new Dictionary<int, int>();
        foreach (var task in tasks) {
            if (!task.Start.Processed || !task.End.Processed) {
                Fail(task.End, result, "Gantt task dates contain a circular dependency.");
            }
            if (!task.Start.Failed && !task.End.Failed && task.Start.Value.HasValue && task.End.Value.HasValue) indexes.Add(task.Index, indexes.Count);
        }
        foreach (var task in tasks) {
            if (!indexes.ContainsKey(task.Index)) continue;
            var milestone = ContainsTag(task.Tags, "milestone");
            var parsed = new MermaidGanttTask(task.Title, task.Id, task.Section, task.Start.Value!.Value, task.End.Value!.Value,
                milestone || ContainsTag(task.Tags, "done") ? 1 : 0, milestone, task.Tags, task.AfterIds, task.RawMetadata, task.Span);
            parsed.UntilTaskIds.AddRange(task.UntilIds);
            if (indexes.TryGetValue(task.DependencyIndex, out var dependencyIndex)) parsed.DependencyIndex = dependencyIndex;
            document.Tasks.Add(parsed);
        }
    }

    private static void AddDependency(DateSlot slot, DateSlot dependency) {
        slot.Dependencies.Add(dependency);
        dependency.Dependents.Add(slot);
        slot.Pending++;
    }

    private static void AddReferences(DateSlot slot, string text, string keyword, List<string> ids,
        Dictionary<string, TaskDefinition> taskIds, MermaidParseResult<MermaidDocument> result) {
        var references = text.Substring(keyword.Length).Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        var valid = references.Length > 0;
        foreach (var id in references) {
            ids.Add(id);
            if (!taskIds.TryGetValue(id, out var target) || (keyword == "after" && target.Index >= slot.Task.Index)) {
                valid = false;
            } else AddDependency(slot, keyword == "after" ? target.End : target.Start);
        }
        if (!valid) Fail(slot, result, keyword == "after"
            ? "Gantt after clauses must reference earlier task ids."
            : "Gantt until clauses must reference declared task ids.");
    }

    private static void ResolveDate(DateSlot slot, MermaidGanttDocument document, MermaidParseResult<MermaidDocument> result) {
        var task = slot.Task;
        if (!slot.IsEnd) {
            if (slot.Dependencies.Count > 0) {
                DateTime? latest = null;
                foreach (var dependency in slot.Dependencies) {
                    if (!latest.HasValue || dependency.Value!.Value > latest.Value) {
                        latest = dependency.Value;
                        if (task.AfterIds.Count > 0) task.DependencyIndex = dependency.Task.Index;
                    }
                }
                slot.Value = latest;
            } else if (TryParseDate(task.StartSpec!, document.DateFormat, out var start)) slot.Value = start;
            else Fail(slot, result, "Gantt task start dates must match dateFormat '" + document.DateFormat + "'.");
            return;
        }

        var taskStart = task.Start.Value!.Value;
        DateTime end;
        if (task.UntilIds.Count > 0) {
            var earliest = slot.Dependencies[1].Value!.Value;
            for (var index = 2; index < slot.Dependencies.Count; index++) {
                if (slot.Dependencies[index].Value!.Value < earliest) earliest = slot.Dependencies[index].Value!.Value;
            }
            if (!TryResolveCalendarEnd(taskStart, earliest, document, out end)) {
                Fail(slot, result, "Gantt exclusion calendar has no reachable working end date.");
                return;
            }
        } else if (TryParseDuration(task.EndSpec, out var amount, out var unit)) {
            if (!TryResolveDurationEnd(taskStart, amount, unit, document, out end)) {
                Fail(slot, result, "Gantt duration exceeds the supported date range or exclusion calendar has no reachable working day.");
                return;
            }
        } else if (!TryParseDate(task.EndSpec, document.DateFormat, out end)) {
            Fail(slot, result, "Gantt task end values must be dates, durations or until clauses.");
            return;
        }
        if (end < taskStart) Fail(slot, result, "Gantt task end must be greater than or equal to start.");
        else slot.Value = end;
    }

    private static void Fail(DateSlot slot, MermaidParseResult<MermaidDocument> result, string message) {
        slot.Failed = true;
        var span = slot.Task.Span;
        Add(result, span.Line, span.Column, span.Length, MermaidDiagnosticSeverity.Error, message);
    }

    private sealed class DateSlot {
        public DateSlot(TaskDefinition task, bool isEnd) { Task = task; IsEnd = isEnd; }
        public TaskDefinition Task { get; }
        public bool IsEnd { get; }
        public List<DateSlot> Dependencies { get; } = new();
        public List<DateSlot> Dependents { get; } = new();
        public int Pending { get; set; }
        public bool Processed { get; set; }
        public bool Failed { get; set; }
        public DateTime? Value { get; set; }
    }

    private sealed class TaskDefinition {
        public TaskDefinition(string title, string? id, string? section, string? startSpec, string endSpec,
            List<string> tags, string rawMetadata, MermaidSourceSpan span, int index) {
            Title = title; Id = id; Section = section; StartSpec = startSpec; EndSpec = endSpec;
            Tags = tags; RawMetadata = rawMetadata; Span = span; Index = index;
            Start = new DateSlot(this, false); End = new DateSlot(this, true);
        }
        public string Title { get; }
        public string? Id { get; }
        public string? Section { get; }
        public string? StartSpec { get; }
        public string EndSpec { get; }
        public List<string> Tags { get; }
        public string RawMetadata { get; }
        public MermaidSourceSpan Span { get; }
        public int Index { get; }
        public DateSlot Start { get; }
        public DateSlot End { get; }
        public List<string> AfterIds { get; } = new();
        public List<string> UntilIds { get; } = new();
        public int DependencyIndex { get; set; } = -1;
    }
}
