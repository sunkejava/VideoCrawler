using Microsoft.AspNetCore.Mvc;
using VideoCrawler.Infrastructure.Crawler;

namespace VideoCrawler.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DebugController : ControllerBase
{
    private readonly IVideoCrawlerService _crawlerService;
    private readonly ILogger<DebugController> _logger;

    public DebugController(
        IVideoCrawlerService crawlerService,
        ILogger<DebugController> logger)
    {
        _crawlerService = crawlerService;
        _logger = logger;
    }

    /// <summary>
    /// 分析网站结构
    /// </summary>
    [HttpGet("analyze")]
    public async Task<ActionResult<SiteAnalysisResult>> AnalyzeSite([FromQuery] string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return BadRequest("请提供 URL 参数");
        }

        var analyzer = new SiteAnalyzer();
        var result = await analyzer.AnalyzeAsync(url);

        return Ok(result);
    }

    /// <summary>
    /// 测试爬取视频列表
    /// </summary>
    [HttpGet("test-list")]
    public async Task<ActionResult<TestCrawlResult>> TestCrawlList(
        [FromQuery] string url, 
        [FromQuery] int maxCount = 10)
    {
        if (string.IsNullOrEmpty(url))
        {
            return BadRequest("请提供 URL 参数");
        }

        try
        {
            var videos = await _crawlerService.FetchVideoListAsync(url, maxCount);
            
            return Ok(new TestCrawlResult
            {
                Success = true,
                Url = url,
                Count = videos.Count,
                Videos = videos.Select(v => new VideoSummary
                {
                    Title = v.Title,
                    Url = v.SourceUrl,
                    CoverImage = v.CoverImage,
                    Category = v.Category
                }).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "测试爬取失败");
            return Ok(new TestCrawlResult
            {
                Success = false,
                Url = url,
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// 测试爬取视频详情
    /// </summary>
    [HttpGet("test-detail")]
    public async Task<ActionResult<VideoSummary>> TestCrawlDetail([FromQuery] string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return BadRequest("请提供 URL 参数");
        }

        try
        {
            var video = await _crawlerService.FetchVideoDetailAsync(url);
            
            if (video == null)
            {
                return NotFound("无法解析视频详情");
            }

            return Ok(new VideoSummary
            {
                Title = video.Title,
                Url = video.SourceUrl,
                CoverImage = video.CoverImage,
                Category = video.Category,
                Description = video.Description,
                Actor = video.Actor,
                Director = video.Director,
                PublishYear = video.PublishYear,
                M3u8Url = video.M3u8Url
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "测试爬取详情失败");
            return BadRequest(new { error = ex.Message });
        }
    }
}

public class TestCrawlResult
{
    public bool Success { get; set; }
    public string Url { get; set; } = "";
    public string? Error { get; set; }
    public int Count { get; set; }
    public List<VideoSummary> Videos { get; set; } = new();
}

public class VideoSummary
{
    public string Title { get; set; } = "";
    public string Url { get; set; } = "";
    public string? CoverImage { get; set; }
    public string? Category { get; set; }
    public string? Description { get; set; }
    public string? Actor { get; set; }
    public string? Director { get; set; }
    public int? PublishYear { get; set; }
    public string? M3u8Url { get; set; }
}
