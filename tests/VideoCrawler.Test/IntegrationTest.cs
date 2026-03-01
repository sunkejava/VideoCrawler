using System.Text;
using VideoCrawler.Infrastructure.Crawler;
using VideoCrawler.Infrastructure.Data;
using VideoCrawler.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace VideoCrawler.Test;

public class IntegrationTest
{
    public static async Task RunAllTestsAsync()
    {
        Console.OutputEncoding = Encoding.UTF8;
        
        Console.WriteLine("╔════════════════════════════════════════════════════════╗");
        Console.WriteLine("║          🎬 VideoCrawler 完整功能测试                  ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════╝\n");

        var testResults = new List<TestResult>();

        // 测试 1: 数据库初始化
        testResults.Add(await TestDatabaseInitAsync());

        // 测试 2: 网站结构分析
        testResults.Add(await TestSiteAnalysisAsync());

        // 测试 3: 视频列表爬取
        testResults.Add(await TestCrawlVideoListAsync());

        // 测试 4: 视频详情爬取
        testResults.Add(await TestCrawlVideoDetailAsync());

        // 测试 5: 数据持久化
        testResults.Add(await TestDataPersistenceAsync());

        // 输出总结
        PrintSummary(testResults);
    }

    private static async Task<TestResult> TestDatabaseInitAsync()
    {
        Console.WriteLine("【测试 1】数据库初始化测试");
        Console.WriteLine(new string('-', 50));

        try
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite("Data Source=test_vodcrawler.db")
                .Options;

            using var context = new ApplicationDbContext(options);
            await context.Database.EnsureCreatedAsync();

            var videoCount = await context.Videos.CountAsync();
            var taskCount = await context.CrawlerTasks.CountAsync();

            Console.WriteLine($"✅ 数据库创建成功");
            Console.WriteLine($"   - 当前视频数：{videoCount}");
            Console.WriteLine($"   - 当前任务数：{taskCount}");
            
            // 清理测试数据库
            File.Delete("test_vodcrawler.db");

            return new TestResult 
            { 
                Name = "数据库初始化", 
                Success = true,
                Message = $"数据库创建成功，视频表：{videoCount}条，任务表：{taskCount}条"
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 测试失败：{ex.Message}");
            return new TestResult 
            { 
                Name = "数据库初始化", 
                Success = false,
                Message = ex.Message
            };
        }
    }

