using VideoCrawler.Infrastructure.Crawler;
using FluentAssertions;

namespace VideoCrawler.Infrastructure.Tests;

public class HuaduZYParserTests
{
    private readonly HuaduZYParser _parser;

    public HuaduZYParserTests()
    {
        _parser = new HuaduZYParser();
    }

    [Fact]
    public void Parser_SiteName_ShouldBeHuaduZY()
    {
        _parser.SiteName.Should().Be("HuaduZY");
    }

    [Fact]
    public void Parser_SupportedDomains_ShouldContainHuaduZY()
    {
        _parser.SupportedDomains.Should().Contain("huaduzy.cc");
        _parser.SupportedDomains.Should().Contain("b.huaduzy.cc");
    }

    [Fact]
    public async Task ParseVideoList_EmptyHtml_ShouldReturnEmptyList()
    {
        // Arrange
        var html = string.Empty;
        var baseUrl = "https://b.huaduzy.cc/vodshow/tangxinVlog-----------.html";

        // Act
        var videos = await _parser.ParseVideoListAsync(html, baseUrl);

        // Assert
        videos.Should().BeEmpty();
    }

    [Fact]
    public void ExtractM3u8Url_WithM3u8Link_ShouldExtract()
    {
        // Arrange
        var html = @"<script>var url = 'https://example.com/video.m3u8';</script>";

        // Act
        var result = _parser.ExtractM3u8Url(html);

        // Assert
        result.Should().Contain(".m3u8");
    }

    [Fact]
    public void ExtractM3u8Url_NoM3u8_ShouldReturnEmpty()
    {
        // Arrange
        var html = @"<div>No m3u8 here</div>";

        // Act
        var result = _parser.ExtractM3u8Url(html);

        // Assert
        result.Should().BeEmpty();
    }
}
