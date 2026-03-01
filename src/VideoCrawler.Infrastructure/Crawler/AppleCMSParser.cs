using System.Text.Json;
using VideoCrawler.Domain.Entities;

namespace VideoCrawler.Infrastructure.Crawler;

/// <summary>
/// 苹果 CMS 解析器 (大多数影视站使用)
/// </summary>
public class AppleCMSParser : IVideoSiteParser
{
    public string SiteName => "AppleCMS";
    public string[] SupportedDomains => new[] { "*" }; // 通用解析器

    public async Task<List<Video>> ParseVideoListAsync(string html, string baseUrl)
    {
        var videos = new List<Video>();
        
        try
        {
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);

            // 方案 1: 查找 JSON-LD 结构化数据
            var jsonLdNodes = doc.DocumentNode.SelectNodes("//script[@type='application/ld+json']");
            if (jsonLdNodes != null)
            {
                foreach (var node in jsonLdNodes)
                {
                    var json = node.InnerText.Trim();
                    var parsedVideos = ParseJsonLd(json, baseUrl);
                    if (parsedVideos.Any())
                    {
                        return parsedVideos;
                    }
                }
            }

            // 方案 2: 查找常见的视频列表结构
            // 苹果 CMS 通常使用 vodlist 相关 class
            var videoItems = doc.DocumentNode.SelectNodes("//*[contains(@class,'vodlist')]/li") ??
                            doc.DocumentNode.SelectNodes("//li[contains(@class,'col')]") ??
                            doc.DocumentNode.SelectNodes("//div[contains(@class,'vod-item')]") ??
                            doc.DocumentNode.SelectNodes("//a[contains(@class,'vodlist_thumb')]");

            if (videoItems != null)
            {
                foreach (var item in videoItems)
                {
                    try
                    {
                        var video = ExtractVideoFromNode(item, baseUrl);
                        if (video != null && !string.IsNullOrEmpty(video.Title))
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

            // 方案 3: 查找所有视频链接
            if (!videos.Any())
            {
                var allLinks = doc.DocumentNode.SelectNodes("//a[@href]") ?? Enumerable.Empty<HtmlNode>();
                
                foreach (var link in allLinks)
                {
                    var href = link.GetAttributeValue("href", "");
                    
                    // 检测是否为视频详情页
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
                        
                        if (!string.IsNullOrEmpty(video.Title))
                        {
                            videos.Add(video);
                        }
                    }
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

            // 1. 查找 JSON-LD
            var jsonLdNodes = doc.DocumentNode.SelectNodes("//script[@type='application/ld+json']");
            if (jsonLdNodes != null)
            {
                foreach (var node in jsonLdNodes)
                {
                    var json = node.InnerText.Trim();
                    var parsedVideo = ParseJsonLdDetail(json, pageUrl);
                    if (parsedVideo != null)
                    {
                        // 合并信息
                        if (!string.IsNullOrEmpty(parsedVideo.Title)) video.Title = parsedVideo.Title;
                        if (!string.IsNullOrEmpty(parsedVideo.Description)) video.Description = parsedVideo.Description;
                        if (!string.IsNullOrEmpty(parsedVideo.CoverImage)) video.CoverImage = parsedVideo.CoverImage;
                        if (parsedVideo.PublishYear.HasValue) video.PublishYear = parsedVideo.PublishYear;
                        if (!string.IsNullOrEmpty(parsedVideo.Actor)) video.Actor = parsedVideo.Actor;
                        if (!string.IsNullOrEmpty(parsedVideo.Director)) video.Director = parsedVideo.Director;
                        break;
                    }
                }
            }

            // 2. 查找标题
            if (string.IsNullOrEmpty(video.Title))
            {
                var titleNode = doc.DocumentNode.SelectSingleNode("//h1[contains(@class,'title')]") ??
                               doc.DocumentNode.SelectSingleNode("//h1") ??
                               doc.DocumentNode.SelectSingleNode("//div[contains(@class,'content')]//h2") ??
                               doc.DocumentNode.SelectSingleNode("//div[contains(@class,'header')]//h1");
                
                if (titleNode != null)
                {
                    video.Title = CleanText(titleNode.InnerText);
                }
            }

            // 3. 查找描述
            if (string.IsNullOrEmpty(video.Description))
            {
                var descNode = doc.DocumentNode.SelectSingleNode("//div[contains(@class,'content')]") ??
                              doc.DocumentNode.SelectSingleNode("//div[contains(@class,'desc')]") ??
                              doc.DocumentNode.SelectSingleNode("//div[contains(@class,'summary')]") ??
                              doc.DocumentNode.SelectSingleNode("//div[contains(@class,'introduction')]") ??
                              doc.DocumentNode.SelectSingleNode("//meta[@name='description']");
                
                if (descNode != null)
                {
                    video.Description = CleanText(descNode.GetAttributeValue("content", descNode.InnerText));
                }
            }

            // 4. 查找封面图
            if (string.IsNullOrEmpty(video.CoverImage))
            {
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
            }

            // 5. 查找分类/类型
            if (string.IsNullOrEmpty(video.Category))
            {
                var categoryNodes = doc.DocumentNode.SelectNodes("//a[contains(@class,'tag')]") ??
                                   doc.DocumentNode.SelectNodes("//a[contains(@class,'type')]") ??
                                   doc.DocumentNode.SelectNodes("//span[contains(@class,'category')]");
                
                if (categoryNodes != null && categoryNodes.Any())
                {
                    video.Category = string.Join(",", categoryNodes.Take(3).Select(n => CleanText(n.InnerText)));
                }
            }

            // 6. 查找演员
            if (string.IsNullOrEmpty(video.Actor))
            {
                var actorNode = doc.DocumentNode.SelectSingleNode("//*[contains(text(),'主演') or contains(text(),'演员')]") ??
                               doc.DocumentNode.SelectSingleNode("//div[contains(@class,'actor')]");
                
                if (actorNode != null)
                {
                    video.Actor = CleanText(actorNode.InnerText.Replace("主演：", "").Replace("演员：", ""));
                }
            }

            // 7. 查找导演
            if (string.IsNullOrEmpty(video.Director))
            {
                var directorNode = doc.DocumentNode.SelectSingleNode("//*[contains(text(),'导演')]") ??
                                  doc.DocumentNode.SelectSingleNode("//div[contains(@class,'director')]");
                
                if (directorNode != null)
                {
                    video.Director = CleanText(directorNode.InnerText.Replace("导演：", ""));
                }
            }

            // 8. 查找年份
            if (!video.PublishYear.HasValue)
            {
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
            }

            // 9. 查找地区
            if (string.IsNullOrEmpty(video.Area))
            {
                var areaNode = doc.DocumentNode.SelectSingleNode("//*[contains(text(),'地区')]") ??
                              doc.DocumentNode.SelectSingleNode("//span[contains(@class,'area')]");
                
                if (areaNode != null)
                {
                    video.Area = CleanText(areaNode.InnerText.Replace("地区：", ""));
                }
            }

            // 10. 查找 M3U8 播放地址
            var m3u8Url = ExtractM3u8Url(html);
            if (!string.IsNullOrEmpty(m3u8Url))
            {
                video.M3u8Url = m3u8Url;
                video.VideoUrl = m3u8Url;
            }

            // 11. 查找集数
            var episodeNodes = doc.DocumentNode.SelectNodes("//a[contains(@href,'play')]") ??
                              doc.DocumentNode.SelectNodes("//a[contains(@class,'episode')]") ??
                              doc.DocumentNode.SelectNodes("//button[contains(@class,'episode')]");
            
            if (episodeNodes != null)
            {
                video.EpisodeCount = episodeNodes.Count;
                if (video.EpisodeCount > 0)
                {
                    video.CurrentEpisode = $"1/{video.EpisodeCount}";
                }
            }

            // 12. 查找状态
            if (string.IsNullOrEmpty(video.Status))
            {
                var statusNode = doc.DocumentNode.SelectSingleNode("//*[contains(text(),'状态')]") ??
                                doc.DocumentNode.SelectSingleNode("//span[contains(@class,'status')]") ??
                                doc.DocumentNode.SelectSingleNode("//span[contains(@class,'remarks')]");
                
                if (statusNode != null)
                {
                    video.Status = CleanText(statusNode.InnerText.Replace("状态：", ""));
                }
            }

            // 13. 查找标签
            var tagNodes = doc.DocumentNode.SelectNodes("//a[contains(@class,'tag')]");
            if (tagNodes != null && tagNodes.Any())
            {
                video.Tags = string.Join(",", tagNodes.Select(t => CleanText(t.InnerText)));
            }

            // 14. 查找评分
            if (!video.Rating.HasValue)
            {
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
            // 模式 1: 直接查找.m3u8 链接
            var m3u8Pattern = @"https?://[^""'\s<>]+\.m3u8[^""'\s<>]*";
            var matches = System.Text.RegularExpressions.Regex.Matches(html, m3u8Pattern);
            if (matches.Count > 0)
            {
                return matches[0].Value;
            }

            // 模式 2: 从 player_data 或类似变量中提取
            var playerDataPattern = @"[""']?url[""']?\s*[:=]\s*[""']([^'""]+\.m3u8)[""']";
            var playerMatches = System.Text.RegularExpressions.Regex.Matches(html, playerDataPattern);
            if (playerMatches.Count > 0)
            {
                return playerMatches[0].Groups[1].Value;
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
        catch
        {
            return string.Empty;
        }
    }

    #region Helper Methods

    private List<Video> ParseJsonLd(string json, string baseUrl)
    {
        var videos = new List<Video>();
        
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // 处理 VideoObject 类型
            if (root.TryGetProperty("@type", out var typeProp) && 
                (typeProp.GetString()?.Contains("VideoObject") == true || 
                 typeProp.GetString()?.Contains("Movie") == true ||
                 typeProp.GetString()?.Contains("TVSeries") == true))
            {
                var video = new Video { SourceSite = SiteName };

                if (root.TryGetProperty("name", out var nameProp))
                    video.Title = nameProp.GetString() ?? "";

                if (root.TryGetProperty("description", out var descProp))
                    video.Description = descProp.GetString();

                if (root.TryGetProperty("thumbnailUrl", out var thumbProp))
                    video.CoverImage = thumbProp.GetString();

                if (root.TryGetProperty("contentUrl", out var contentProp))
                    video.VideoUrl = contentProp.GetString();

                if (root.TryGetProperty("uploadDate", out var dateProp))
                {
                    if (DateTime.TryParse(dateProp.GetString(), out var date))
                    {
                        video.PublishYear = date.Year;
                    }
                }

                if (root.TryGetProperty("actor", out var actorProp))
                {
                    video.Actor = actorProp.ToString();
                }

                if (root.TryGetProperty("director", out var directorProp))
                {
                    video.Director = directorProp.GetString();
                }

                if (!string.IsNullOrEmpty(video.Title))
                {
                    videos.Add(video);
                }
            }

            // 处理 ItemList 类型（视频列表）
            if (root.TryGetProperty("@type", out var listType) && 
                listType.GetString() == "ItemList" &&
                root.TryGetProperty("itemListElement", out var itemsProp))
            {
                foreach (var item in itemsProp.EnumerateArray())
                {
                    if (item.TryGetProperty("url", out var urlProp))
                    {
                        var video = new Video
                        {
                            SourceUrl = urlProp.GetString() ?? "",
                            SourceSite = SiteName
                        };

                        if (item.TryGetProperty("name", out var nameProp))
                            video.Title = nameProp.GetString() ?? "";

                        if (!string.IsNullOrEmpty(video.Title))
                        {
                            videos.Add(video);
                        }
                    }
                }
            }
        }
        catch (JsonException)
        {
            // JSON 解析失败，忽略
        }

        return videos;
    }

    private Video? ParseJsonLdDetail(string json, string pageUrl)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("@type", out var typeProp) && 
                (typeProp.GetString()?.Contains("VideoObject") == true || 
                 typeProp.GetString()?.Contains("Movie") == true))
            {
                var video = new Video
                {
                    SourceUrl = pageUrl,
                    SourceSite = SiteName
                };

                if (root.TryGetProperty("name", out var name)) video.Title = name.GetString() ?? "";
                if (root.TryGetProperty("description", out var desc)) video.Description = desc.GetString();
                if (root.TryGetProperty("thumbnailUrl", out var thumb)) video.CoverImage = thumb.GetString();
                if (root.TryGetProperty("contentUrl", out var content)) video.VideoUrl = content.GetString();
                if (root.TryGetProperty("actor", out var actor)) video.Actor = actor.ToString();
                if (root.TryGetProperty("director", out var director)) video.Director = director.GetString();

                return video;
            }
        }
        catch (JsonException)
        {
            // 忽略
        }

        return null;
    }

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

        // 查找封面图
        var imgNode = node.SelectSingleNode(".//img") ?? 
                      node.SelectSingleNode(".//div[contains(@class,'lazy')]");
        
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
        // 从 URL 中提取分类信息
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
