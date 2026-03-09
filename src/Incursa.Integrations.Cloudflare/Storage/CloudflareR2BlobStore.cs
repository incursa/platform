using Amazon.S3;
using Amazon.S3.Model;

namespace Incursa.Integrations.Cloudflare.Storage;

public sealed class CloudflareR2BlobStore : ICloudflareR2BlobStore
{
    private readonly IAmazonS3 client;
    private readonly string bucket;

    public CloudflareR2BlobStore(IAmazonS3 client, string bucket)
    {
        this.client = client ?? throw new ArgumentNullException(nameof(client));
        this.bucket = string.IsNullOrWhiteSpace(bucket)
            ? throw new ArgumentException("Bucket is required.", nameof(bucket))
            : bucket.Trim();
    }

    public async ValueTask PutAsync(string key, Stream content, string? contentType = null, CancellationToken cancellationToken = default)
    {
        PutObjectRequest request = new PutObjectRequest
        {
            BucketName = bucket,
            Key = key,
            InputStream = content,
            AutoCloseStream = false,
            AutoResetStreamPosition = false,
            ContentType = contentType,
            UseChunkEncoding = false,
        };

        await client.PutObjectAsync(request, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<Stream?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await client.GetObjectAsync(bucket, key, cancellationToken).ConfigureAwait(false);
            return response.ResponseStream;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound || ex.ErrorCode == "NoSuchKey")
        {
            return null;
        }
    }

    public async ValueTask<Stream?> GetRangeAsync(string key, long startInclusive, long endInclusive, CancellationToken cancellationToken = default)
    {
        if (startInclusive < 0 || endInclusive < startInclusive)
        {
            return null;
        }

        try
        {
            GetObjectRequest request = new GetObjectRequest
            {
                BucketName = bucket,
                Key = key,
                ByteRange = new ByteRange(startInclusive, endInclusive),
            };
            var response = await client.GetObjectAsync(request, cancellationToken).ConfigureAwait(false);
            return response.ResponseStream;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound || ex.ErrorCode == "NoSuchKey")
        {
            return null;
        }
    }

    public async ValueTask<CloudflareBlobObjectInfo?> HeadAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await client.GetObjectMetadataAsync(bucket, key, cancellationToken).ConfigureAwait(false);
            return new CloudflareBlobObjectInfo(key, response.ContentLength, response.Headers.ContentType, response.LastModified);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound || ex.ErrorCode == "NoSuchKey")
        {
            return null;
        }
    }

    public async IAsyncEnumerable<CloudflareBlobObjectInfo> ListAsync(string prefix, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        string? continuationToken = null;
        do
        {
            var response = await client.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = bucket,
                Prefix = prefix ?? string.Empty,
                ContinuationToken = continuationToken,
            }, cancellationToken).ConfigureAwait(false);

            foreach (var item in response.S3Objects)
            {
                if (!string.IsNullOrWhiteSpace(item.Key))
                {
                    yield return new CloudflareBlobObjectInfo(item.Key, item.Size ?? 0, null, item.LastModified);
                }
            }

            continuationToken = response.IsTruncated == true ? response.NextContinuationToken : null;
        }
        while (!string.IsNullOrWhiteSpace(continuationToken));
    }

    public async ValueTask DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        await client.DeleteObjectAsync(bucket, key, cancellationToken).ConfigureAwait(false);
    }
}
