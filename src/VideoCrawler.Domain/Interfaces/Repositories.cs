using VideoCrawler.Domain.Entities;

namespace VideoCrawler.Domain.Interfaces;

/// <summary>
/// 视频仓储接口
/// </summary>
public interface IVideoRepository : IRepository<Video>
{
    Task<Video?> GetBySourceUrlAsync(string sourceUrl);
    Task<List<Video>> GetByCategoryAsync(string category, int page, int pageSize);
    Task<List<Video>> SearchAsync(string keyword, int page, int pageSize);
    Task<List<Video>> GetCachedVideosAsync(int page, int pageSize);
    Task<List<Video>> GetUncachedVideosAsync(int page, int pageSize);
    Task<int> GetTotalCountAsync();
    Task<int> GetCachedCountAsync();
    Task UpdateBatchAsync(IEnumerable<Video> videos);
}

/// <summary>
/// 爬取任务仓储接口
/// </summary>
public interface ICrawlerTaskRepository : IRepository<CrawlerTask>
{
    Task<List<CrawlerTask>> GetPendingTasksAsync(int maxCount = 10);
    Task<List<CrawlerTask>> GetRunningTasksAsync();
    Task<List<CrawlerTask>> GetCompletedTasksAsync(DateTime from, DateTime to);
    Task<List<CrawlerTask>> GetFailedTasksAsync(int maxCount = 10);
    Task<CrawlerTask?> GetNextPendingTaskAsync();
    Task UpdateStatusAsync(Guid taskId, string status);
}

/// <summary>
/// 通用仓储接口
/// </summary>
public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id);
    Task<List<T>> GetAllAsync();
    Task<List<T>> FindAsync(Func<T, bool> predicate);
    Task<T> AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);
    Task DeleteByIdAsync(Guid id);
    Task<int> CountAsync();
}
