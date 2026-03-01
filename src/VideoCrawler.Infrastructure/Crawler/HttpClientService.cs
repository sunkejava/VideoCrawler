using System.Net.Http.Headers;
using Polly;
using Polly.Retry;

namespace VideoCrawler.Infrastructure.Crawler;

/// <summary>
/// HTTP 爬虫服务
/// </summary>
public interface IHttpClientService
{
    Task<string> GetAsync(string url, CancellationToken cancellationToken = default);
    Task<byte[]> GetBytesAsync(string url, CancellationToken cancellationToken = default);
    Task<Stream> GetStreamAsync(string url, CancellationToken cancellationToken = default);
}

public class HttpClientService : IHttpClientService
{
    private readonly HttpClient _httpClient;
    private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;
    private readonly ILogger<HttpClientService> _logger;

    public HttpClientService(HttpClient httpClient, ILogger<HttpClientService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        
        _retryPolicy = Policy
            .Handle<HttpRequestException>()
            .OrResult<HttpResponseMessage>(r => 
                (int)r.StatusCode >= 500 || 
                r.StatusCode == System.Net.HttpStatusCode.RequestTimeout)
            .WaitAndRetryAsync(3, retryAttempt => 
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (result, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning("请求失败，{TimeSpan}s 后重试 (第{RetryCount}次): {Url}", 
                        timeSpan.TotalSeconds, retryCount, result.Result?.RequestMessage?.RequestUri);
                });
    }

    public async Task<string> GetAsync(string url, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("请求 URL: {Url}", url);
        
        var response = await _retryPolicy.ExecuteAsync(
            async () => await _httpClient.GetAsync(url, cancellationToken),
            cancellationToken);

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    public async Task<byte[]> GetBytesAsync(string url, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("下载文件：{Url}", url);
        
        var response = await _retryPolicy.ExecuteAsync(
            async () => await _httpClient.GetAsync(url, cancellationToken),
            cancellationToken);

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    public async Task<Stream> GetStreamAsync(string url, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("下载流：{Url}", url);
        
        var response = await _retryPolicy.ExecuteAsync(
            async () => await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken),
            cancellationToken);

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStreamAsync(cancellationToken);
    }
}

/// <summary>
/// 视频爬取服务实现
/// </summary>
public class VideoCrawlerService : IVideoCrawlerService
{
    private readonly IHttpClientService _httpClient;
    private readonly IVideoRepository _videoRepository;
    private readonly ICrawlerTaskRepository _taskRepository;
    private readonly IWorkerService _workerService;
    private readonly IVideoCacheService _cacheService;
    private readonly IVideoSiteParser _parser;
    private readonly ILogger<VideoCrawlerService> _logger;

    public VideoCrawlerService(
        IHttpClientService httpClient,
        IVideoRepository videoRepository,
        ICrawlerTaskRepository taskRepository,
        IWorkerService workerService,
        IVideoCacheService cacheService,
        IEnumerable<IVideoSiteParser> parsers,
        ILogger<VideoCrawlerService> logger)
    {
        _httpClient = httpClient;
        _videoRepository = videoRepository;
        _taskRepository = taskRepository;
        _workerService = workerService;
        _cacheService = cacheService;
        _parser = parsers.FirstOrDefault() ?? new HuaduZYParser();
        _logger = logger;
    }

    public async Task<CrawlerTask> CreateCrawlTaskAsync(string targetUrl, string taskType = "Incremental")
    {
        var task = new CrawlerTask(
            taskName: $"爬取任务-{DateTime.Now:yyyyMMddHHmmss}",
            targetUrl: targetUrl,
            taskType: taskType
        );

        await _taskRepository.AddAsync(task);
        _logger.LogInformation("创建爬取任务：{TaskId} - {Url}", task.Id, targetUrl);
        
        return task;
    }

    public async Task ExecuteCrawlTaskAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        var task = await _taskRepository.GetByIdAsync(taskId);
        if (task == null)
            throw new InvalidOperationException($"任务不存在：{taskId}");

        var workerId = await _workerService.AssignTaskAsync(taskId);
        task.Start(workerId);
        await _taskRepository.UpdateAsync(task);

        try
        {
            // 爬取视频列表
            var videos = await FetchVideoListAsync(task.TargetUrl);
            task.TotalCount = videos.Count;
            _logger.LogInformation("爬取到 {Count} 个视频", videos.Count);

            int success = 0, failed = 0, skipped = 0;
            
            foreach (var video in videos)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    task.Cancel();
                    break;
                }

