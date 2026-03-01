using System.Text;
using VideoCrawler.Infrastructure.Crawler;

namespace VideoCrawler.Test;

public class CrawlerTest
{
    public static async Task RunTestAsync()
    {
        Console.WriteLine("========================================");
        Console.WriteLine("🎬 VideoCrawler 爬取功能测试");
        Console.WriteLine("========================================\n");

        var targetUrl = "https://b.huaduzy.cc/vodshow/tangxinVlog-----------.html";
        var parser = new HuaduZYParser();
        var analyzer = new SiteAnalyzer();
        
        Console.WriteLine($"📋 每页视频数：{parser.VideosPerPage} 条\n");

        // 测试 1: 网站结构分析
        Console.WriteLine("📊 测试 1: 网站结构分析");
        Console.WriteLine($"目标 URL: {targetUrl}\n");
        
        try
        {
            var analysisResult = await analyzer.AnalyzeAsync(targetUrl);
            
            if (analysisResult.Success)
            {
                Console.WriteLine($"✅ 网站标题：{analysisResult.Title}");
                Console.WriteLine($"📄 HTML 长度：{analysisResult.HtmlLength} 字符");
                Console.WriteLine($"🔗 总链接数：{analysisResult.TotalLinks}");
                Console.WriteLine($"🖼️ 总图片数：{analysisResult.TotalImages}");
                Console.WriteLine($"📋 JSON-LD 类型：{string.Join(", ", analysisResult.JsonLdTypes)}");
                Console.WriteLine($"🎯 推荐解析器：{analysisResult.RecommendedParser}");
                
                Console.WriteLine("\n📋 视频列表选择器匹配数:");
                foreach (var selector in analysisResult.VideoListSelectors)
                {
                    Console.WriteLine($"  - {selector.Key}: {selector.Value}");
                }
                
                Console.WriteLine("\n📄 详情页选择器匹配数:");
                foreach (var selector in analysisResult.DetailSelectors)
                {
                    Console.WriteLine($"  - {selector.Key}: {selector.Value}");
                }
                
                if (analysisResult.M3u8Urls.Any())
                {
                    Console.WriteLine($"\n🎬 发现 M3U8 地址：{analysisResult.M3u8Urls.Count} 个");
                    foreach (var url in analysisResult.M3u8Urls.Take(3))
                    {
                        Console.WriteLine($"  - {url}");
                    }
                }
                
                if (analysisResult.VideoLinks.Any())
                {
                    Console.WriteLine($"\n📺 发现视频链接：{analysisResult.VideoLinks.Count} 个");
                    foreach (var link in analysisResult.VideoLinks.Take(5))
                    {
                        Console.WriteLine($"  - {link.Title} -> {link.Url}");
                    }
                }
            }
            else
            {
                Console.WriteLine($"❌ 分析失败：{analysisResult.Error}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 测试 1 失败：{ex.Message}");
        }

        Console.WriteLine("\n" + new string('-', 50) + "\n");

        // 测试 2: 爬取视频列表
        Console.WriteLine("📺 测试 2: 爬取视频列表");
        
        try
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            var html = await httpClient.GetStringAsync(targetUrl);
            var videos = await parser.ParseVideoListAsync(html, targetUrl);

            Console.WriteLine($"✅ 爬取成功！共 {videos.Count} 个视频\n");

            if (videos.Any())
            {
                Console.WriteLine("前 10 个视频:");
                foreach (var (video, index) in videos.Take(10).Select((v, i) => (v, i + 1)))
                {
                    Console.WriteLine($"\n  [{index}] {video.Title}");
                    Console.WriteLine($"      URL: {video.SourceUrl}");
                    Console.WriteLine($"      封面：{(!string.IsNullOrEmpty(video.CoverImage) ? "✅" : "❌")}");
                    Console.WriteLine($"      分类：{video.Category ?? "N/A"}");
                    Console.WriteLine($"      年份：{(video.PublishYear?.ToString() ?? "N/A")}");
                    Console.WriteLine($"      评分：{(video.Rating?.ToString() ?? "N/A")}");
                }
            }
            else
            {
                Console.WriteLine("⚠️ 未解析到视频，可能需要调整解析器");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 测试 2 失败：{ex.Message}");
            Console.WriteLine($"   StackTrace: {ex.StackTrace}");
        }

        Console.WriteLine("\n" + new string('-', 50) + "\n");

        // 测试 3: 爬取视频详情（如果有视频链接）
        Console.WriteLine("🎬 测试 3: 爬取视频详情");
        
        try
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            // 先获取列表
            var listHtml = await httpClient.GetStringAsync(targetUrl);
            var videos = await parser.ParseVideoListAsync(listHtml, targetUrl);

            if (videos.Any())
            {
                var firstVideo = videos.First();
                Console.WriteLine($"测试视频：{firstVideo.Title}");
                Console.WriteLine($"详情 URL: {firstVideo.SourceUrl}\n");

                var detailHtml = await httpClient.GetStringAsync(firstVideo.SourceUrl);
                var detail = await parser.ParseVideoDetailAsync(detailHtml, firstVideo.SourceUrl);

                if (detail != null)
                {
                    Console.WriteLine("✅ 详情解析成功!\n");
                    Console.WriteLine($"  标题：{detail.Title}");
                    Console.WriteLine($"  描述：{(detail.Description?.Length > 50 ? detail.Description[..50] + "..." : detail.Description ?? "N/A")}");
                    Console.WriteLine($"  封面：{(!string.IsNullOrEmpty(detail.CoverImage) ? "✅" : "❌")}");
                    Console.WriteLine($"  分类：{detail.Category ?? "N/A"}");
                    Console.WriteLine($"  演员：{(detail.Actor?.Length > 30 ? detail.Actor[..30] + "..." : detail.Actor ?? "N/A")}");
                    Console.WriteLine($"  导演：{detail.Director ?? "N/A"}");
                    Console.WriteLine($"  年份：{(detail.PublishYear?.ToString() ?? "N/A")}");
                    Console.WriteLine($"  地区：{detail.Area ?? "N/A"}");
                    Console.WriteLine($"  集数：{(detail.EpisodeCount?.ToString() ?? "N/A")}");
                    Console.WriteLine($"  评分：{(detail.Rating?.ToString() ?? "N/A")}");
                    Console.WriteLine($"  M3U8: {(!string.IsNullOrEmpty(detail.M3u8Url) ? "✅" : "❌")}");
                    
                    if (!string.IsNullOrEmpty(detail.M3u8Url))
                    {
                        Console.WriteLine($"      {detail.M3u8Url}");
                    }
                }
                else
                {
                    Console.WriteLine("❌ 详情解析失败");
                }
            }
            else
            {
                Console.WriteLine("⚠️ 没有视频可用于详情测试");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 测试 3 失败：{ex.Message}");
        }

        Console.WriteLine("\n========================================");
        Console.WriteLine("✅ 测试完成!");
        Console.WriteLine("========================================");
    }
}

// 入口点
public class Program
{
    public static async Task Main(string[] args)
    {
        await CrawlerTest.RunTestAsync();
    }
}
