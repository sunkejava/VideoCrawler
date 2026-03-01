using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using VideoCrawler.Domain.Interfaces;
using VideoCrawler.Infrastructure.Data;
using VideoCrawler.Infrastructure.Repositories;
using VideoCrawler.Infrastructure.Services;
using VideoCrawler.Application.Services;
using VideoCrawler.Infrastructure.Crawler;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? "Data Source=vodcrawler.db"));

// Configure options
builder.Services.Configure<CacheOptions>(builder.Configuration.GetSection("Cache"));
builder.Services.Configure<CrawlerOptions>(builder.Configuration.GetSection("Crawler"));

// Repositories
builder.Services.AddScoped<IVideoRepository, VideoRepository>();
builder.Services.AddScoped<ICrawlerTaskRepository, CrawlerTaskRepository>();
builder.Services.AddScoped<IWorkerService, WorkerService>();
builder.Services.AddScoped<IVideoCacheService, VideoCacheService>();

// Crawler services
builder.Services.AddScoped<IVideoCrawlerService, VideoCrawlerService>();
builder.Services.AddScoped<IHttpClientService, HttpClientService>();

// HTTP Client with Polly
builder.Services.AddHttpClient<HttpClientService>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<CrawlerOptions>>().Value;
    client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
    client.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
    client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("zh-CN,zh;q=0.9");
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
})
.AddPolicyHandler(GetRetryPolicy())
.AddPolicyHandler(GetTimeoutPolicy());

static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
}

static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.RequestTimeout)
        .WaitAndRetryAsync(1, _ => TimeSpan.FromSeconds(10));
}

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "VideoCrawler API", 
        Version = "v1",
        Description = "视频爬取系统 API - .NET 10 DDD 架构\n\n支持自动爬取、缓存管理、任务调度等功能"
    });
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVueApp", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "VideoCrawler API V1");
        c.RoutePrefix = string.Empty; // 设置 Swagger UI 为根路径
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowVueApp");
app.UseAuthorization();
app.MapControllers();

// 初始化数据库
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
    Console.WriteLine("✅ 数据库初始化完成");
}

// 创建缓存目录
var cacheOptions = builder.Configuration.GetSection("Cache").Get<CacheOptions>() ?? new CacheOptions();
Directory.CreateDirectory(cacheOptions.CachePath);
Directory.CreateDirectory(Path.Combine(cacheOptions.CachePath, "videos"));
Directory.CreateDirectory(Path.Combine(cacheOptions.CachePath, "covers"));
Console.WriteLine($"✅ 缓存目录已创建：{cacheOptions.CachePath}");

Console.WriteLine("\n========================================");
Console.WriteLine("🎬 VideoCrawler API 已启动");
Console.WriteLine("========================================");
Console.WriteLine($"📊 Swagger UI: http://localhost:5000");
Console.WriteLine($"📁 缓存路径：{cacheOptions.CachePath}");
Console.WriteLine($"📄 数据库：vodcrawler.db");
Console.WriteLine("========================================\n");

app.Run();
