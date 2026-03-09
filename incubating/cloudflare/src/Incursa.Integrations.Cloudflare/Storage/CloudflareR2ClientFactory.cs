using Amazon.Runtime;
using Amazon.S3;
using Incursa.Integrations.Cloudflare.Options;
using Microsoft.Extensions.Options;

namespace Incursa.Integrations.Cloudflare.Storage;

public interface ICloudflareR2ClientFactory
{
    IAmazonS3 CreateClient();
}

public sealed class CloudflareR2ClientFactory : ICloudflareR2ClientFactory
{
    private readonly CloudflareR2Options options;

    public CloudflareR2ClientFactory(IOptions<CloudflareR2Options> options)
    {
        this.options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public IAmazonS3 CreateClient()
    {
        var endpoint = Required(options.Endpoint, nameof(CloudflareR2Options.Endpoint));
        var accessKeyId = Required(options.AccessKeyId, nameof(CloudflareR2Options.AccessKeyId));
        var secretAccessKey = Required(options.SecretAccessKey, nameof(CloudflareR2Options.SecretAccessKey));

        return new AmazonS3Client(
            new BasicAWSCredentials(accessKeyId, secretAccessKey),
            new AmazonS3Config
            {
                ServiceURL = endpoint,
                ForcePathStyle = true,
                AuthenticationRegion = options.Region,
                RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
                ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED,
            });
    }

    private static string Required(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Cloudflare R2 option '{name}' is required.");
        }

        return value.Trim();
    }
}
