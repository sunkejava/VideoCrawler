namespace VideoCrawler.Application.DTOs;

public class VideoDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? CoverImage { get; set; }
    public string? CoverImageLocal { get; set; }
    public string VideoUrl { get; set; } = string.Empty;
    public string? VideoUrlLocal { get; set; }
    public string? M3u8Url { get; set; }
    public string? Category { get; set; }
    public string? Tags { get; set; }
    public string? Actor { get; set; }
    public string? Director { get; set; }
    public int? Duration { get; set; }
    public int? PublishYear { get; set; }
    public string? Area { get; set; }
    public string? Language { get; set; }
    public decimal? Rating { get; set; }
    public int? EpisodeCount { get; set; }
    public string? CurrentEpisode { get; set; }
    public string? Status { get; set; }
    public string SourceUrl { get; set; } = string.Empty;
    public string SourceSite { get; set; } = "HuaduZY";
    public DateTime? CrawlTime { get; set; }
    public DateTime? LastUpdateTime { get; set; }
    public bool IsCached { get; set; }
    public string? CachePath { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CrawlerTaskDto
{
    public Guid Id { get; set; }
    public string TaskName { get; set; } = string.Empty;
    public string TargetUrl { get; set; } = string.Empty;
    public string TaskType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int TotalCount { get; set; }
    public int ProcessedCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string? AssignedWorker { get; set; }
    public int Priority { get; set; }
    public int RetryCount { get; set; }
    public double Progress => TotalCount > 0 ? (double)ProcessedCount / TotalCount * 100 : 0;
}

public class WorkerDto
{
    public string WorkerId { get; set; } = string.Empty;
    public string WorkerName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? CurrentTaskId { get; set; }
    public int CompletedTasks { get; set; }
    public int FailedTasks { get; set; }
    public DateTime LastHeartbeat { get; set; }
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);

    public static PagedResult<T> Create(List<T> items, int total, int page, int pageSize)
    {
        return new PagedResult<T>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }
}
