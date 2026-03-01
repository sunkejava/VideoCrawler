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

// Repositories
builder.Services.AddScoped<IVideoRepository, VideoRepository>();
builder.Services.AddScoped<ICrawlerTaskRepository, CrawlerTaskRepository>();
builder.Services.AddScoped<IWorkerService, WorkerService>();
builder.Services.AddScoped<IVideoCacheService, VideoCacheService>();

// Crawler services
builder.Services.AddScoped<IVideoCrawlerService, VideoCrawlerService>();
builder.Services.AddScoped<IVideoSiteParser, HuaduZYParser>();
builder.Services.AddScoped<IHttpClientService, HttpClientService>();

// HTTP Client with Polly
builder.Services.AddHttpClient<HttpClientService>((sp, client) =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
    client.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
    client.Timeout = TimeSpan.FromSeconds(30);
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
        Description = "视频爬取系统 API - .NET 10 DDD 架构"
    });
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVueApp", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
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
}

app.Run();
