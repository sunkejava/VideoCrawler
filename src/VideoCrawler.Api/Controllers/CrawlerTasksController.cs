using Microsoft.AspNetCore.Mvc;
using VideoCrawler.Application.DTOs;
using VideoCrawler.Domain.Interfaces;
using VideoCrawler.Domain.Entities;

namespace VideoCrawler.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CrawlerTasksController : ControllerBase
{
    private readonly ICrawlerTaskRepository _taskRepository;
    private readonly IVideoCrawlerService _crawlerService;
    private readonly IWorkerService _workerService;
    private readonly ILogger<CrawlerTasksController> _logger;

    public CrawlerTasksController(
        ICrawlerTaskRepository taskRepository,
        IVideoCrawlerService crawlerService,
        IWorkerService workerService,
        ILogger<CrawlerTasksController> logger)
    {
        _taskRepository = taskRepository;
        _crawlerService = crawlerService;
        _workerService = workerService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<CrawlerTaskDto>> CreateTask([FromBody] CreateTaskRequest request)
    {
        var task = await _crawlerService.CreateCrawlTaskAsync(request.TargetUrl, request.TaskType);
        
        return CreatedAtAction(nameof(GetTask), new { id = task.Id }, new CrawlerTaskDto
        {
            Id = task.Id,
            TaskName = task.TaskName,
            TargetUrl = task.TargetUrl,
            TaskType = task.TaskType,
            Status = task.Status,
            TotalCount = task.TotalCount,
            ProcessedCount = task.ProcessedCount,
            SuccessCount = task.SuccessCount,
            FailedCount = task.FailedCount,
            Priority = task.Priority,
            StartTime = task.StartTime,
            EndTime = task.EndTime
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CrawlerTaskDto>> GetTask(Guid id)
    {
        var task = await _taskRepository.GetByIdAsync(id);
        if (task == null)
            return NotFound();

        return Ok(new CrawlerTaskDto
        {
            Id = task.Id,
            TaskName = task.TaskName,
            TargetUrl = task.TargetUrl,
            TaskType = task.TaskType,
            Status = task.Status,
            TotalCount = task.TotalCount,
            ProcessedCount = task.ProcessedCount,
            SuccessCount = task.SuccessCount,
            FailedCount = task.FailedCount,
            ErrorMessage = task.ErrorMessage,
            StartTime = task.StartTime,
            EndTime = task.EndTime,
            AssignedWorker = task.AssignedWorker,
            Priority = task.Priority,
            RetryCount = task.RetryCount
        });
    }

    [HttpGet]
    public async Task<ActionResult<List<CrawlerTaskDto>>> GetTasks(
        [FromQuery] string? status = null,
        [FromQuery] int limit = 20)
    {
        List<CrawlerTask> tasks;

        if (string.IsNullOrEmpty(status))
        {
            tasks = await _taskRepository.GetAllAsync();
        }
        else
        {
            tasks = await _taskRepository.FindAsync(t => t.Status == status);
        }

        var dtos = tasks
            .OrderByDescending(t => t.CreatedAt)
            .Take(limit)
            .Select(t => new CrawlerTaskDto
            {
                Id = t.Id,
                TaskName = t.TaskName,
                TargetUrl = t.TargetUrl,
                TaskType = t.TaskType,
                Status = t.Status,
                TotalCount = t.TotalCount,
                ProcessedCount = t.ProcessedCount,
                SuccessCount = t.SuccessCount,
                FailedCount = t.FailedCount,
                ErrorMessage = t.ErrorMessage,
                StartTime = t.StartTime,
                EndTime = t.EndTime,
                AssignedWorker = t.AssignedWorker,
                Priority = t.Priority,
                RetryCount = t.RetryCount
            })
            .ToList();

        return Ok(dtos);
    }

    [HttpPost("{id:guid}/start")]
    public async Task<ActionResult> StartTask(Guid id)
    {
        var task = await _taskRepository.GetByIdAsync(id);
        if (task == null)
            return NotFound();

        // 后台执行任务
        _ = Task.Run(async () =>
        {
            try
            {
                await _crawlerService.ExecuteCrawlTaskAsync(id);
                _logger.LogInformation("任务完成：{TaskId}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "任务执行失败：{TaskId}", id);
            }
        });

        return Ok(new { message = "任务已启动", taskId = id });
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult> CancelTask(Guid id)
    {
        var task = await _taskRepository.GetByIdAsync(id);
        if (task == null)
            return NotFound();

        task.Cancel();
        await _taskRepository.UpdateAsync(task);

        return Ok(new { message = "任务已取消", taskId = id });
    }

    [HttpPost("{id:guid}/retry")]
    public async Task<ActionResult> RetryTask(Guid id)
    {
        var task = await _taskRepository.GetByIdAsync(id);
        if (task == null)
            return NotFound();

        task.Retry();
        await _taskRepository.UpdateAsync(task);

        return Ok(new { message = "任务已重新排队", taskId = id });
    }

    [HttpGet("workers")]
    public async Task<ActionResult<List<WorkerDto>>> GetWorkers()
    {
        var workers = await _workerService.GetAvailableWorkersAsync();
        
        // TODO: 获取所有工人信息
        return Ok(new List<WorkerDto>());
    }
}

public class CreateTaskRequest
{
    public string TargetUrl { get; set; } = string.Empty;
    public string TaskType { get; set; } = "Incremental"; // Full, Incremental, Single
}
