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

using Incursa.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Incursa.Integrations.Storage.Azure.Tests;

[Trait("Category", "Unit")]
public sealed class AzureStorageRegistrationTests
{
    [Fact]
    public void AddAzureStorage_RegistersProviderServices()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddAzureStorage(options =>
        {
            options.CopyFrom(AzureStorageTestOptions.CreateUnitOptions());
        });

        using ServiceProvider provider = services.BuildServiceProvider(validateScopes: true);

        provider.GetRequiredService<IOptions<AzureStorageOptions>>().Value.ConnectionString.ShouldBe("UseDevelopmentStorage=true");
        provider.GetRequiredService<IPayloadStore>().ShouldNotBeNull();
        provider.GetRequiredService<ICoordinationStore>().ShouldNotBeNull();
        provider.GetRequiredService<IRecordStore<SampleRecord>>().ShouldNotBeNull();
        provider.GetRequiredService<ILookupStore<SampleLookup>>().ShouldNotBeNull();
        provider.GetRequiredService<IWorkStore<SampleWorkItem>>().ShouldNotBeNull();
    }

    [Fact]
    public void AddAzureStorage_ThrowsForInvalidOptions()
    {
        ServiceCollection services = new();

        Should.Throw<OptionsValidationException>(() =>
            services.AddAzureStorage(options => options.PayloadContainerName = "bad--container"));
    }
}
