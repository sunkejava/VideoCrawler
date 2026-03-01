using System.Text.Json;
using VideoCrawler.Domain.Entities;

namespace VideoCrawler.Infrastructure.Crawler;

/// <summary>
/// 视频网站解析器接口
/// </summary>
public interface IVideoSiteParser
{
    string SiteName { get; }
    string[] SupportedDomains { get; }
    int VideosPerPage { get; } // 每页视频数量
    Task<List<Video>> ParseVideoListAsync(string html, string baseUrl);
    Task<Video?> ParseVideoDetailAsync(string html, string pageUrl);
    string ExtractM3u8Url(string html);
    Task<PagedList<Video>> ParseVideoListWithPagingAsync(string html, string baseUrl, int currentPage, int totalPages);
}

/// <summary>
/// 分页结果
/// </summary>
public class PagedList<T>
{
    public List<T> Items { get; set; } = new();
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int PageSize { get; set; }
    public int TotalCount => Items.Count;
    public bool HasNext => CurrentPage < TotalPages;
    public bool HasPrevious => CurrentPage > 1;
}

/// <summary>
/// 花度影视解析器 - 针对目标网站优化
/// </summary>
public class HuaduZYParser : IVideoSiteParser
{
    public string SiteName => "HuaduZY";
    public string[] SupportedDomains => new[] { "huaduzy.cc", "b.huaduzy.cc" };
    public int VideosPerPage => 40; // 每页 40 条视频

    public async Task<List<Video>> ParseVideoListAsync(string html, string baseUrl)
    {
        var result = await ParseVideoListWithPagingAsync(html, baseUrl, 1, 1);
        return result.Items;
    }

    public async Task<PagedList<Video>> ParseVideoListWithPagingAsync(string html, string baseUrl, int currentPage, int totalPages)
    {
        var videos = new List<Video>();
        
        try
        {
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);

            // 策略 1: 查找视频列表容器 - 针对花都影视优化
            // 花都影视通常使用 .vodlist 和 .col 类
            var videoNodes = doc.DocumentNode.SelectNodes("//ul[contains(@class,'vodlist')]/li") ??
                            doc.DocumentNode.SelectNodes("//div[contains(@class,'vodlist')]/ul/li") ??
                            doc.DocumentNode.SelectNodes("//li[contains(@class,'col')]") ??
                            doc.DocumentNode.SelectNodes("//div[contains(@class,'vod-item')]");

            // 如果没有找到，尝试更通用的选择器
            if (videoNodes == null || !videoNodes.Any())
            {
                // 查找所有带有视频缩略图的链接
                videoNodes = doc.DocumentNode.SelectNodes("//a[contains(@class,'vodlist_thumb')]") ??
                            doc.DocumentNode.SelectNodes("//a[contains(@class,'thumb')]") ??
                            doc.DocumentNode.SelectNodes("//div[contains(@class,'video-box')]");
            }

            if (videoNodes != null)
            {
                Console.WriteLine($"[HuaduZY] 找到 {videoNodes.Count} 个视频节点 (第{currentPage}页)");
                
                foreach (var node in videoNodes)
                {
                    try
                    {
                        var video = ExtractVideoFromNode(node, baseUrl);
                        if (video != null && !string.IsNullOrEmpty(video.Title))
                        {
                            video.SourceSite = SiteName;
                            videos.Add(video);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[HuaduZY] 解析节点失败：{ex.Message}");
                    }
                }
            }

            // 如果还是没有找到，尝试查找所有视频链接
            if (!videos.Any())
            {
                var allLinks = doc.DocumentNode.SelectNodes("//a[@href]") ?? Enumerable.Empty<HtmlNode>();
                
                foreach (var link in allLinks)
                {
                    var href = link.GetAttributeValue("href", "");
                    
                    if (IsVideoDetailUrl(href, baseUrl))
                    {
                        var video = new Video
                        {
                            Title = ExtractTitleFromLink(link),
                            SourceUrl = ResolveUrl(href, baseUrl),
                            SourceSite = SiteName,
                            Category = ExtractCategoryFromUrl(href)
                        };
                        
                        var img = link.SelectSingleNode(".//img");
                        if (img != null)
                        {
                            video.CoverImage = img.GetAttributeValue("data-original",
                                                      img.GetAttributeValue("data-src",
                                                      img.GetAttributeValue("src", "")));
                        }
                        
                        if (!string.IsNullOrEmpty(video.Title) && !videos.Any(v => v.SourceUrl == video.SourceUrl))
                        {
                            videos.Add(video);
                        }
                    }
                }
            }

            Console.WriteLine($"[HuaduZY] 第{currentPage}页解析到 {videos.Count} 个视频");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HuaduZY] 解析视频列表失败：{ex.Message}");
        }

