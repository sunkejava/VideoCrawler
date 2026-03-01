using VideoCrawler.Domain.Entities;

namespace VideoCrawler.Infrastructure.Crawler;

/// <summary>
/// 视频网站解析器接口
/// </summary>
public interface IVideoSiteParser
{
    string SiteName { get; }
    string[] SupportedDomains { get; }
    Task<List<Video>> ParseVideoListAsync(string html, string baseUrl);
    Task<Video?> ParseVideoDetailAsync(string html, string pageUrl);
    string ExtractM3u8Url(string html);
}

/// <summary>
/// 花度影视解析器
/// </summary>
public class HuaduZYParser : IVideoSiteParser
{
    public string SiteName => "HuaduZY";
    public string[] SupportedDomains => new[] { "huaduzy.cc", "b.huaduzy.cc" };

    public async Task<List<Video>> ParseVideoListAsync(string html, string baseUrl)
    {
        var videos = new List<Video>();
        
        try
        {
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);

            // 查找视频列表容器 - 常见选择器
            var videoNodes = doc.DocumentNode.SelectNodes("//li[@class='col']//div[contains(@class,'video')]")
                              ?? doc.DocumentNode.SelectNodes("//div[contains(@class,'vodlist_item')]")
                              ?? doc.DocumentNode.SelectNodes("//a[contains(@class,'vodlist_thumb')]");

            if (videoNodes == null)
            {
                // 尝试更通用的选择器
                var allLinks = doc.DocumentNode.SelectNodes("//a[@href]") ?? Enumerable.Empty<HtmlNode>();
                
                foreach (var link in allLinks)
                {
                    var href = link.GetAttributeValue("href", "");
                    if (href.Contains("/voddetail/") || href.Contains("/video/"))
                    {
                        var video = new Video
                        {
                            Title = link.GetAttributeValue("title", link.InnerText.Trim()),
                            SourceUrl = href.StartsWith("http") ? href : new Uri(new Uri(baseUrl), href).ToString(),
                            SourceSite = SiteName,
                            Category = "tangxinVlog"
                        };
                        
                        // 尝试获取封面图
                        var img = link.SelectSingleNode(".//img") ?? link.SelectSingleNode(".//div[contains(@class,'lazy')]");
                        if (img != null)
                        {
                            video.CoverImage = img.GetAttributeValue("data-original", 
                                                       img.GetAttributeValue("src", ""));
                        }
                        
                        if (!string.IsNullOrEmpty(video.Title) && !string.IsNullOrEmpty(video.SourceUrl))
                        {
                            videos.Add(video);
                        }
                    }
                }
                
                return videos;
            }

            foreach (var node in videoNodes)
            {
                try
                {
                    var video = new Video { SourceSite = SiteName };
                    
                    // 获取链接和标题
                    var linkNode = node.SelectSingleNode(".//a") ?? node;
                    video.SourceUrl = linkNode.GetAttributeValue("href", "");
                    if (!string.IsNullOrEmpty(video.SourceUrl) && !video.SourceUrl.StartsWith("http"))
                    {
                        video.SourceUrl = new Uri(new Uri(baseUrl), video.SourceUrl).ToString();
                    }
                    
                    video.Title = linkNode.GetAttributeValue("title", 
                                  linkNode.GetAttributeValue("name", 
                                  linkNode.InnerText.Trim()));
                    
                    // 获取封面图
                    var imgNode = node.SelectSingleNode(".//img") ?? 
                                  node.SelectSingleNode(".//div[contains(@class,'lazy')]") ??
                                  node.SelectSingleNode(".//div[contains(@class,'cover')]");
                    
                    if (imgNode != null)
                    {
                        video.CoverImage = imgNode.GetAttributeValue("data-original",
                                                   imgNode.GetAttributeValue("data-src",
                                                   imgNode.GetAttributeValue("src", "")));
                    }
                    
                    // 获取分类
                    var categoryNode = node.SelectSingleNode(".//span[contains(@class,'category')]") ??
                                       node.SelectSingleNode(".//a[contains(@class,'type')]");
                    if (categoryNode != null)
                    {
                        video.Category = categoryNode.InnerText.Trim();
                    }
                    else
                    {
                        video.Category = "tangxinVlog";
                    }
                    
                    // 获取年份
                    var yearNode = node.SelectSingleNode(".//span[contains(@class,'year')]") ??
                                   node.SelectSingleNode(".//span[contains(@class,'date')]");
                    if (yearNode != null && int.TryParse(yearNode.InnerText.Trim(), out int year))
                    {
                        video.PublishYear = year;
                    }
                    
                    // 获取评分
                    var ratingNode = node.SelectSingleNode(".//span[contains(@class,'score')]") ??
                                     node.SelectSingleNode(".//em");
                    if (ratingNode != null && decimal.TryParse(ratingNode.InnerText.Trim(), out decimal rating))
                    {
                        video.Rating = rating;
                    }
                    
                    if (!string.IsNullOrEmpty(video.Title) && !string.IsNullOrEmpty(video.SourceUrl))
                    {
                        videos.Add(video);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"解析节点失败：{ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"解析视频列表失败：{ex.Message}");
        }

        return videos;
    }

    public async Task<Video?> ParseVideoDetailAsync(string html, string pageUrl)
    {
        try
        {
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);

            var video = new Video
            {
                SourceUrl = pageUrl,
                SourceSite = SiteName,
                CrawlTime = DateTime.UtcNow
            };

            // 获取标题
            var titleNode = doc.DocumentNode.SelectSingleNode("//h1[contains(@class,'title')]") ??
                           doc.DocumentNode.SelectSingleNode("//h1") ??
                           doc.DocumentNode.SelectSingleNode("//div[contains(@class,'title')]");
            
            if (titleNode != null)
            {
                video.Title = titleNode.InnerText.Trim();
            }

            // 获取描述
            var descNode = doc.DocumentNode.SelectSingleNode("//div[contains(@class,'content')]") ??
                          doc.DocumentNode.SelectSingleNode("//div[contains(@class,'desc')]") ??
                          doc.DocumentNode.SelectSingleNode("//div[contains(@class,'summary')]") ??
                          doc.DocumentNode.SelectSingleNode("//meta[@name='description']");
            
            if (descNode != null)
            {
                video.Description = descNode.GetAttributeValue("content", descNode.InnerText.Trim());
            }

            // 获取封面图
            var coverNode = doc.DocumentNode.SelectSingleNode("//img[contains(@class,'lazy')]") ??
                           doc.DocumentNode.SelectSingleNode("//img[@class='thumb']") ??
                           doc.DocumentNode.SelectSingleNode("//div[contains(@class,'cover')]//img");
            
            if (coverNode != null)
            {
                video.CoverImage = coverNode.GetAttributeValue("data-original",
                                            coverNode.GetAttributeValue("data-src",
                                            coverNode.GetAttributeValue("src", "")));
            }

            // 获取分类
            var categoryNode = doc.DocumentNode.SelectSingleNode("//a[contains(@class,'tag')]") ??
                              doc.DocumentNode.SelectSingleNode("//span[contains(@class,'category')]");
            if (categoryNode != null)
            {
                video.Category = categoryNode.InnerText.Trim();
            }

            // 获取演员
            var actorNode = doc.DocumentNode.SelectSingleNode("//div[contains(text(),'主演')]") ??
                           doc.DocumentNode.SelectSingleNode("//div[contains(text(),'演员')]");
            if (actorNode != null)
            {
                video.Actor = actorNode.InnerText.Replace("主演：", "").Replace("演员：", "").Trim();
            }

            // 获取导演
            var directorNode = doc.DocumentNode.SelectSingleNode("//div[contains(text(),'导演')]");
            if (directorNode != null)
            {
                video.Director = directorNode.InnerText.Replace("导演：", "").Trim();
            }

            // 获取年份
            var yearNode = doc.DocumentNode.SelectSingleNode("//div[contains(text(),'年份')]") ??
                          doc.DocumentNode.SelectSingleNode("//div[contains(text(),'年代')]");
            if (yearNode != null && int.TryParse(yearNode.InnerText.Replace("年份：", "").Replace("年代：", "").Trim(), out int year))
            {
                video.PublishYear = year;
            }

            // 获取地区
            var areaNode = doc.DocumentNode.SelectSingleNode("//div[contains(text(),'地区')]");
            if (areaNode != null)
            {
                video.Area = areaNode.InnerText.Replace("地区：", "").Trim();
            }

            // 获取 M3U8 播放地址
            var m3u8Url = ExtractM3u8Url(html);
            if (!string.IsNullOrEmpty(m3u8Url))
            {
                video.M3u8Url = m3u8Url;
                video.VideoUrl = m3u8Url;
            }

            // 获取集数
            var episodeNodes = doc.DocumentNode.SelectNodes("//a[contains(@href,'play')]") ??
                              doc.DocumentNode.SelectNodes("//button[contains(@class,'episode')]");
            if (episodeNodes != null)
            {
                video.EpisodeCount = episodeNodes.Count;
                video.CurrentEpisode = $"1/{video.EpisodeCount}";
            }

            // 获取状态
            var statusNode = doc.DocumentNode.SelectSingleNode("//div[contains(text(),'状态')]") ??
                            doc.DocumentNode.SelectSingleNode("//span[contains(@class,'status')]");
            if (statusNode != null)
            {
                video.Status = statusNode.InnerText.Replace("状态：", "").Trim();
            }

            // 获取标签
            var tags = doc.DocumentNode.SelectNodes("//a[contains(@class,'tag')]");
            if (tags != null && tags.Any())
            {
                video.Tags = string.Join(",", tags.Select(t => t.InnerText.Trim()));
            }

            return string.IsNullOrEmpty(video.Title) ? null : video;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"解析视频详情失败：{ex.Message}");
            return null;
        }
    }

    public string ExtractM3u8Url(string html)
    {
        try
        {
            // 常见的 M3U8 URL 模式
            var patterns = new[]
            {
                @"https?://[^""'\s]+\.m3u8",
                @"url:\s*['""]([^'""]+\.m3u8)['""]",
                @"file:\s*['""]([^'""]+\.m3u8)['""]",
                @"data-url=['""]([^'""]+\.m3u8)['""]"
            };

            foreach (var pattern in patterns)
            {
                var matches = System.Text.RegularExpressions.Regex.Matches(html, pattern);
                if (matches.Count > 0)
                {
                    return matches[0].Groups[1].Success ? matches[0].Groups[1].Value : matches[0].Value;
                }
            }

            // 尝试从 script 标签中查找
            var scriptNodes = html.Split("<script>")
                .Where(s => s.Contains(".m3u8") || s.Contains("m3u8"))
                .ToArray();

            foreach (var script in scriptNodes)
            {
                var match = System.Text.RegularExpressions.Regex.Match(script, @"(https?://[^""'\s]+\.m3u8)");
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }

            return string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}
