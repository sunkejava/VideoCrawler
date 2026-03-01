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
    private readonly ILogger<HttpClientService> _logger;

    public HttpClientService(HttpClient httpClient, ILogger<HttpClientService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<string> GetAsync(string url, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("请求 URL: {Url}", url);
        
        var response = await ExecuteWithRetry(async () => 
            await _httpClient.GetAsync(url, cancellationToken), 
            url);

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    public async Task<byte[]> GetBytesAsync(string url, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("下载文件：{Url}", url);
        
        var response = await ExecuteWithRetry(async () => 
            await _httpClient.GetAsync(url, cancellationToken), 
            url);

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    public async Task<Stream> GetStreamAsync(string url, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("下载流：{Url}", url);
        
        var response = await ExecuteWithRetry(async () => 
            await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken), 
            url);

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStreamAsync(cancellationToken);
    }

    private async Task<HttpResponseMessage> ExecuteWithRetry(
        Func<Task<HttpResponseMessage>> action, 
        string url)
    {
        var policy = Policy
            .Handle<HttpRequestException>()
            .OrResult<HttpResponseMessage>(r => 
                (int)r.StatusCode >= 500 || 
                r.StatusCode == System.Net.HttpStatusCode.RequestTimeout)
            .WaitAndRetryAsync(3, 
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (result, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning("请求失败，{TimeSpan}s 后重试 (第{RetryCount}次): {Url}", 
                        timeSpan.TotalSeconds, retryCount, url);
                });

        return await policy.ExecuteAsync(action);
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
    private readonly HuaduZYParser _parser;
    private readonly ILogger<VideoCrawlerService> _logger;

    public VideoCrawlerService(
        IHttpClientService httpClient,
        IVideoRepository videoRepository,
        ICrawlerTaskRepository taskRepository,
        IWorkerService workerService,
        IVideoCacheService cacheService,
        ILogger<VideoCrawlerService> logger)
    {
        _httpClient = httpClient;
        _videoRepository = videoRepository;
        _taskRepository = taskRepository;
        _workerService = workerService;
        _cacheService = cacheService;
        _parser = new HuaduZYParser();
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
            // 解析 URL 并爬取所有分页
            var baseUrl = task.TargetUrl;
            var allVideos = new List<Video>();

            // 爬取第 1 页
            _logger.LogInformation("开始爬取第 1 页：{Url}", baseUrl);
            var html = await _httpClient.GetAsync(baseUrl);
            var firstPageResult = await _parser.ParseVideoListWithPagingAsync(html, baseUrl, 1, 999);
            allVideos.AddRange(firstPageResult.Items);
            _logger.LogInformation("第 1 页爬取完成，共 {Count} 个视频", firstPageResult.Items.Count);

            // 爬取后续分页（如果有）
            if (firstPageResult.HasNext)
            {
                var currentPage = 2;
                while (currentPage <= 10 && firstPageResult.HasNext) // 最多爬取 10 页
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        task.Cancel();
                        break;
                    }

                    // 构建分页 URL - 花都影视的分页格式通常是 /page/2.html 或 ?page=2
                    var pageUrl = BuildPageUrl(baseUrl, currentPage);
                    _logger.LogInformation("开始爬取第 {Page} 页：{Url}", currentPage, pageUrl);

                    try
                    {
                        var pageHtml = await _httpClient.GetAsync(pageUrl);
                        var pageResult = await _parser.ParseVideoListWithPagingAsync(pageHtml, baseUrl, currentPage, 999);
                        
                        if (pageResult.Items.Any())
                        {
                            allVideos.AddRange(pageResult.Items);
                            _logger.LogInformation("第 {Page} 页爬取完成，共 {Count} 个视频", currentPage, pageResult.Items.Count);
                            currentPage++;
                        }
                        else
                        {
                            _logger.LogInformation("第 {Page} 页没有数据，停止爬取", currentPage);
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "第 {Page} 页爬取失败", currentPage);
                        break;
                    }
                }
            }

            task.TotalCount = allVideos.Count;
            _logger.LogInformation("所有页面爬取完成，共 {Count} 个视频", allVideos.Count);

            int success = 0, failed = 0, skipped = 0;
            
            foreach (var video in allVideos)
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

    /// <summary>
    /// 构建分页 URL
    /// </summary>
    private string BuildPageUrl(string baseUrl, int page)
    {
        // 花都影视的分页格式：/vodshow/xxx----------2.html
        if (baseUrl.EndsWith(".html"))
        {
            var basePart = baseUrl.Substring(0, baseUrl.Length - 5); // 去掉 .html
            if (basePart.EndsWith("-"))
            {
                return $"{basePart}{page}.html";
            }
            else
            {
                return $"{basePart}----------{page}.html";
            }
        }
        
        // 查询字符串格式：?page=2
        var separator = baseUrl.Contains("?") ? "&" : "?";
        return $"{baseUrl}{separator}page={page}";
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
        await Task.CompletedTask;
    }
}
