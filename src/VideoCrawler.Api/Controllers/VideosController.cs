using Microsoft.AspNetCore.Mvc;
using VideoCrawler.Application.DTOs;
using VideoCrawler.Domain.Interfaces;
using VideoCrawler.Domain.Entities;

namespace VideoCrawler.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VideosController : ControllerBase
{
    private readonly IVideoRepository _videoRepository;
    private readonly IVideoCrawlerService _crawlerService;
    private readonly ILogger<VideosController> _logger;

    public VideosController(
        IVideoRepository videoRepository,
        IVideoCrawlerService crawlerService,
        ILogger<VideosController> logger)
    {
        _videoRepository = videoRepository;
        _crawlerService = crawlerService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<VideoDto>>> GetVideos(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 20)
    {
        var videos = await _videoRepository.GetAllAsync();
        var total = videos.Count;
        
        var pagedVideos = videos
            .OrderByDescending(v => v.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(v => new VideoDto
            {
                Id = v.Id,
                Title = v.Title,
                Description = v.Description,
                CoverImage = v.CoverImage,
                CoverImageLocal = v.CoverImageLocal,
                VideoUrl = v.VideoUrl,
                VideoUrlLocal = v.VideoUrlLocal,
                M3u8Url = v.M3u8Url,
                Category = v.Category,
                Tags = v.Tags,
                Actor = v.Actor,
                Director = v.Director,
                Duration = v.Duration,
                PublishYear = v.PublishYear,
                Area = v.Area,
                Language = v.Language,
                Rating = v.Rating,
                EpisodeCount = v.EpisodeCount,
                CurrentEpisode = v.CurrentEpisode,
                Status = v.Status,
                SourceUrl = v.SourceUrl,
                SourceSite = v.SourceSite,
                CrawlTime = v.CrawlTime,
                LastUpdateTime = v.LastUpdateTime,
                IsCached = v.IsCached,
                CachePath = v.CachePath,
                CreatedAt = v.CreatedAt
            })
            .ToList();

        return Ok(PagedResult<VideoDto>.Create(pagedVideos, total, page, pageSize));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<VideoDto>> GetVideo(Guid id)
    {
        var video = await _videoRepository.GetByIdAsync(id);
        if (video == null)
            return NotFound();

        return Ok(new VideoDto
        {
            Id = video.Id,
            Title = video.Title,
            Description = video.Description,
            CoverImage = video.CoverImage,
            CoverImageLocal = video.CoverImageLocal,
            VideoUrl = video.VideoUrl,
            VideoUrlLocal = video.VideoUrlLocal,
            M3u8Url = video.M3u8Url,
            Category = video.Category,
            Tags = video.Tags,
            Actor = video.Actor,
            Director = video.Director,
            Duration = video.Duration,
            PublishYear = video.PublishYear,
            Area = video.Area,
            Language = video.Language,
            Rating = video.Rating,
            EpisodeCount = video.EpisodeCount,
            CurrentEpisode = video.CurrentEpisode,
            Status = video.Status,
            SourceUrl = video.SourceUrl,
            SourceSite = video.SourceSite,
            CrawlTime = video.CrawlTime,
            LastUpdateTime = video.LastUpdateTime,
            IsCached = video.IsCached,
            CachePath = video.CachePath,
            CreatedAt = video.CreatedAt
        });
    }

    [HttpGet("category/{category}")]
    public async Task<ActionResult<PagedResult<VideoDto>>> GetByCategory(
        string category, 
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 20)
    {
        var videos = await _videoRepository.GetByCategoryAsync(category, page, pageSize);
        var total = await _videoRepository.GetTotalCountAsync();

        var dtos = videos.Select(v => new VideoDto
        {
            Id = v.Id,
            Title = v.Title,
            Category = v.Category,
            CoverImage = v.CoverImage,
            CoverImageLocal = v.CoverImageLocal,
            VideoUrl = v.VideoUrl,
            IsCached = v.IsCached,
            CreatedAt = v.CreatedAt
        }).ToList();

        return Ok(PagedResult<VideoDto>.Create(dtos, total, page, pageSize));
    }

    [HttpGet("search")]
    public async Task<ActionResult<PagedResult<VideoDto>>> Search(
        [FromQuery] string keyword,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var videos = await _videoRepository.SearchAsync(keyword, page, pageSize);
        var total = await _videoRepository.GetTotalCountAsync();

        var dtos = videos.Select(v => new VideoDto
        {
            Id = v.Id,
            Title = v.Title,
            Category = v.Category,
            CoverImage = v.CoverImage,
            IsCached = v.IsCached,
            CreatedAt = v.CreatedAt
        }).ToList();

        return Ok(PagedResult<VideoDto>.Create(dtos, total, page, pageSize));
    }

    [HttpGet("cached")]
    public async Task<ActionResult<PagedResult<VideoDto>>> GetCachedVideos(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var videos = await _videoRepository.GetCachedVideosAsync(page, pageSize);
        var total = await _videoRepository.GetCachedCountAsync();

        var dtos = videos.Select(v => new VideoDto
        {
            Id = v.Id,
            Title = v.Title,
            Category = v.Category,
            CoverImageLocal = v.CoverImageLocal,
            VideoUrlLocal = v.VideoUrlLocal,
            IsCached = v.IsCached,
            CachePath = v.CachePath,
            CreatedAt = v.CreatedAt
        }).ToList();

        return Ok(PagedResult<VideoDto>.Create(dtos, total, page, pageSize));
    }
}
