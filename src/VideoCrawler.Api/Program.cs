using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using VideoCrawler.Domain.Interfaces;
using VideoCrawler.Infrastructure.Data;
using VideoCrawler.Infrastructure.Repositories;
using VideoCrawler.Infrastructure.Services;
using VideoCrawler.Application.Services;

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
builder.Services.AddScoped<IVideoCrawlerService, VideoCrawlerService>();

// Configure cache options
builder.Services.Configure<CacheOptions>(options =>
{
    options.CachePath = builder.Configuration.GetValue<string>("Cache:Path") ?? "./cache";
    options.DefaultExpiration = TimeSpan.FromDays(30);
    options.MaxCacheSizeBytes = 10L * 1024 * 1024 * 1024;
});

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
