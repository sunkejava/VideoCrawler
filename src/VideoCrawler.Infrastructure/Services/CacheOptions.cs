namespace VideoCrawler.Infrastructure.Services;

public class CacheOptions
{
    public string CachePath { get; set; } = "./cache";
    public int DefaultExpirationDays { get; set; } = 30;
    public long MaxSizeGB { get; set; } = 10;
    
    public TimeSpan DefaultExpiration => TimeSpan.FromDays(DefaultExpirationDays);
    public long MaxCacheSizeBytes => MaxSizeGB * 1024 * 1024 * 1024;
}

public class CrawlerOptions
{
    public int MaxPages { get; set; } = 10;
    public int VideosPerPage { get; set; } = 40;
    public int RequestDelayMs { get; set; } = 1000;
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxRetries { get; set; } = 3;
}