        return new PagedList<Video>
        {
            Items = videos,
            CurrentPage = currentPage,
            TotalPages = totalPages,
            PageSize = VideosPerPage
        };
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

            // 1. 查找标题 - 花都影视通常使用 .content__title 或 h1.title
            var titleNode = doc.DocumentNode.SelectSingleNode("//div[contains(@class,'content__title')]//h1") ??
                           doc.DocumentNode.SelectSingleNode("//h1[contains(@class,'title')]") ??
                           doc.DocumentNode.SelectSingleNode("//h1") ??
                           doc.DocumentNode.SelectSingleNode("//div[contains(@class,'video-title')]");
            
            if (titleNode != null)
            {
                video.Title = CleanText(titleNode.InnerText);
            }

            // 2. 查找描述
            var descNode = doc.DocumentNode.SelectSingleNode("//div[contains(@class,'content__desc')]") ??
                          doc.DocumentNode.SelectSingleNode("//div[contains(@class,'desc')]") ??
                          doc.DocumentNode.SelectSingleNode("//div[contains(@class,'summary')]") ??
                          doc.DocumentNode.SelectSingleNode("//div[contains(@class,'introduction')]") ??
                          doc.DocumentNode.SelectSingleNode("//meta[@name='description']");
            
            if (descNode != null)
            {
                video.Description = CleanText(descNode.GetAttributeValue("content", descNode.InnerText));
            }

            // 3. 查找封面图
            var coverNode = doc.DocumentNode.SelectSingleNode("//img[contains(@class,'lazy')]") ??
                           doc.DocumentNode.SelectSingleNode("//img[contains(@class,'thumb')]") ??
                           doc.DocumentNode.SelectSingleNode("//img[contains(@class,'cover')]") ??
                           doc.DocumentNode.SelectSingleNode("//div[contains(@class,'cover')]//img") ??
                           doc.DocumentNode.SelectSingleNode("//meta[@property='og:image']");
            
            if (coverNode != null)
            {
                video.CoverImage = coverNode.GetAttributeValue("data-original",
                                            coverNode.GetAttributeValue("data-src",
                                            coverNode.GetAttributeValue("src",
                                            coverNode.GetAttributeValue("content", ""))));
            }

            // 4. 查找分类
            var categoryNodes = doc.DocumentNode.SelectNodes("//a[contains(@class,'tag')]") ??
                               doc.DocumentNode.SelectNodes("//a[contains(@class,'type')]") ??
                               doc.DocumentNode.SelectNodes("//span[contains(@class,'category')]");
            
            if (categoryNodes != null && categoryNodes.Any())
            {
                video.Category = string.Join(",", categoryNodes.Take(3).Select(n => CleanText(n.InnerText)));
            }

            // 5. 查找演员
            var actorNode = doc.DocumentNode.SelectSingleNode("//*[contains(text(),'主演') or contains(text(),'演员')]") ??
                           doc.DocumentNode.SelectSingleNode("//div[contains(@class,'actor')]");
            
            if (actorNode != null)
            {
                video.Actor = CleanText(actorNode.InnerText.Replace("主演：", "").Replace("演员：", ""));
            }

            // 6. 查找导演
            var directorNode = doc.DocumentNode.SelectSingleNode("//*[contains(text(),'导演')]") ??
                              doc.DocumentNode.SelectSingleNode("//div[contains(@class,'director')]");
            
            if (directorNode != null)
            {
                video.Director = CleanText(directorNode.InnerText.Replace("导演：", ""));
            }

            // 7. 查找年份
            var yearNode = doc.DocumentNode.SelectSingleNode("//*[contains(text(),'年份') or contains(text(),'年代')]") ??
                          doc.DocumentNode.SelectSingleNode("//span[contains(@class,'year')]");
            
            if (yearNode != null)
            {
                var yearText = yearNode.InnerText.Replace("年份：", "").Replace("年代：", "").Trim();
                if (int.TryParse(yearText, out int year))
                {
                    video.PublishYear = year;
                }
            }

            // 8. 查找地区
            var areaNode = doc.DocumentNode.SelectSingleNode("//*[contains(text(),'地区')]") ??
                          doc.DocumentNode.SelectSingleNode("//span[contains(@class,'area')]");
            
