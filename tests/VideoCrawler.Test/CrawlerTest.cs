using System.Text;
using VideoCrawler.Infrastructure.Crawler;

namespace VideoCrawler.Test;

public class CrawlerTest
{
    public static async Task RunTestAsync()
    {
        Console.OutputEncoding = Encoding.UTF8;
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
                
                if (analysisResult.VideoLinks.Any())
                {
                    Console.WriteLine($"\n📺 发现视频链接：{analysisResult.VideoLinks.Count} 个");
                    foreach (var link in analysisResult.VideoLinks.Take(5))
                    {
                        Console.WriteLine($"  - {link.Title}");
                        Console.WriteLine($"    URL: {link.Url}");
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
        Console.WriteLine("📺 测试 2: 爬取视频列表 (第 1 页)");
        
        try
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            var html = await httpClient.GetStringAsync(targetUrl);
            var videos = await parser.ParseVideoListAsync(html, targetUrl);

            Console.WriteLine($"\n✅ 爬取成功！共 {videos.Count} 个视频\n");

            if (videos.Any())
            {
                Console.WriteLine($"{'序号',-6} {'标题',-50} {'分类',-20} {'封面',-8}");
                Console.WriteLine(new string('-', 90));
                
                foreach (var (video, index) in videos.Select((v, i) => (v, i + 1)))
                {
                    var title = video.Title.Length > 48 ? video.Title[..48] + "..." : video.Title;
                    var category = video.Category ?? "N/A";
                    var hasCover = !string.IsNullOrEmpty(video.CoverImage) ? "✅" : "❌";
                    
                    Console.WriteLine($"{index,-6} {title,-50} {category,-20} {hasCover,-8}");
                    Console.WriteLine($"       URL: {video.SourceUrl}");
                }

                Console.WriteLine("\n" + new string('-', 50));
                Console.WriteLine($"📊 统计信息:");
                Console.WriteLine($"   - 总视频数：{videos.Count}");
                Console.WriteLine($"   - 有封面：{videos.Count(v => !string.IsNullOrEmpty(v.CoverImage))}");
                Console.WriteLine($"   - 有分类：{videos.Count(v => !string.IsNullOrEmpty(v.Category))}");
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

        // 测试 3: 爬取视频详情
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
                // 测试前 3 个视频的详情
                var testCount = Math.Min(3, videos.Count);
                
                for (int i = 0; i < testCount; i++)
                {
                    var video = videos[i];
                    Console.WriteLine($"\n【视频 {i + 1}/{testCount}】");
                    Console.WriteLine($"测试视频：{video.Title}");
                    Console.WriteLine($"详情 URL: {video.SourceUrl}\n");

                    try
                    {
                        var detailHtml = await httpClient.GetStringAsync(video.SourceUrl);
                        var detail = await parser.ParseVideoDetailAsync(detailHtml, video.SourceUrl);

                        if (detail != null)
                        {
                            Console.WriteLine("✅ 详情解析成功!\n");
                            Console.WriteLine($"  📺 标题：{detail.Title}");
                            Console.WriteLine($"  📝 描述：{(detail.Description?.Length > 100 ? detail.Description[..100] + "..." : detail.Description ?? "N/A")}");
                            Console.WriteLine($"  🖼️ 封面：{(!string.IsNullOrEmpty(detail.CoverImage) ? "✅ " + detail.CoverImage : "❌")}");
                            Console.WriteLine($"  📁 分类：{detail.Category ?? "N/A"}");
                            Console.WriteLine($"  🎭 演员：{(detail.Actor?.Length > 50 ? detail.Actor[..50] + "..." : detail.Actor ?? "N/A")}");
                            Console.WriteLine($"  🎬 导演：{detail.Director ?? "N/A"}");
                            Console.WriteLine($"  📅 年份：{(detail.PublishYear?.ToString() ?? "N/A")}");
                            Console.WriteLine($"  🌍 地区：{detail.Area ?? "N/A"}");
                            Console.WriteLine($"  📊 集数：{(detail.EpisodeCount?.ToString() ?? "N/A")}");
                            Console.WriteLine($"  ⭐ 评分：{(detail.Rating?.ToString() ?? "N/A")}");
                            Console.WriteLine($"  🎞️ 状态：{detail.Status ?? "N/A"}");
                            Console.WriteLine($"  🔗 M3U8: {(!string.IsNullOrEmpty(detail.M3u8Url) ? "✅ " + detail.M3u8Url : "❌ 未找到")}");
                            
                            if (!string.IsNullOrEmpty(detail.Tags))
                            {
                                Console.WriteLine($"  🏷️ 标签：{detail.Tags}");
                            }
                        }
                        else
                        {
                            Console.WriteLine("❌ 详情解析失败");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ 爬取详情失败：{ex.Message}");
                    }

                    Console.WriteLine(new string('-', 50));
                    
                    // 避免请求过快
                    if (i < testCount - 1)
                    {
                        await Task.Delay(500);
                    }
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
