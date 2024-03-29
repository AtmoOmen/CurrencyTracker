using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CurrencyTracker.Manager.Tasks;

public class TaskManager : IDisposable
{
    private static readonly List<TaskManager> Instances = new();

    /// <summary>
    ///     Number of tasks that were registered in current cycle. Increases each time a task is enqueued and resets once there
    ///     are no more tasks.
    /// </summary>
    public int MaxTasks { get; private set; }

    /// <summary>
    ///     After this time limit has passed, a task will be given up.
    /// </summary>
    public int TimeLimitMS = 10000;

    /// <summary>
    ///     Whether to abort execution on timeout and clear all remaining tasks or not.
    /// </summary>
    public bool AbortOnTimeout = false;

    /// <summary>
    ///     Tick count (<see cref="Environment.TickCount64" />) at which current task will be aborted
    /// </summary>
    public long AbortAt { get; private set; }

    private TaskManagerTask? CurrentTask;
    public string? CurrentTaskName => CurrentTask?.Name;
    public List<string?> TaskStack => ImmediateTasks.Select(x => x.Name).Union(Tasks.Select(x => x.Name)).ToList();

    /// <summary>
    ///     Amount of currently queued tasks, including one that is currently being executed
    /// </summary>
    public int NumQueuedTasks => Tasks.Count + ImmediateTasks.Count + (CurrentTask == null ? 0 : 1);

    /// <summary>
    ///     Whether to redirect timeout errors into Verbose channel
    /// </summary>
    public bool TimeoutSilently = false;

    /// <summary>
    ///     Whether to output debug information into PluginLog
    /// </summary>
    public bool ShowDebug = true;

    private readonly Queue<TaskManagerTask> Tasks = new();
    private readonly Queue<TaskManagerTask> ImmediateTasks = new();

    /// <summary>
    ///     Initializes new instance of <see cref="TaskManager" />.
    /// </summary>
    public TaskManager()
    {
        Service.Framework.Update += Tick;
        Instances.Add(this);
    }


    /// <summary>
    ///     Sets step mode, when enabled task manager won't execute tasks automatically and will wait for Step command from
    ///     you.
    /// </summary>
    /// <param name="enabled"></param>
    public void SetStepMode(bool enabled)
    {
        Service.Framework.Update -= Tick;
        if (!enabled) Service.Framework.Update += Tick;
    }

    /// <summary>
    ///     Manually execute task manager cycle.
    /// </summary>
    public void Step()
    {
        Tick(null);
    }

    public void Dispose()
    {
        Service.Framework.Update -= Tick;
        Instances.Remove(this);
    }

    internal static void DisposeAll()
    {
        var i = 0;
        foreach (var manager in Instances)
        {
            i++;
            Service.Framework.Update -= manager.Tick;
        }

        if (i > 0) Service.Log.Debug($"Auto-disposing {i} task managers");
        Instances.Clear();
    }

    /// <summary>
    ///     Whether TaskManager is currently executing a task.
    /// </summary>
    public bool IsBusy => CurrentTask != null || Tasks.Count > 0 || ImmediateTasks.Count > 0;

    public void Enqueue(Func<bool?> task, string? name = null)
    {
        Tasks.Enqueue(new TaskManagerTask(task, TimeLimitMS, AbortOnTimeout, name));
        MaxTasks++;
    }

    public void Enqueue(Func<bool?> task, int timeLimitMs, string? name = null)
    {
        Tasks.Enqueue(new TaskManagerTask(task, timeLimitMs, AbortOnTimeout, name));
        MaxTasks++;
    }

    public void Enqueue(Func<bool?> task, bool abortOnTimeout, string? name = null)
    {
        Tasks.Enqueue(new TaskManagerTask(task, TimeLimitMS, abortOnTimeout, name));
        MaxTasks++;
    }

