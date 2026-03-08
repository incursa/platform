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

using Microsoft.Extensions.Options;

namespace Incursa.Integrations.Storage.Azure.Tests;

[Trait("Category", "Unit")]
public sealed class AzureStorageOptionsValidatorTests
{
    [Fact]
    public void Validate_FailsWhenConnectionAndServiceUrisAreMissing()
    {
        AzureStorageOptions options = new()
        {
            PayloadContainerName = "payloads",
            WorkPayloadContainerName = "workpayloads",
            WorkQueuePrefix = "work",
            CoordinationContainerName = "coordination",
            CoordinationTableName = "Coordination",
        };

        ValidateOptionsResult result = new AzureStorageOptionsValidator().Validate(Options.DefaultName, options);

        result.Failed.ShouldBeTrue();
        result.Failures.ShouldContain(failure => failure.Contains("Provide either a connection string", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_FailsWhenInlineThresholdIsTooLarge()
    {
        AzureStorageOptions options = AzureStorageTestOptions.CreateUnitOptions();
        options.WorkMessageInlineThresholdBytes = 60 * 1024;

        ValidateOptionsResult result = new AzureStorageOptionsValidator().Validate(Options.DefaultName, options);

        result.Failed.ShouldBeTrue();
        result.Failures.ShouldContain(failure => failure.Contains("WorkMessageInlineThresholdBytes", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_FailsWhenTableOverrideUsesInvalidName()
    {
        AzureStorageOptions options = AzureStorageTestOptions.CreateUnitOptions();
        options.RecordTables[typeof(SampleRecord).Name] = "bad-name";

        ValidateOptionsResult result = new AzureStorageOptionsValidator().Validate(Options.DefaultName, options);

        result.Failed.ShouldBeTrue();
        result.Failures.ShouldContain(failure => failure.Contains("Record-table overrides", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_SucceedsForValidConnectionStringConfiguration()
    {
        AzureStorageOptions options = AzureStorageTestOptions.CreateUnitOptions();

        ValidateOptionsResult result = new AzureStorageOptionsValidator().Validate(Options.DefaultName, options);

        result.Succeeded.ShouldBeTrue();
    }
}
