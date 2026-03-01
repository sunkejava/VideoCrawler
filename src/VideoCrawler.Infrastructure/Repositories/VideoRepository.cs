using Microsoft.EntityFrameworkCore;
using VideoCrawler.Domain.Entities;
using VideoCrawler.Domain.Interfaces;
using VideoCrawler.Infrastructure.Data;

namespace VideoCrawler.Infrastructure.Repositories;

public class VideoRepository : IVideoRepository
{
    private readonly ApplicationDbContext _context;

    public VideoRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Video?> GetByIdAsync(Guid id)
    {
        return await _context.Videos.FindAsync(id);
    }

    public async Task<List<Video>> GetAllAsync()
    {
        return await _context.Videos.ToListAsync();
    }

    public async Task<List<Video>> FindAsync(Func<Video, bool> predicate)
    {
        return _context.Videos.Where(predicate).ToList();
    }

    public async Task<Video> AddAsync(Video entity)
    {
        await _context.Videos.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task UpdateAsync(Video entity)
    {
        _context.Videos.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Video entity)
    {
        _context.Videos.Remove(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteByIdAsync(Guid id)
    {
        var entity = await GetByIdAsync(id);
        if (entity != null)
        {
            await DeleteAsync(entity);
        }
    }

    public async Task<int> CountAsync()
    {
        return await _context.Videos.CountAsync();
    }

    public async Task<Video?> GetBySourceUrlAsync(string sourceUrl)
    {
        return await _context.Videos.FirstOrDefaultAsync(v => v.SourceUrl == sourceUrl);
    }

    public async Task<List<Video>> GetByCategoryAsync(string category, int page, int pageSize)
    {
        return await _context.Videos
            .Where(v => v.Category == category)
            .OrderByDescending(v => v.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<List<Video>> SearchAsync(string keyword, int page, int pageSize)
    {
        return await _context.Videos
            .Where(v => v.Title.Contains(keyword) || (v.Description != null && v.Description.Contains(keyword)))
            .OrderByDescending(v => v.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<List<Video>> GetCachedVideosAsync(int page, int pageSize)
    {
        return await _context.Videos
            .Where(v => v.IsCached)
            .OrderByDescending(v => v.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<List<Video>> GetUncachedVideosAsync(int page, int pageSize)
    {
        return await _context.Videos
            .Where(v => !v.IsCached)
            .OrderByDescending(v => v.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetTotalCountAsync()
    {
        return await _context.Videos.CountAsync();
    }

    public async Task<int> GetCachedCountAsync()
    {
        return await _context.Videos.CountAsync(v => v.IsCached);
    }

    public async Task UpdateBatchAsync(IEnumerable<Video> videos)
    {
        _context.Videos.UpdateRange(videos);
        await _context.SaveChangesAsync();
    }
}

public class CrawlerTaskRepository : ICrawlerTaskRepository
{
    private readonly ApplicationDbContext _context;

    public CrawlerTaskRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CrawlerTask?> GetByIdAsync(Guid id)
    {
        return await _context.CrawlerTasks.FindAsync(id);
    }

    public async Task<List<CrawlerTask>> GetAllAsync()
    {
        return await _context.CrawlerTasks.ToListAsync();
    }

    public async Task<List<CrawlerTask>> FindAsync(Func<CrawlerTask, bool> predicate)
    {
        return _context.CrawlerTasks.Where(predicate).ToList();
    }

    public async Task<CrawlerTask> AddAsync(CrawlerTask entity)
    {
        await _context.CrawlerTasks.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task UpdateAsync(CrawlerTask entity)
    {
        _context.CrawlerTasks.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(CrawlerTask entity)
    {
        _context.CrawlerTasks.Remove(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteByIdAsync(Guid id)
    {
        var entity = await GetByIdAsync(id);
        if (entity != null)
        {
            await DeleteAsync(entity);
        }
    }

    public async Task<int> CountAsync()
    {
        return await _context.CrawlerTasks.CountAsync();
    }

    public async Task<List<CrawlerTask>> GetPendingTasksAsync(int maxCount = 10)
    {
        return await _context.CrawlerTasks
            .Where(t => t.Status == "Pending")
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.CreatedAt)
            .Take(maxCount)
            .ToListAsync();
    }

    public async Task<List<CrawlerTask>> GetRunningTasksAsync()
    {
        return await _context.CrawlerTasks
            .Where(t => t.Status == "Running")
            .ToListAsync();
    }

    public async Task<List<CrawlerTask>> GetCompletedTasksAsync(DateTime from, DateTime to)
    {
        return await _context.CrawlerTasks
            .Where(t => t.Status == "Completed" && t.EndTime >= from && t.EndTime <= to)
            .ToListAsync();
    }

    public async Task<List<CrawlerTask>> GetFailedTasksAsync(int maxCount = 10)
    {
        return await _context.CrawlerTasks
            .Where(t => t.Status == "Failed")
            .OrderByDescending(t => t.EndTime)
            .Take(maxCount)
            .ToListAsync();
    }

    public async Task<CrawlerTask?> GetNextPendingTaskAsync()
    {
        return await _context.CrawlerTasks
            .Where(t => t.Status == "Pending")
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task UpdateStatusAsync(Guid taskId, string status)
    {
        var task = await GetByIdAsync(taskId);
        if (task != null)
        {
            task.Status = status;
            await UpdateAsync(task);
        }
    }
}
