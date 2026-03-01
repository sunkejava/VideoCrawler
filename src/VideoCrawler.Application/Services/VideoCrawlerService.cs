using VideoCrawler.Application.DTOs;
using VideoCrawler.Domain.Entities;
using VideoCrawler.Domain.Interfaces;

namespace VideoCrawler.Application.Services;

public class VideoCrawlerService : IVideoCrawlerService
{
    private readonly IVideoRepository _videoRepository;
    private readonly ICrawlerTaskRepository _taskRepository;
    private readonly IWorkerService _workerService;
    private readonly IVideoCacheService _cacheService;
    private readonly ILogger<VideoCrawlerService> _logger;

    public VideoCrawlerService(
        IVideoRepository videoRepository,
        ICrawlerTaskRepository taskRepository,
        IWorkerService workerService,
        IVideoCacheService cacheService,
        ILogger<VideoCrawlerService> logger)
    {
        _videoRepository = videoRepository;
        _taskRepository = taskRepository;
        _workerService = workerService;
        _cacheService = cacheService;
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
            var videos = await FetchVideoListAsync(task.TargetUrl);
            task.TotalCount = videos.Count;

            int success = 0, failed = 0;
            foreach (var video in videos)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    task.Cancel();
                    break;
                }

                try
                {
                    // 检查是否已缓存
                    if (await _cacheService.IsCachedAsync(video.SourceUrl))
                    {
                        _logger.LogInformation("视频已缓存，跳过：{Title}", video.Title);
                        success++;
                        continue;
                    }

                    // 爬取详情
                    var detail = await FetchVideoDetailAsync(video.SourceUrl);
                    if (detail != null)
                    {
                        // 缓存视频和封面
                        await _cacheService.CacheVideoAsync(detail);
                        await _cacheService.CacheCoverImageAsync(detail);
                        
                        await _videoRepository.AddAsync(detail);
                        success++;
                    }
                    else
                    {
                        failed++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "爬取视频失败：{Title}", video.Title);
                    failed++;
                }

                task.UpdateProgress(success + failed, success, failed);
                await _taskRepository.UpdateAsync(task);
            }

            task.Complete(success, failed);
            await _taskRepository.UpdateAsync(task);
            
            await _workerService.UpdateWorkerStatusAsync(workerId!, "Idle");
        }
        catch (Exception ex)
        {
            task.Fail(ex.Message);
            await _taskRepository.UpdateAsync(task);
            await _workerService.UpdateWorkerStatusAsync(workerId!, "Idle");
            throw;
        }
    }

    public async Task<Video?> FetchVideoDetailAsync(string url)
    {
        // TODO: 实现具体网站解析逻辑
        // 需要根据目标网站 HTML 结构解析
        _logger.LogInformation("爬取视频详情：{Url}", url);
        
        return await Task.FromResult<Video?>(null);
    }

    public async Task<List<Video>> FetchVideoListAsync(string listUrl, int maxCount = 100)
    {
        // TODO: 实现具体网站列表解析逻辑
        _logger.LogInformation("爬取视频列表：{Url}, 最大数量：{MaxCount}", listUrl, maxCount);
        
        return await Task.FromResult(new List<Video>());
    }

    public Task<bool> DownloadVideoAsync(Video video, string savePath)
    {
        // TODO: 实现 M3U8 下载
        throw new NotImplementedException();
    }

    public Task<bool> DownloadCoverImageAsync(Video video, string savePath)
    {
        // TODO: 实现封面下载
        throw new NotImplementedException();
    }

    public Task ParseM3u8Async(string m3u8Url, string savePath)
    {
        // TODO: 实现 M3U8 解析
        throw new NotImplementedException();
    }
}
