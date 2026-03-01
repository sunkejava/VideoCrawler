using VideoCrawler.Domain.Entities;
using FluentAssertions;

namespace VideoCrawler.Domain.Tests;

public class VideoTests
{
    [Fact]
    public void Video_Creation_ShouldSetId()
    {
        // Arrange & Act
        var video = new Video();

        // Assert
        video.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Video_UpdateCacheInfo_ShouldSetCacheProperties()
    {
        // Arrange
        var video = new Video
        {
            Title = "Test Video"
        };
        var localPath = "/cache/videos/test.mp4";
        var m3u8Path = "/cache/videos/test.m3u8";

        // Act
        video.UpdateCacheInfo(localPath, m3u8Path);

        // Assert
        video.IsCached.Should().BeTrue();
        video.CachePath.Should().Be(localPath);
        video.VideoUrlLocal.Should().Be(localPath);
        video.M3u8Url.Should().Be(m3u8Path);
        video.LastUpdateTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Video_UpdateCoverImageLocal_ShouldSetCoverPath()
    {
        // Arrange
        var video = new Video();
        var coverPath = "/cache/covers/test.jpg";

        // Act
        video.UpdateCoverImageLocal(coverPath);

        // Assert
        video.CoverImageLocal.Should().Be(coverPath);
    }

    [Fact]
    public void Video_MarkAsCrawled_ShouldSetSourceInfo()
    {
        // Arrange
        var video = new Video();
        var sourceUrl = "https://example.com/video/123";

        // Act
        video.MarkAsCrawled(sourceUrl);

        // Assert
        video.SourceUrl.Should().Be(sourceUrl);
        video.CrawlTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}

public class CrawlerTaskTests
{
    [Fact]
    public void CrawlerTask_Creation_ShouldSetPendingStatus()
    {
        // Arrange & Act
        var task = new CrawlerTask("Test Task", "https://example.com", "Incremental");

        // Assert
        task.Status.Should().Be("Pending");
        task.TaskName.Should().Be("Test Task");
        task.TargetUrl.Should().Be("https://example.com");
    }

    [Fact]
    public void CrawlerTask_Start_ShouldSetRunningStatus()
    {
        // Arrange
        var task = new CrawlerTask("Test", "https://example.com");
        var workerId = "worker-123";

        // Act
        task.Start(workerId);

        // Assert
        task.Status.Should().Be("Running");
        task.AssignedWorker.Should().Be(workerId);
        task.StartTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void CrawlerTask_Complete_ShouldSetCompletedStatus()
    {
        // Arrange
        var task = new CrawlerTask("Test", "https://example.com");
        task.Start();

        // Act
        task.Complete(10, 2);

        // Assert
        task.Status.Should().Be("Completed");
        task.SuccessCount.Should().Be(10);
        task.FailedCount.Should().Be(2);
        task.ProcessedCount.Should().Be(12);
        task.EndTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void CrawlerTask_Fail_ShouldSetFailedStatus()
    {
        // Arrange
        var task = new CrawlerTask("Test", "https://example.com");

        // Act
        task.Fail("Test error message");

        // Assert
        task.Status.Should().Be("Failed");
        task.ErrorMessage.Should().Be("Test error message");
    }

    [Fact]
    public void CrawlerTask_Retry_ShouldIncrementRetryCount()
    {
        // Arrange
        var task = new CrawlerTask("Test", "https://example.com");
        task.Fail("Error");

        // Act
        task.Retry();

        // Assert
        task.Status.Should().Be("Pending");
        task.RetryCount.Should().Be(1);
        task.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void CrawlerTask_Retry_ExceedMaxRetry_ShouldFail()
    {
        // Arrange
        var task = new CrawlerTask("Test", "https://example.com");
        task.MaxRetryCount = 3;
        task.RetryCount = 3;

        // Act
        task.Retry();

        // Assert
        task.Status.Should().Be("Failed");
        task.ErrorMessage.Should().Contain("达到最大重试次数");
    }

    [Fact]
    public void CrawlerTask_Cancel_ShouldSetCancelledStatus()
    {
        // Arrange
        var task = new CrawlerTask("Test", "https://example.com");

        // Act
        task.Cancel();

        // Assert
        task.Status.Should().Be("Cancelled");
        task.EndTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