            if (areaNode != null)
            {
                video.Area = CleanText(areaNode.InnerText.Replace("地区：", ""));
            }

            // 9. 查找 M3U8 播放地址 - 花都影视通常在 script 标签中
            var m3u8Url = ExtractM3u8Url(html);
            if (!string.IsNullOrEmpty(m3u8Url))
            {
                video.M3u8Url = m3u8Url;
                video.VideoUrl = m3u8Url;
            }

            // 10. 查找集数
            var episodeNodes = doc.DocumentNode.SelectNodes("//a[contains(@href,'play')]") ??
                              doc.DocumentNode.SelectNodes("//a[contains(@class,'episode')]") ??
                              doc.DocumentNode.SelectNodes("//button[contains(@class,'episode')]") ??
                              doc.DocumentNode.SelectNodes("//div[contains(@class,'playlist')]//a");
            
            if (episodeNodes != null)
            {
                video.EpisodeCount = episodeNodes.Count;
                if (video.EpisodeCount > 0)
                {
                    video.CurrentEpisode = $"1/{video.EpisodeCount}";
                }
            }

            // 11. 查找状态
            var statusNode = doc.DocumentNode.SelectSingleNode("//*[contains(text(),'状态')]") ??
                            doc.DocumentNode.SelectSingleNode("//span[contains(@class,'status')]") ??
                            doc.DocumentNode.SelectSingleNode("//span[contains(@class,'remarks')]");
            
            if (statusNode != null)
            {
                video.Status = CleanText(statusNode.InnerText.Replace("状态：", ""));
            }

            // 12. 查找标签
            var tagNodes = doc.DocumentNode.SelectNodes("//a[contains(@class,'tag')]");
            if (tagNodes != null && tagNodes.Any())
            {
                video.Tags = string.Join(",", tagNodes.Select(t => CleanText(t.InnerText)));
            }

            // 13. 查找评分
            var ratingNode = doc.DocumentNode.SelectSingleNode("//span[contains(@class,'score')]") ??
                            doc.DocumentNode.SelectSingleNode("//em[contains(@class,'score')]") ??
                            doc.DocumentNode.SelectSingleNode("//div[contains(@class,'rating')]");
            
            if (ratingNode != null)
            {
                var ratingText = ratingNode.InnerText.Trim();
                if (decimal.TryParse(ratingText, out decimal rating))
                {
                    video.Rating = rating;
                }
            }

