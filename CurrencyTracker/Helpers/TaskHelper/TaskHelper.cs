using System;
using System.Collections.Generic;
using System.Linq;
using CurrencyTracker.Helpers.Throttler;
using CurrencyTracker.Manager;

namespace CurrencyTracker.Helpers.TaskHelper;

public partial class TaskHelper : IDisposable
{
    private static readonly List<TaskHelper> Instances = [];
    private readonly FrameThrottler<string> FrameThrottler;
    private readonly Throttler<string> Throttler;

    public TaskHelper()
    {
        FrameThrottler = new(() => (long)Service.UiBuilder.FrameCount);
        Throttler = new();
        Service.Framework.Update += Tick;
        Instances.Add(this);
    }

    public  TaskHelperTask?            CurrentTask     { get; set; }
    public  string                     CurrentTaskName => CurrentTask?.Name ?? string.Empty;
    private SortedSet<TaskHelperQueue> Queues          { get; } = [new(1), new(0)];
    public  List<string>               TaskStack       => Queues.SelectMany(q => q.Tasks.Select(t => t.Name)).ToList();
    public  int                        NumQueuedTasks  => Queues.Sum(q => q.Tasks.Count) + (CurrentTask == null ? 0 : 1);
    public  bool                       IsBusy          => CurrentTask != null || Queues.Any(q => q.Tasks.Count > 0);
    public  int                        MaxTasks        { get; private set; }
    public  bool                       AbortOnTimeout  { get; set; } = true;
    public  long                       AbortAt         { get; private set; }
    public  bool                       ShowDebug       { get; set; } = false;
    public  int                        TimeLimitMS     { get; set; } = 10000;

    private void Tick(object? _)
    {
        if (CurrentTask == null)
        {
            ProcessNextTask();
        }
        else
        {
            ExecuteCurrentTask();
        }
    }

    private void ProcessNextTask()
    {
        foreach (var queue in Queues)
        {
            if (queue.Tasks.TryDequeue(out var task))
            {
                CurrentTask = task;
                if (ShowDebug)
                    Service.Log.Debug($"Start Executing Task: {CurrentTask.Name}");

                AbortAt = Environment.TickCount64 + CurrentTask.TimeLimitMS;
                break;
            }
        }

        if (CurrentTask == null) MaxTasks = 0;
    }

    private void ExecuteCurrentTask()
    {
        try
        {
            var result = CurrentTask.Action();
            switch (result)
            {
                case true:
                    CompleteTask();
                    break;
                case false:
                    CheckForTimeout();
                    break;
                default:
                    AbortAllTasks();
                    break;
            }
        }
        catch (TimeoutException e)
        {
            HandleTimeout(e);
        }
        catch (Exception e)
        {
            HandleError(e);
        }
    }

    private void CompleteTask()
    {
        if (ShowDebug)
            Service.Log.Debug($"Task Completed: {CurrentTask.Name}");
        CurrentTask = null;
    }

    private void CheckForTimeout()
    {
        if (Environment.TickCount64 > AbortAt)
        {
            if (CurrentTask.AbortOnTimeout)
            {
                AbortAllTasks($"Task {CurrentTask.Name} Execution Time Is Too Long");
            }
            else
            {
                Service.Log.Warning($"Task {CurrentTask. Name} Takes Too Long To Execute, But Is Set To Not Terminate Other Tasks.");
            }
        }
    }

    private void AbortAllTasks(string reason = "None")
    {
        Service.Log.Warning($"Cleaning Up All Remaining Tasks (Reason: {reason})");
        Abort();
    }

    private void HandleTimeout(Exception e)
    {
        Service.Log.Error("Task Execution Timeout", e);
        CurrentTask = null;
    }

    private void HandleError(Exception e)
    {
        Service.Log.Error("An Error Occurred During The Execution Of The Task", e);
        CurrentTask = null;
    }

    public void SetStepMode(bool enabled)
    {
        Service.Framework.Update -= Tick;
        if (!enabled)
            Service.Framework.Update += Tick;
    }

    public void Step() => Tick(null);

    public bool AddQueue(uint weight)
    {
        if (Queues.Any(q => q.Weight == weight)) return false;
        Queues.Add(new TaskHelperQueue(weight));
        return true;
    }

    public bool RemoveQueue(uint weight) => Queues.RemoveWhere(q => q.Weight == weight) > 0;

    public void RemoveAllTasks(uint weight) =>
        Queues.FirstOrDefault(q => q.Weight == weight)?.Tasks.Clear();

    public bool RemoveFirstTask(uint weight) =>
        Queues.FirstOrDefault(q => q.Weight == weight)?.Tasks.TryDequeue(out _) ?? false;

    public bool RemoveLastTask(uint weight)
    {
        var queue = Queues.FirstOrDefault(q => q.Weight == weight);
        if (queue?.Tasks.Count > 0)
        {
            queue.Tasks.RemoveAt(queue.Tasks.Count - 1);
            return true;
        }
        return false;
    }

    public bool RemoveFirstNTasks(uint weight, int count)
    {
        var queue = Queues.FirstOrDefault(q => q.Weight == weight);
        if (queue?.Tasks.Count > 0)
        {
            var actualCountToRemove = Math.Min(count, queue.Tasks.Count);
            queue.Tasks.RemoveRange(0, actualCountToRemove);
            return true;
        }
        return false;
    }

    public void Abort()
    {
        foreach (var queue in Queues)
            queue.Tasks.Clear();
        CurrentTask = null;
    }

    public void Dispose()
    {
        Service.Framework.Update -= Tick;
        Instances.Remove(this);
    }

    public static void DisposeAll()
    {
        var disposedCount = 0;
        foreach (var instance in Instances)
        {
            Service.Framework.Update -= instance.Tick;
            disposedCount++;
        }

        if (disposedCount > 0)
            Service.Log.Debug($"{disposedCount} Task Managers Have Been Automatically Cleared");

        Instances.Clear();
    }
}
