using System.Collections.Concurrent;

namespace server;

class TaskTracker
{
    ConcurrentDictionary<Guid, TaskData> tasks = new();

    public TaskData AddNew(string expression)
    {
        Guid id = Guid.NewGuid();
        var data = new TaskData { Id = id, Expression = expression, Status = TaskStatus.Pending, Result = null };
        tasks.TryAdd(id, data);
        return data;
    }

    public TaskData? Start(Guid id)
    {
        if (tasks.TryGetValue(id, out var data))
        {
            data.Status = TaskStatus.InProgress;
            return data;
        }

        return null;
    }

    public TaskData? Complete(Guid id, object value)
    {
        if (tasks.TryGetValue(id, out var data))
        {
            data.Status = TaskStatus.Complete;
            data.Result = value;
            return data;
        }

        return null;
    }

    public IEnumerable<TaskData> GetTasks()
    {
        return tasks.Values;
    }
}

class TaskData
{
    public Guid Id { get; set; }
    public string Expression { get; set; } = "";
    public TaskStatus Status { get; set; }
    public object? Result { get; set; }
}

enum TaskStatus
{
    Pending,
    InProgress,
    Complete,
    Error,
}