    public void Enqueue(Func<bool?> task, int timeLimitMs, bool abortOnTimeout, string? name = null)
    {
        Tasks.Enqueue(new TaskManagerTask(task, timeLimitMs, abortOnTimeout, name));
        MaxTasks++;
    }

    public void Enqueue(Action task, string? name = null)
    {
        Tasks.Enqueue(new TaskManagerTask(() =>
        {
            task();
            return true;
        }, TimeLimitMS, AbortOnTimeout, name));
        MaxTasks++;
    }

    public void Enqueue(Action task, int timeLimitMs, string? name = null)
    {
        Tasks.Enqueue(new TaskManagerTask(() =>
        {
            task();
            return true;
        }, timeLimitMs, AbortOnTimeout, name));
        MaxTasks++;
    }

    public void Enqueue(Action task, bool abortOnTimeout, string? name = null)
    {
        Tasks.Enqueue(new TaskManagerTask(() =>
        {
            task();
            return true;
        }, TimeLimitMS, abortOnTimeout, name));
        MaxTasks++;
    }

    public void Enqueue(Action task, int timeLimitMs, bool abortOnTimeout, string? name = null)
    {
        Tasks.Enqueue(new TaskManagerTask(() =>
        {
            task();
            return true;
        }, timeLimitMs, abortOnTimeout, name));
        MaxTasks++;
    }

    public void DelayNext(int delayMS, bool useFrameThrottler = false)
    {
        DelayNext("ECommonsGenericDelay", delayMS, useFrameThrottler);
    }

    public void DelayNext(string uniqueName, int delayMS, bool useFrameThrottler = false)
    {
        if (useFrameThrottler)
        {
            Enqueue(() => FrameThrottler.Throttle(uniqueName, delayMS),
                    $"FrameThrottler.Throttle({uniqueName}, {delayMS})");
            Enqueue(() => FrameThrottler.Check(uniqueName), $"FrameThrottler.Check({uniqueName})");
        }
        else
        {
            Enqueue(() => EzThrottler.Throttle(uniqueName, delayMS), $"EzThrottler.Throttle({uniqueName}, {delayMS})");
            Enqueue(() => EzThrottler.Check(uniqueName), $"EzThrottler.Check({uniqueName})");
        }

        MaxTasks += 2;
    }

    public void DelayNextImmediate(int delayMS, bool useFrameThrottler = false)
    {
        DelayNextImmediate("ECommonsGenericDelay", delayMS, useFrameThrottler);
    }

    public void DelayNextImmediate(string uniqueName, int delayMS, bool useFrameThrottler = false)
    {
        if (useFrameThrottler)
        {
            EnqueueImmediate(() => FrameThrottler.Throttle(uniqueName, delayMS),
                             $"FrameThrottler.Throttle({uniqueName}, {delayMS})");
            EnqueueImmediate(() => FrameThrottler.Check(uniqueName), $"FrameThrottler.Check({uniqueName})");
        }
        else
        {
            EnqueueImmediate(() => EzThrottler.Throttle(uniqueName, delayMS),
                             $"EzThrottler.Throttle({uniqueName}, {delayMS})");
            EnqueueImmediate(() => EzThrottler.Check(uniqueName), $"EzThrottler.Check({uniqueName})");
        }

        MaxTasks += 2;
    }

    public void Abort()
    {
        Tasks.Clear();
        ImmediateTasks.Clear();
        CurrentTask = null;
    }

    public void EnqueueImmediate(Func<bool?> task, string? name = null)
    {
        ImmediateTasks.Enqueue(new TaskManagerTask(task, TimeLimitMS, AbortOnTimeout, name));
        MaxTasks++;
    }

    public void EnqueueImmediate(Func<bool?> task, int timeLimitMs, string? name = null)
    {
        ImmediateTasks.Enqueue(new TaskManagerTask(task, timeLimitMs, AbortOnTimeout, name));
        MaxTasks++;
    }

