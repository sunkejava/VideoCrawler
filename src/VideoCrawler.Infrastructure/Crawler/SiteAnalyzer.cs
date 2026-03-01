using System.Text.Json;

namespace VideoCrawler.Infrastructure.Crawler;

/// <summary>
/// 网站结构分析工具 - 用于调试和验证解析器
/// </summary>
public class SiteAnalyzer
{
    public async Task<SiteAnalysisResult> AnalyzeAsync(string url)
    {
        var result = new SiteAnalysisResult
        {
            Url = url,
            AnalyzedAt = DateTime.UtcNow
        };

        try
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            var html = await httpClient.GetStringAsync(url);
            result.HtmlLength = html.Length;

            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);

            // 分析页面结构
            result.Title = doc.DocumentNode.SelectSingleNode("//title")?.InnerText.Trim() ?? "";
            
            // 查找 JSON-LD
            var jsonLdNodes = doc.DocumentNode.SelectNodes("//script[@type='application/ld+json']");
            if (jsonLdNodes != null)
            {
                foreach (var node in jsonLdNodes)
                {
                    var json = node.InnerText.Trim();
                    result.JsonLdData.Add(json);
                    
                    try
                    {
                        using var jsonDoc = JsonDocument.Parse(json);
                        if (jsonDoc.RootElement.TryGetProperty("@type", out var typeProp))
                        {
                            result.JsonLdTypes.Add(typeProp.GetString() ?? "unknown");
                        }
                    }
                    catch { }
                }
            }

            // 查找视频相关元素
            result.VideoListSelectors = new Dictionary<string, int>
            {
                [".vodlist li"] = CountNodes(doc, "//*[contains(@class,'vodlist')]/li"),
                ["li.col"] = CountNodes(doc, "//li[contains(@class,'col')]"),
                [".vod-item"] = CountNodes(doc, "//div[contains(@class,'vod-item')]"),
                ["a.vodlist_thumb"] = CountNodes(doc, "//a[contains(@class,'vodlist_thumb')]")
            };

            // 查找详情相关元素
            result.DetailSelectors = new Dictionary<string, int>
            {
                ["h1.title"] = CountNodes(doc, "//h1[contains(@class,'title')]"),
                ["h1"] = CountNodes(doc, "//h1"),
                [".content"] = CountNodes(doc, "//div[contains(@class,'content')]"),
                [".desc"] = CountNodes(doc, "//div[contains(@class,'desc')]"),
                ["img.lazy"] = CountNodes(doc, "//img[contains(@class,'lazy')]"),
                [".cover img"] = CountNodes(doc, "//div[contains(@class,'cover')]//img")
            };

            // 查找 M3U8
            var m3u8Pattern = @"https?://[^""'\s<>]+\.m3u8[^""'\s<>]*";
            var m3u8Matches = System.Text.RegularExpressions.Regex.Matches(html, m3u8Pattern);
            result.M3u8Urls = m3u8Matches.Select(m => m.Value).ToList();

            // 查找所有链接
            var allLinks = doc.DocumentNode.SelectNodes("//a[@href]") ?? Enumerable.Empty<HtmlNode>();
            result.TotalLinks = allLinks.Count();
            
            // 分析视频链接模式
            var videoLinks = allLinks.Where(a => 
            {
                var href = a.GetAttributeValue("href", "");
                return href.Contains("voddetail") || 
                       href.Contains("video") || 
                       href.Contains("detail") ||
                       href.Contains("/vod/");
            }).ToList();
            
            result.VideoLinks = videoLinks.Take(20).Select(a => new VideoLinkInfo
            {
                Url = a.GetAttributeValue("href", ""),
                Text = a.InnerText.Trim(),
                Title = a.GetAttributeValue("title", "")
            }).ToList();

            // 查找图片
            var allImages = doc.DocumentNode.SelectNodes("//img") ?? Enumerable.Empty<HtmlNode>();
            result.TotalImages = allImages.Count();
            
            result.CoverImages = allImages.Where(img => 
            {
                var src = img.GetAttributeValue("data-original", 
                            img.GetAttributeValue("data-src", 
                            img.GetAttributeValue("src", "")));
                return !string.IsNullOrEmpty(src) && (src.Contains("cover") || src.Contains("thumb") || src.Contains("poster"));
            })
            .Take(20)
            .Select(img => new ImageInfo
            {
                Src = img.GetAttributeValue("data-original", img.GetAttributeValue("src", "")),
                Alt = img.GetAttributeValue("alt", ""),
                Class = img.GetAttributeValue("class", "")
            })
            .ToList();

            // 分析 Meta 信息
            var metaNodes = doc.DocumentNode.SelectNodes("//meta") ?? Enumerable.Empty<HtmlNode>();
            foreach (var meta in metaNodes)
            {
                var name = meta.GetAttributeValue("name", meta.GetAttributeValue("property", ""));
                var content = meta.GetAttributeValue("content", "");
                
                if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(content))
                {
                    result.MetaData[name] = content;
                }
            }

            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Error = ex.Message;
            result.Success = false;
        }

        return result;
    }

    private int CountNodes(HtmlAgilityPack.HtmlDocument doc, string xpath)
    {
        try
        {
            var nodes = doc.DocumentNode.SelectNodes(xpath);
            return nodes?.Count ?? 0;
        }
        catch
        {
            return 0;
        }
    }
}

public class SiteAnalysisResult
{
    public string Url { get; set; } = "";
    public DateTime AnalyzedAt { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
    public int HtmlLength { get; set; }
    public string Title { get; set; } = "";
    
    public List<string> JsonLdData { get; set; } = new();
    public List<string> JsonLdTypes { get; set; } = new();
    
    public Dictionary<string, int> VideoListSelectors { get; set; } = new();
    public Dictionary<string, int> DetailSelectors { get; set; } = new();
    
    public List<string> M3u8Urls { get; set; } = new();
    public int TotalLinks { get; set; }
    public List<VideoLinkInfo> VideoLinks { get; set; } = new();
    
    public int TotalImages { get; set; }
    public List<ImageInfo> CoverImages { get; set; } = new();
    
    public Dictionary<string, string> MetaData { get; set; } = new();
    
    public string RecommendedParser => JsonLdTypes.Any() ? "JsonLdParser" : "AppleCMSParser";
}

public class VideoLinkInfo
{
    public string Url { get; set; } = "";
    public string Text { get; set; } = "";
    public string Title { get; set; } = "";
}

public class ImageInfo
{
    public string Src { get; set; } = "";
    public string Alt { get; set; } = "";
    public string Class { get; set; } = "";
}