            return string.IsNullOrEmpty(video.Title) ? null : video;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HuaduZY] 解析视频详情失败：{ex.Message}");
            return null;
        }
    }

    public string ExtractM3u8Url(string html)
    {
        try
        {
            // 模式 1: 直接查找.m3u8 链接
            var m3u8Pattern = @"https?://[^""'\s<>]+\.m3u8[^""'\s<>]*";
            var matches = System.Text.RegularExpressions.Regex.Matches(html, m3u8Pattern);
            if (matches.Count > 0)
            {
                return matches[0].Value;
            }

            // 模式 2: 从 player_data 或类似变量中提取
            var playerDataPatterns = new[]
            {
                @"[""']?url[""']?\s*[:=]\s*[""']([^'""]+\.m3u8)[""']",
                @"player_data\s*=\s*{[^}]*[""']?url[""']?\s*:\s*[""']([^'""]+\.m3u8)[""']",
                @"Gplayer\s*\(\s*{[^}]*url\s*:\s*['""]([^'""]+\.m3u8)['""]",
                @"MacPlayerConfig\s*=\s*{[^}]*url\s*:\s*['""]([^'""]+\.m3u8)['""]"
            };

            foreach (var pattern in playerDataPatterns)
            {
                var playerMatches = System.Text.RegularExpressions.Regex.Matches(html, pattern);
                if (playerMatches.Count > 0)
                {
                    return playerMatches[0].Groups[1].Value;
                }
            }

            // 模式 3: 从 script 标签的 JSON 中提取
            var scriptPattern = @"<script[^>]*>([\s\S]*?)</script>";
            var scriptMatches = System.Text.RegularExpressions.Regex.Matches(html, scriptPattern);
            
            foreach (System.Text.RegularExpressions.Match scriptMatch in scriptMatches)
            {
                var scriptContent = scriptMatch.Groups[1].Value;
                if (scriptContent.Contains(".m3u8") || scriptContent.Contains("m3u8"))
                {
                    var jsonMatch = System.Text.RegularExpressions.Regex.Match(scriptContent, m3u8Pattern);
                    if (jsonMatch.Success)
                    {
                        return jsonMatch.Value;
                    }
                }
            }

            return string.Empty;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HuaduZY] 提取 M3U8 失败：{ex.Message}");
            return string.Empty;
        }
    }

    #region Helper Methods

    private Video? ExtractVideoFromNode(HtmlNode node, string baseUrl)
    {
        var video = new Video { SourceSite = SiteName };

        // 查找链接
        var linkNode = node.SelectSingleNode(".//a") ?? node;
        var href = linkNode.GetAttributeValue("href", "");
        video.SourceUrl = ResolveUrl(href, baseUrl);

        // 查找标题
        video.Title = linkNode.GetAttributeValue("title",
                      linkNode.GetAttributeValue("name",
                      CleanText(linkNode.InnerText)));

        // 如果标题为空，尝试从子元素获取
        if (string.IsNullOrEmpty(video.Title))
        {
            var titleNode = node.SelectSingleNode(".//h3") ??
                           node.SelectSingleNode(".//h4") ??
                           node.SelectSingleNode(".//div[contains(@class,'title')]");
            
            if (titleNode != null)
            {
                video.Title = CleanText(titleNode.InnerText);
            }
        }

        // 查找封面图
        var imgNode = node.SelectSingleNode(".//img") ?? 
                      node.SelectSingleNode(".//div[contains(@class,'lazy')]") ??
                      node.SelectSingleNode(".//div[contains(@class,'cover')]");
        
        if (imgNode != null)
        {
            video.CoverImage = imgNode.GetAttributeValue("data-original",
                                        imgNode.GetAttributeValue("data-src",
                                        imgNode.GetAttributeValue("src", "")));
        }

        // 查找分类
        var categoryNode = node.SelectSingleNode(".//span[contains(@class,'category')]") ??
                          node.SelectSingleNode(".//a[contains(@class,'type')]");
        if (categoryNode != null)
        {
            video.Category = CleanText(categoryNode.InnerText);
        }

        // 查找年份
        var yearNode = node.SelectSingleNode(".//span[contains(@class,'year')]") ??
                      node.SelectSingleNode(".//span[contains(@class,'date')]");
        if (yearNode != null && int.TryParse(CleanText(yearNode.InnerText), out int year))
        {
            video.PublishYear = year;
        }

        // 查找评分
        var ratingNode = node.SelectSingleNode(".//span[contains(@class,'score')]") ??
                        node.SelectSingleNode(".//em");
        if (ratingNode != null && decimal.TryParse(CleanText(ratingNode.InnerText), out decimal rating))
        {
            video.Rating = rating;
        }

        return video;
    }

    private bool IsVideoDetailUrl(string url, string baseUrl)
    {
        if (string.IsNullOrEmpty(url) || url.StartsWith("#") || url.StartsWith("javascript:"))
            return false;

        var fullPath = ResolveUrl(url, baseUrl);
        
        // 常见的视频详情页 URL 模式
        var videoPatterns = new[]
        {
            "/voddetail/",
            "/video/",
            "/vod/",
            "/detail/",
            "/show/",
            "detail-",
            "video-",
            "/play/"
        };

        return videoPatterns.Any(p => fullPath.Contains(p, StringComparison.OrdinalIgnoreCase));
    }

    private string ExtractTitleFromLink(HtmlNode link)
    {
        return link.GetAttributeValue("title",
               link.GetAttributeValue("name",
               CleanText(link.InnerText)));
    }

    private string ExtractCategoryFromUrl(string url)
    {
        if (url.Contains("tangxinVlog")) return "tangxinVlog";
        if (url.Contains("movie")) return "电影";
        if (url.Contains("tv") || url.Contains("drama")) return "电视剧";
        if (url.Contains("anime")) return "动漫";
        if (url.Contains("variety")) return "综艺";
        return "";
    }

    private string ResolveUrl(string url, string baseUrl)
    {
        if (string.IsNullOrEmpty(url)) return "";
        
        if (url.StartsWith("http://") || url.StartsWith("https://"))
            return url;
        
        if (url.StartsWith("//"))
            return "https:" + url;
        
        if (url.StartsWith("/"))
        {
            var baseUri = new Uri(baseUrl);
            return $"{baseUri.Scheme}://{baseUri.Host}{url}";
        }
        
        return new Uri(new Uri(baseUrl), url).ToString();
    }

    private string CleanText(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        
        return text.Trim()
            .Replace("\n", " ")
            .Replace("\r", " ")
            .Replace("\t", " ")
            .Replace("  ", " ");
    }

    #endregion
}