    public void EnqueueImmediate(Func<bool?> task, bool abortOnTimeout, string? name = null)
    {
        ImmediateTasks.Enqueue(new TaskManagerTask(task, TimeLimitMS, abortOnTimeout, name));
        MaxTasks++;
    }

    public void EnqueueImmediate(Func<bool?> task, int timeLimitMs, bool abortOnTimeout, string? name = null)
    {
        ImmediateTasks.Enqueue(new TaskManagerTask(task, timeLimitMs, abortOnTimeout, name));
        MaxTasks++;
    }

    public void EnqueueImmediate(Action task, string? name = null)
    {
        ImmediateTasks.Enqueue(new TaskManagerTask(() =>
        {
            task();
            return true;
        }, TimeLimitMS, AbortOnTimeout, name));
        MaxTasks++;
    }

    public void EnqueueImmediate(Action task, int timeLimitMs, string? name = null)
    {
        ImmediateTasks.Enqueue(new TaskManagerTask(() =>
        {
            task();
            return true;
        }, timeLimitMs, AbortOnTimeout, name));
        MaxTasks++;
    }

    public void EnqueueImmediate(Action task, bool abortOnTimeout, string? name = null)
    {
        ImmediateTasks.Enqueue(new TaskManagerTask(() =>
        {
            task();
            return true;
        }, TimeLimitMS, abortOnTimeout, name));
        MaxTasks++;
    }

    public void EnqueueImmediate(Action task, int timeLimitMs, bool abortOnTimeout, string? name = null)
    {
        ImmediateTasks.Enqueue(new TaskManagerTask(() =>
        {
            task();
            return true;
        }, timeLimitMs, abortOnTimeout, name));
        MaxTasks++;
    }

    private void Tick(object? _)
    {
        if (CurrentTask == null)
        {
            if (ImmediateTasks.TryDequeue(out CurrentTask))
            {
                if (ShowDebug)
                {
                    Service.Log.Debug(
                        $"Starting to execute immediate task: {CurrentTask.Name ?? CurrentTask.Action.GetMethodInfo().Name}");
                }

                AbortAt = Environment.TickCount64 + CurrentTask.TimeLimitMS;
            }
            else if (Tasks.TryDequeue(out CurrentTask))
            {
                if (ShowDebug)
                {
                    Service.Log.Debug(
                        $"Starting to execute task: {CurrentTask.Name ?? CurrentTask.Action.GetMethodInfo().Name}");
                }

                AbortAt = Environment.TickCount64 + CurrentTask.TimeLimitMS;
            }
            else
                MaxTasks = 0;
        }
        else
        {
            try
            {
                var result = CurrentTask.Action();
                switch (result)
                {
                    case true:
                    {
                        if (ShowDebug)
                        {
                            Service.Log.Debug(
                                $"Task {CurrentTask.Name ?? CurrentTask.Action.GetMethodInfo().Name} completed successfully");
                        }

                        CurrentTask = null;
                        break;
                    }
                    case false:
                    {
                        if (Environment.TickCount64 > AbortAt)
                        {
                            if (CurrentTask.AbortOnTimeout)
                            {
                                Service.Log.Warning($"Clearing {Tasks.Count} remaining tasks because of timeout");
                                Tasks.Clear();
                                ImmediateTasks.Clear();
                            }

                            throw new TimeoutException(
                                $"Task {CurrentTask.Name ?? CurrentTask.Action.GetMethodInfo().Name} took too long to execute");
                        }

                        break;
                    }
                    default:
                        Service.Log.Warning(
                            $"Clearing {Tasks.Count} remaining tasks because there was a signal from task {CurrentTask.Name ?? CurrentTask.Action.GetMethodInfo().Name} to abort");
                        Abort();
                        break;
                }
            }
            catch (TimeoutException e)
            {
                Service.Log.Warning($"{e.Message}\n{e.StackTrace}");
                CurrentTask = null;
            }
            catch (Exception e)
            {
                Service.Log.Debug(e.StackTrace);
                CurrentTask = null;
            }
        }
    }
}
