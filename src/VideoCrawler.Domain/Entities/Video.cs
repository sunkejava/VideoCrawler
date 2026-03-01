namespace VideoCrawler.Domain.Entities;

/// <summary>
/// 视频实体
/// </summary>
public class Video : BaseEntity
{
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? CoverImage { get; private set; }
    public string? CoverImageLocal { get; private set; }
    public string VideoUrl { get; private set; } = string.Empty;
    public string? VideoUrlLocal { get; private set; }
    public string? M3u8Url { get; private set; }
    public string? Category { get; private set; }
    public string? Tags { get; private set; }
    public string? Actor { get; private set; }
    public string? Director { get; private set; }
    public int? Duration { get; private set; }
    public int? PublishYear { get; private set; }
    public string? Area { get; private set; }
    public string? Language { get; private set; }
    public decimal? Rating { get; private set; }
    public int? EpisodeCount { get; private set; }
    public string? CurrentEpisode { get; private set; }
    public string? Status { get; private set; }
    public string SourceUrl { get; private set; } = string.Empty;
    public string SourceSite { get; private set; } = "HuaduZY";
    public DateTime? CrawlTime { get; private set; }
    public DateTime? LastUpdateTime { get; private set; }
    public bool IsCached { get; private set; }
    public string? CachePath { get; private set; }

    public Video()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateCacheInfo(string localPath, string m3u8Path)
    {
        IsCached = true;
        CachePath = localPath;
        VideoUrlLocal = localPath;
        M3u8Url = m3u8Path;
        LastUpdateTime = DateTime.UtcNow;
    }

    public void UpdateCoverImageLocal(string localPath)
    {
        CoverImageLocal = localPath;
        LastUpdateTime = DateTime.UtcNow;
    }

    public void MarkAsCrawled(string sourceUrl)
    {
        SourceUrl = sourceUrl;
        CrawlTime = DateTime.UtcNow;
        LastUpdateTime = DateTime.UtcNow;
    }
}

/// <summary>
/// 基础实体类
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}
