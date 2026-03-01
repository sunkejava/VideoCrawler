using VideoCrawler.Domain.Entities;

namespace VideoCrawler.Domain.Interfaces;

/// <summary>
/// 视频爬取服务接口
/// </summary>
public interface IVideoCrawlerService
{
    Task<CrawlerTask> CreateCrawlTaskAsync(string targetUrl, string taskType = "Incremental");
    Task ExecuteCrawlTaskAsync(Guid taskId, CancellationToken cancellationToken = default);
    Task<Video?> FetchVideoDetailAsync(string url);
    Task<List<Video>> FetchVideoListAsync(string listUrl, int maxCount = 100);
    Task<bool> DownloadVideoAsync(Video video, string savePath);
    Task<bool> DownloadCoverImageAsync(Video video, string savePath);
    Task ParseM3u8Async(string m3u8Url, string savePath);
}

/// <summary>
/// 视频缓存服务接口
/// </summary>
public interface IVideoCacheService
{
    Task<bool> IsCachedAsync(string sourceUrl);
    Task<string?> GetCachedVideoPathAsync(string sourceUrl);
    Task<string?> GetCachedCoverPathAsync(string sourceUrl);
    Task CacheVideoAsync(Video video);
    Task CacheCoverImageAsync(Video video);
    Task CleanExpiredCacheAsync(TimeSpan expiration);
    Task<CacheStats> GetCacheStatsAsync();
}

public class CacheStats
{
    public int TotalVideos { get; set; }
    public int CachedVideos { get; set; }
    public long TotalSizeBytes { get; set; }
    public DateTime LastCleanup { get; set; }
}

/// <summary>
/// 工作节点服务接口（多智能体任务分配）
/// </summary>
public interface IWorkerService
{
    Task<string> RegisterWorkerAsync(string workerName);
    Task UnregisterWorkerAsync(string workerId);
    Task<List<string>> GetAvailableWorkersAsync();
    Task<string?> AssignTaskAsync(Guid taskId);
    Task UpdateWorkerStatusAsync(string workerId, string status, string? currentTaskId = null);
    Task<WorkerInfo> GetWorkerInfoAsync(string workerId);
}

public class WorkerInfo
{
    public string WorkerId { get; set; } = string.Empty;
    public string WorkerName { get; set; } = string.Empty;
    public string Status { get; set; } = "Idle"; // Idle, Busy, Offline
    public string? CurrentTaskId { get; set; }
    public int CompletedTasks { get; set; }
    public int FailedTasks { get; set; }
    public DateTime LastHeartbeat { get; set; }
    public DateTime RegisteredAt { get; set; }
}
