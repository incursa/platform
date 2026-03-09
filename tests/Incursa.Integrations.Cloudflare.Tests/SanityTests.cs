// Copyright (c) Incursa
// See NOTICE.md for full restrictions and usage terms.
// </copyright>

namespace Incursa.Integrations.Cloudflare.Tests;

public sealed class SanityTests
{
    [Fact]
    public void TypeName_ReturnsExpectedValue()
    {
        Assert.Equal("Incursa.Integrations.Cloudflare.CloudflareRoot", global::Incursa.Integrations.Cloudflare.CloudflareRoot.TypeName());
    }
}
