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

public class VideoCacheService : IVideoCacheService
{
    private readonly IVideoRepository _videoRepository;
    private readonly string _cacheRoot;
    private readonly ILogger<VideoCacheService> _logger;

    public VideoCacheService(
        IVideoRepository videoRepository,
        IOptions<CacheOptions> options,
        ILogger<VideoCacheService> logger)
    {
        _videoRepository = videoRepository;
        _cacheRoot = options.Value.CachePath;
        _logger = logger;

        Directory.CreateDirectory(_cacheRoot);
        Directory.CreateDirectory(Path.Combine(_cacheRoot, "videos"));
        Directory.CreateDirectory(Path.Combine(_cacheRoot, "covers"));
    }

    public Task<bool> IsCachedAsync(string sourceUrl)
    {
        // TODO: 检查数据库缓存状态
        return Task.FromResult(false);
    }

    public Task<string?> GetCachedVideoPathAsync(string sourceUrl)
    {
        return Task.FromResult<string?>(null);
    }

    public Task<string?> GetCachedCoverPathAsync(string sourceUrl)
    {
        return Task.FromResult<string?>(null);
    }

    public Task CacheVideoAsync(Video video)
    {
        // TODO: 实现视频缓存
        return Task.CompletedTask;
    }

    public Task CacheCoverImageAsync(Video video)
    {
        // TODO: 实现封面缓存
        return Task.CompletedTask;
    }

    public Task CleanExpiredCacheAsync(TimeSpan expiration)
    {
        // TODO: 实现过期缓存清理
        return Task.CompletedTask;
    }

    public async Task<CacheStats> GetCacheStatsAsync()
    {
        var totalVideos = await _videoRepository.GetTotalCountAsync();
        var cachedVideos = await _videoRepository.GetCachedCountAsync();
        
        var totalSize = CalculateCacheSize();

        return new CacheStats
        {
            TotalVideos = totalVideos,
            CachedVideos = cachedVideos,
            TotalSizeBytes = totalSize,
            LastCleanup = DateTime.UtcNow
        };
    }

    private long CalculateCacheSize()
    {
        try
        {
            var videoDir = Path.Combine(_cacheRoot, "videos");
            var coverDir = Path.Combine(_cacheRoot, "covers");
            
            var videoSize = GetDirectorySize(videoDir);
            var coverSize = GetDirectorySize(coverDir);
            
            return videoSize + coverSize;
        }
        catch
        {
            return 0;
        }
    }

    private long GetDirectorySize(string path)
    {
        if (!Directory.Exists(path)) return 0;
        
        return Directory.GetFiles(path, "*", SearchOption.AllDirectories)
            .Sum(f => new FileInfo(f).Length);
    }
}

public class CacheOptions
{
    public string CachePath { get; set; } = "./cache";
    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromDays(30);
    public long MaxCacheSizeBytes { get; set; } = 10L * 1024 * 1024 * 1024; // 10GB
}