    private static async Task<TestResult> TestSiteAnalysisAsync()
    {
        Console.WriteLine("\n【测试 2】网站结构分析测试");
        Console.WriteLine(new string('-', 50));

        var targetUrl = "https://b.huaduzy.cc/vodshow/tangxinVlog-----------.html";
        Console.WriteLine($"目标 URL: {targetUrl}\n");

        try
        {
            var analyzer = new SiteAnalyzer();
            var result = await analyzer.AnalyzeAsync(targetUrl);

            if (result.Success)
            {
                Console.WriteLine($"✅ 网站分析成功");
                Console.WriteLine($"   - 网站标题：{result.Title}");
                Console.WriteLine($"   - HTML 长度：{result.HtmlLength:N0} 字符");
                Console.WriteLine($"   - 总链接数：{result.TotalLinks}");
                Console.WriteLine($"   - 视频链接：{result.VideoLinks.Count}");
                Console.WriteLine($"   - 推荐解析器：{result.RecommendedParser}");

                return new TestResult
                {
                    Name = "网站结构分析",
                    Success = true,
                    Message = $"分析成功，发现{result.VideoLinks.Count}个视频链接"
                };
            }
            else
            {
                Console.WriteLine($"❌ 分析失败：{result.Error}");
                return new TestResult
                {
                    Name = "网站结构分析",
                    Success = false,
                    Message = result.Error ?? "未知错误"
                };
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 测试失败：{ex.Message}");
            return new TestResult
            {
                Name = "网站结构分析",
                Success = false,
                Message = ex.Message
            };
        }
    }

    private static async Task<TestResult> TestCrawlVideoListAsync()
    {
        Console.WriteLine("\n【测试 3】视频列表爬取测试");
        Console.WriteLine(new string('-', 50));

        var targetUrl = "https://b.huaduzy.cc/vodshow/tangxinVlog-----------.html";
        var parser = new HuaduZYParser();

        try
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            Console.WriteLine("正在获取页面...");
            var html = await httpClient.GetStringAsync(targetUrl);

            Console.WriteLine("正在解析视频列表...");
            var videos = await parser.ParseVideoListAsync(html, targetUrl);

            Console.WriteLine($"\n✅ 爬取成功！共 {videos.Count} 个视频\n");

            if (videos.Any())
            {
                Console.WriteLine($"{'序号',-6} {'标题',-45} {'分类',-15} {'封面'}");
                Console.WriteLine(new string('-', 80));

                foreach (var (video, index) in videos.Take(10).Select((v, i) => (v, i + 1)))
                {
                    var title = video.Title.Length > 43 ? video.Title[..43] + "..." : video.Title;
                    var category = video.Category ?? "N/A";
                    var hasCover = !string.IsNullOrEmpty(video.CoverImage) ? "✅" : "❌";
                    Console.WriteLine($"{index,-6} {title,-45} {category,-15} {hasCover}");
                }

                if (videos.Count > 10)
                {
                    Console.WriteLine($"... 还有 {videos.Count - 10} 个视频");
                }

                // 统计信息
                var withCover = videos.Count(v => !string.IsNullOrEmpty(v.CoverImage));
                var withCategory = videos.Count(v => !string.IsNullOrEmpty(v.Category));

                Console.WriteLine($"\n📊 统计信息:");
                Console.WriteLine($"   - 总视频数：{videos.Count}");
                Console.WriteLine($"   - 有封面：{withCover} ({withCover * 100 / videos.Count}%)");
                Console.WriteLine($"   - 有分类：{withCategory} ({withCategory * 100 / videos.Count}%)");

                return new TestResult
                {
                    Name = "视频列表爬取",
                    Success = true,
                    Message = $"爬取{videos.Count}个视频，{withCover}个有封面"
                };
            }
            else
            {
                Console.WriteLine("⚠️ 未解析到视频");
                return new TestResult
                {
                    Name = "视频列表爬取",
                    Success = false,
                    Message = "未解析到视频"
                };
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 测试失败：{ex.Message}");
            return new TestResult
            {
                Name = "视频列表爬取",
                Success = false,
                Message = ex.Message
            };
        }
    }

    private static async Task<TestResult> TestCrawlVideoDetailAsync()
    {
        Console.WriteLine("\n【测试 4】视频详情爬取测试");
        Console.WriteLine(new string('-', 50));

        var targetUrl = "https://b.huaduzy.cc/vodshow/tangxinVlog-----------.html";
        var parser = new HuaduZYParser();

        try
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            // 先获取列表
            var listHtml = await httpClient.GetStringAsync(targetUrl);
            var videos = await parser.ParseVideoListAsync(listHtml, targetUrl);

            if (!videos.Any())
            {
                Console.WriteLine("⚠️ 没有视频可用于详情测试");
                return new TestResult
                {
                    Name = "视频详情爬取",
                    Success = false,
                    Message = "列表为空"
                };
            }

            // 测试第一个视频的详情
            var testVideo = videos.First();
            Console.WriteLine($"测试视频：{testVideo.Title}");
            Console.WriteLine($"详情 URL: {testVideo.SourceUrl}\n");

            var detailHtml = await httpClient.GetStringAsync(testVideo.SourceUrl);
            var detail = await parser.ParseVideoDetailAsync(detailHtml, testVideo.SourceUrl);

            if (detail != null)
            {
                Console.WriteLine("✅ 详情解析成功!\n");
                Console.WriteLine($"  📺 标题：{detail.Title}");
                Console.WriteLine($"  📝 描述：{GetTruncatedText(detail.Description, 50)}");
                Console.WriteLine($"  🖼️ 封面：{(detail.CoverImage != null ? "✅" : "❌")}");
                Console.WriteLine($"  📁 分类：{detail.Category ?? "N/A"}");
                Console.WriteLine($"  🎭 演员：{GetTruncatedText(detail.Actor, 30)}");
                Console.WriteLine($"  🎬 导演：{detail.Director ?? "N/A"}");
                Console.WriteLine($"  📅 年份：{(detail.PublishYear?.ToString() ?? "N/A")}");
                Console.WriteLine($"  🌍 地区：{detail.Area ?? "N/A"}");
                Console.WriteLine($"  📊 集数：{(detail.EpisodeCount?.ToString() ?? "N/A")}");
                Console.WriteLine($"  ⭐ 评分：{(detail.Rating?.ToString() ?? "N/A")}");
                Console.WriteLine($"  🔗 M3U8: {(detail.M3u8Url != null ? "✅" : "❌")}");

                return new TestResult
                {
                    Name = "视频详情爬取",
                    Success = true,
                    Message = $"详情解析成功，M3U8: {(detail.M3u8Url != null ? "有" : "无")}"
                };
            }
            else
            {
                Console.WriteLine("❌ 详情解析失败");
                return new TestResult
                {
                    Name = "视频详情爬取",
                    Success = false,
                    Message = "解析失败"
                };
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 测试失败：{ex.Message}");
            return new TestResult
            {
                Name = "视频详情爬取",
                Success = false,
                Message = ex.Message
            };
        }
    }

    private static async Task<TestResult> TestDataPersistenceAsync()
    {
        Console.WriteLine("\n【测试 5】数据持久化测试");
        Console.WriteLine(new string('-', 50));

        try
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite("Data Source=test_vodcrawler.db")
                .Options;

            using var context = new ApplicationDbContext(options);
            await context.Database.EnsureCreatedAsync();

            // 创建测试视频
            var testVideo = new Domain.Entities.Video
            {
                Title = "测试视频",
                SourceUrl = "https://example.com/test",
                SourceSite = "Test",
                Category = "测试分类",
                CrawlTime = DateTime.UtcNow,
                IsCached = false
            };

            context.Videos.Add(testVideo);
            await context.SaveChangesAsync();

            // 验证数据
            var savedVideo = await context.Videos.FirstOrDefaultAsync(v => v.Title == "测试视频");
            
            if (savedVideo != null)
            {
                Console.WriteLine($"✅ 数据持久化成功");
                Console.WriteLine($"   - 保存的视频 ID: {savedVideo.Id}");
                Console.WriteLine($"   - 标题：{savedVideo.Title}");
                Console.WriteLine($"   - 来源：{savedVideo.SourceSite}");

                // 清理测试数据
                context.Videos.Remove(savedVideo);
                await context.SaveChangesAsync();
                File.Delete("test_vodcrawler.db");

                return new TestResult
                {
                    Name = "数据持久化",
                    Success = true,
                    Message = "数据保存和读取成功"
                };
            }
            else
            {
                Console.WriteLine("❌ 数据保存失败");
                File.Delete("test_vodcrawler.db");
                return new TestResult
                {
                    Name = "数据持久化",
                    Success = false,
                    Message = "数据未保存"
                };
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 测试失败：{ex.Message}");
            return new TestResult
            {
                Name = "数据持久化",
                Success = false,
                Message = ex.Message
            };
        }
    }

    private static void PrintSummary(List<TestResult> results)
    {
        Console.WriteLine("\n╔════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                      📊 测试总结                       ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════╝\n");

        var successCount = results.Count(r => r.Success);
        var failCount = results.Count(r => !r.Success);

        foreach (var result in results)
        {
            var icon = result.Success ? "✅" : "❌";
            Console.WriteLine($"{icon} {result.Name}: {result.Message}");
        }

        Console.WriteLine($"\n{'═',60}");
        Console.WriteLine($"总测试数：{results.Count}");
        Console.WriteLine($"成功：{successCount} ({successCount * 100 / results.Count}%)");
        Console.WriteLine($"失败：{failCount} ({failCount * 100 / results.Count}%)");
        Console.WriteLine(new string('═', 60));

        if (failCount == 0)
        {
            Console.WriteLine("\n🎉 所有测试通过！系统可以正常运行！\n");
        }
        else
        {
            Console.WriteLine($"\n⚠️ 有 {failCount} 个测试失败，请检查日志\n");
        }
    }

    private static string GetTruncatedText(string? text, int maxLength)
    {
        if (string.IsNullOrEmpty(text)) return "N/A";
        return text.Length > maxLength ? text[..maxLength] + "..." : text;
    }

    private class TestResult
    {
        public string Name { get; set; } = "";
        public bool Success { get; set; }
        public string Message { get; set; } = "";
    }
}

// 入口点
public class Program
{
    public static async Task Main(string[] args)
    {
        try
        {
            await IntegrationTest.RunAllTestsAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ 测试程序异常退出：{ex.Message}");
            Console.WriteLine($"StackTrace: {ex.StackTrace}");
        }
    }
}
