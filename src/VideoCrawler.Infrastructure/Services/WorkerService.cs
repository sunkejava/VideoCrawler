using VideoCrawler.Domain.Interfaces;

namespace VideoCrawler.Infrastructure.Services;

public class WorkerService : IWorkerService
{
    private readonly Dictionary<string, WorkerInfo> _workers = new();
    private readonly ILogger<WorkerService> _logger;
    private readonly object _lock = new();

    public WorkerService(ILogger<WorkerService> logger)
    {
        _logger = logger;
    }

    public Task<string> RegisterWorkerAsync(string workerName)
    {
        var workerId = $"worker-{Guid.NewGuid():N}[..8]";
        
        lock (_lock)
        {
            _workers[workerId] = new WorkerInfo
            {
                WorkerId = workerId,
                WorkerName = workerName,
                Status = "Idle",
                LastHeartbeat = DateTime.UtcNow,
                RegisteredAt = DateTime.UtcNow
            };
        }

        _logger.LogInformation("工作节点注册：{WorkerId} - {WorkerName}", workerId, workerName);
        return Task.FromResult(workerId);
    }

    public Task UnregisterWorkerAsync(string workerId)
    {
        lock (_lock)
        {
            _workers.Remove(workerId);
        }

        _logger.LogInformation("工作节点注销：{WorkerId}", workerId);
        return Task.CompletedTask;
    }

    public Task<List<string>> GetAvailableWorkersAsync()
    {
        lock (_lock)
        {
            var available = _workers
                .Where(w => w.Value.Status == "Idle")
                .Select(w => w.Key)
                .ToList();
            
            return Task.FromResult(available);
        }
    }

    public Task<string?> AssignTaskAsync(Guid taskId)
    {
        lock (_lock)
        {
            var availableWorker = _workers
                .FirstOrDefault(w => w.Value.Status == "Idle")
                .Key;

            if (availableWorker != null)
            {
                _workers[availableWorker].Status = "Busy";
                _workers[availableWorker].CurrentTaskId = taskId.ToString();
                _logger.LogInformation("任务分配：{TaskId} -> {WorkerId}", taskId, availableWorker);
            }

            return Task.FromResult(availableWorker);
        }
    }

    public Task UpdateWorkerStatusAsync(string workerId, string status, string? currentTaskId = null)
    {
        lock (_lock)
        {
            if (_workers.TryGetValue(workerId, out var worker))
            {
                worker.Status = status;
                worker.CurrentTaskId = currentTaskId;
                worker.LastHeartbeat = DateTime.UtcNow;

                if (status == "Idle" && currentTaskId == null)
                {
                    worker.CompletedTasks++;
                }
            }
        }

        return Task.CompletedTask;
    }

    public Task<WorkerInfo> GetWorkerInfoAsync(string workerId)
    {
        lock (_lock)
        {
            if (_workers.TryGetValue(workerId, out var worker))
            {
                return Task.FromResult(worker);
            }

            throw new KeyNotFoundException($"工作节点不存在：{workerId}");
        }
    }
}

public class WorkerInfo
{
    public string WorkerId { get; set; } = string.Empty;
    public string WorkerName { get; set; } = string.Empty;
    public string Status { get; set; } = "Idle";
    public string? CurrentTaskId { get; set; }
    public int CompletedTasks { get; set; }
    public int FailedTasks { get; set; }
    public DateTime LastHeartbeat { get; set; }
    public DateTime RegisteredAt { get; set; }
}
