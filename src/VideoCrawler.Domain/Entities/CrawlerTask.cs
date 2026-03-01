namespace VideoCrawler.Domain.Entities;

/// <summary>
/// 爬取任务实体
/// </summary>
public class CrawlerTask : BaseEntity
{
    public string TaskName { get; private set; } = string.Empty;
    public string TargetUrl { get; private set; } = string.Empty;
    public string TaskType { get; private set; } = string.Empty; // Full, Incremental, Single
    public string Status { get; private set; } = "Pending"; // Pending, Running, Completed, Failed, Cancelled
    public int TotalCount { get; private set; }
    public int ProcessedCount { get; private set; }
    public int SuccessCount { get; private set; }
    public int FailedCount { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime? StartTime { get; private set; }
    public DateTime? EndTime { get; private set; }
    public string? AssignedWorker { get; private set; }
    public int Priority { get; private set; } = 5;
    public int RetryCount { get; private set; }
    public int MaxRetryCount { get; private set; } = 3;
    public TimeSpan? Timeout { get; private set; }

    public CrawlerTask()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }

    public CrawlerTask(string taskName, string targetUrl, string taskType = "Incremental")
    {
        Id = Guid.NewGuid();
        TaskName = taskName;
        TargetUrl = targetUrl;
        TaskType = taskType;
        Status = "Pending";
        CreatedAt = DateTime.UtcNow;
    }

    public void Start(string? workerId = null)
    {
        Status = "Running";
        StartTime = DateTime.UtcNow;
        AssignedWorker = workerId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Complete(int successCount, int failedCount)
    {
        Status = "Completed";
        SuccessCount = successCount;
        FailedCount = failedCount;
        ProcessedCount = successCount + failedCount;
        EndTime = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Fail(string errorMessage)
    {
        Status = "Failed";
        ErrorMessage = errorMessage;
        EndTime = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateProgress(int processed, int success, int failed)
    {
        ProcessedCount = processed;
        SuccessCount = success;
        FailedCount = failed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Retry()
    {
        if (RetryCount < MaxRetryCount)
        {
            RetryCount++;
            Status = "Pending";
            ErrorMessage = null;
            UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            Status = "Failed";
            ErrorMessage = $"达到最大重试次数 ({MaxRetryCount})";
        }
    }

    public void Cancel()
    {
        Status = "Cancelled";
        EndTime = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
