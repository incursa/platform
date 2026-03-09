// Copyright (c) Incursa
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Incursa.Platform.Email;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Incursa.Platform.Email.Postmark;

/// <summary>
/// ASP.NET Core registration helpers for Postmark email services.
/// </summary>
public static class PostmarkServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Postmark sender adapter.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configureOptions">Optional Postmark options configuration.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddIncursaEmailPostmark(
        this IServiceCollection services,
        Action<PostmarkOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }

        services.AddOptions<PostmarkOptions>();
        services.AddOptions<PostmarkValidationOptions>();
        services.AddSingleton<IPostmarkEmailValidator>(sp =>
            new PostmarkEmailValidator(sp.GetRequiredService<IOptions<PostmarkValidationOptions>>().Value));
        services.AddHttpClient<PostmarkOutboundMessageClient>()
            .AddTypedClient((httpClient, sp) =>
                new PostmarkOutboundMessageClient(httpClient, sp.GetRequiredService<IOptions<PostmarkOptions>>().Value));
        services.AddSingleton<IOutboundEmailProbe>(sp =>
            new PostmarkEmailProbe(sp.GetRequiredService<PostmarkOutboundMessageClient>()));
        services.AddHttpClient<PostmarkEmailSender>()
            .AddTypedClient((httpClient, sp) =>
                new PostmarkEmailSender(
                    httpClient,
                    sp.GetRequiredService<IOptions<PostmarkOptions>>().Value,
                    sp.GetRequiredService<IPostmarkEmailValidator>()));
        services.AddTransient<IOutboundEmailSender>(sp => sp.GetRequiredService<PostmarkEmailSender>());
        return services;
    }
}

