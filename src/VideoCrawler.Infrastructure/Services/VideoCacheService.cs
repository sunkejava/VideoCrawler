using VideoCrawler.Domain.Entities;
using VideoCrawler.Domain.Interfaces;

namespace VideoCrawler.Infrastructure.Services;

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

    public async Task<bool> IsCachedAsync(string sourceUrl)
    {
        var video = await _videoRepository.GetBySourceUrlAsync(sourceUrl);
        return video != null && video.IsCached;
    }

    public async Task<string?> GetCachedVideoPathAsync(string sourceUrl)
    {
        var video = await _videoRepository.GetBySourceUrlAsync(sourceUrl);
        return video?.VideoUrlLocal;
    }

    public async Task<string?> GetCachedCoverPathAsync(string sourceUrl)
    {
        var video = await _videoRepository.GetBySourceUrlAsync(sourceUrl);
        return video?.CoverImageLocal;
    }

    public async Task CacheVideoAsync(Video video)
    {
        if (string.IsNullOrEmpty(video.M3u8Url))
        {
            _logger.LogWarning("视频没有 M3U8 地址：{Title}", video.Title);
            return;
        }

        try
        {
            var savePath = Path.Combine(_cacheRoot, "videos", $"{video.Id}.m3u8");
            
            // TODO: 实现 M3U8 下载
            // 目前先保存 M3U8 播放列表
            Directory.CreateDirectory(Path.GetDirectoryName(savePath)!);
            await File.WriteAllTextAsync(savePath, video.M3u8Url);
            
            video.UpdateCacheInfo(savePath, video.M3u8Url);
            _logger.LogInformation("视频缓存信息已保存：{Path}", savePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "缓存视频失败：{Title}", video.Title);
        }
    }

    public async Task CacheCoverImageAsync(Video video)
    {
        if (string.IsNullOrEmpty(video.CoverImage))
        {
            return;
        }

        try
        {
            var extension = Path.GetExtension(video.CoverImage.Split('?')[0]);
            if (string.IsNullOrEmpty(extension))
            {
                extension = ".jpg";
            }
            
            var savePath = Path.Combine(_cacheRoot, "covers", $"{video.Id}{extension}");
            
            // TODO: 使用 HTTP 客户端下载图片
            // 目前先保存 URL
            video.UpdateCoverImageLocal(savePath);
            _logger.LogInformation("封面缓存信息已保存：{Path}", savePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "缓存封面失败：{Title}", video.Title);
        }
    }

    public async Task CleanExpiredCacheAsync(TimeSpan expiration)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow - expiration;
            var videos = await _videoRepository.GetAllAsync();
            
            foreach (var video in videos.Where(v => v.LastUpdateTime < cutoffDate && v.IsCached))
            {
                // 删除缓存文件
                if (!string.IsNullOrEmpty(video.VideoUrlLocal) && File.Exists(video.VideoUrlLocal))
                {
                    File.Delete(video.VideoUrlLocal);
                }
                
                if (!string.IsNullOrEmpty(video.CoverImageLocal) && File.Exists(video.CoverImageLocal))
                {
                    File.Delete(video.CoverImageLocal);
                }
                
                video.IsCached = false;
                video.CachePath = null;
                video.VideoUrlLocal = null;
                video.CoverImageLocal = null;
                
                await _videoRepository.UpdateAsync(video);
                _logger.LogInformation("清理过期缓存：{Title}", video.Title);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清理过期缓存失败");
        }
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
