using Incursa.Platform.Health;

namespace Incursa.Platform.Tests;

public sealed class PlatformHealthBucketTagTests
{
    [Theory]
    [InlineData(PlatformHealthBucket.Live, PlatformHealthTags.Live)]
    [InlineData(PlatformHealthBucket.Ready, PlatformHealthTags.Ready)]
    [InlineData(PlatformHealthBucket.Dep, PlatformHealthTags.Dep)]
    public void BucketToTag_ReturnsExpectedTag(PlatformHealthBucket bucket, string expectedTag)
    {
        PlatformHealthReportFormatter.BucketToTag(bucket).ShouldBe(expectedTag);
    }
}
