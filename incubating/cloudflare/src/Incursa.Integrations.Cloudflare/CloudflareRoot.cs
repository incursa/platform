// <copyright file="CloudflareRoot.cs" company="Bravellian">
// CONFIDENTIAL - Copyright (c) Bravellian. All rights reserved.
// See NOTICE.md for full restrictions and usage terms.
// </copyright>

namespace Incursa.Integrations.Cloudflare;

public static class CloudflareRoot
{
    public static string TypeName() => typeof(CloudflareRoot).FullName ?? nameof(CloudflareRoot);
}