                try
                {
                    // 检查是否已存在
                    var existingVideo = await _videoRepository.GetBySourceUrlAsync(video.SourceUrl);
                    if (existingVideo != null)
                    {
                        _logger.LogInformation("视频已存在，跳过：{Title}", video.Title);
                        skipped++;
                        continue;
                    }

                    // 爬取详情
                    var detail = await FetchVideoDetailAsync(video.SourceUrl);
                    if (detail != null)
                    {
                        // 下载封面
                        if (!string.IsNullOrEmpty(detail.CoverImage))
                        {
                            await _cacheService.CacheCoverImageAsync(detail);
                        }
                        
                        await _videoRepository.AddAsync(detail);
                        success++;
                        _logger.LogInformation("爬取成功：{Title}", detail.Title);
                    }
                    else
                    {
                        failed++;
                        _logger.LogWarning("爬取详情失败：{Title}", video.Title);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "爬取视频失败：{Title}", video.Title);
                    failed++;
                }

                task.UpdateProgress(success + failed + skipped, success, failed);
                await _taskRepository.UpdateAsync(task);
            }

            task.Complete(success, failed);
            await _taskRepository.UpdateAsync(task);
            
            if (workerId != null)
            {
                await _workerService.UpdateWorkerStatusAsync(workerId, "Idle");
            }
            
            _logger.LogInformation("任务完成：成功={Success}, 失败={Failed}, 跳过={Skipped}", success, failed, skipped);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "任务执行失败");
            task.Fail(ex.Message);
            await _taskRepository.UpdateAsync(task);
            
            if (workerId != null)
            {
                await _workerService.UpdateWorkerStatusAsync(workerId, "Idle");
            }
            throw;
        }
    }

    public async Task<Video?> FetchVideoDetailAsync(string url)
    {
        try
        {
            var html = await _httpClient.GetAsync(url);
            var video = await _parser.ParseVideoDetailAsync(html, url);
            
            if (video != null)
            {
                video.MarkAsCrawled(url);
            }
            
            return video;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "爬取视频详情失败：{Url}", url);
            return null;
        }
    }

    public async Task<List<Video>> FetchVideoListAsync(string listUrl, int maxCount = 100)
    {
        try
        {
            var html = await _httpClient.GetAsync(listUrl);
            var videos = await _parser.ParseVideoListAsync(html, listUrl);
            
            _logger.LogInformation("解析到 {Count} 个视频", videos.Count);
            
            return videos.Take(maxCount).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "爬取视频列表失败：{Url}", listUrl);
            return new List<Video>();
        }
    }

    public async Task<bool> DownloadVideoAsync(Video video, string savePath)
    {
        if (string.IsNullOrEmpty(video.M3u8Url))
        {
            _logger.LogWarning("视频没有 M3U8 地址：{Title}", video.Title);
            return false;
        }

        try
        {
            var bytes = await _httpClient.GetBytesAsync(video.M3u8Url);
            
            Directory.CreateDirectory(Path.GetDirectoryName(savePath)!);
            await File.WriteAllBytesAsync(savePath, bytes);
            
            video.UpdateCacheInfo(savePath, video.M3u8Url);
            await _videoRepository.UpdateAsync(video);
            
            _logger.LogInformation("视频下载成功：{Path}", savePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "视频下载失败：{Url}", video.M3u8Url);
            return false;
        }
    }

    public async Task<bool> DownloadCoverImageAsync(Video video, string savePath)
    {
        if (string.IsNullOrEmpty(video.CoverImage))
        {
            return false;
        }

        try
        {
            var bytes = await _httpClient.GetBytesAsync(video.CoverImage);
            
            Directory.CreateDirectory(Path.GetDirectoryName(savePath)!);
            await File.WriteAllBytesAsync(savePath, bytes);
            
            video.UpdateCoverImageLocal(savePath);
            await _videoRepository.UpdateAsync(video);
            
            _logger.LogInformation("封面下载成功：{Path}", savePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "封面下载失败：{Url}", video.CoverImage);
            return false;
        }
    }

    public async Task ParseM3u8Async(string m3u8Url, string savePath)
    {
        // TODO: 实现完整的 M3U8 解析和下载
        await Task.CompletedTask;
    }
}
