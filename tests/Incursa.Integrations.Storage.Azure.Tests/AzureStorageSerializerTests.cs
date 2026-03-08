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

namespace Incursa.Integrations.Storage.Azure.Tests;

[Trait("Category", "Unit")]
public sealed class AzureStorageSerializerTests
{
    [Fact]
    public void SerializeToString_AndDeserialize_RoundTripRecord()
    {
        AzureStorageJsonSerializer serializer = new(AzureStorageTestOptions.CreateUnitOptions());
        SamplePayload payload = new("hello", 3);

        string json = serializer.SerializeToString(payload);
        SamplePayload? roundTrip = serializer.Deserialize<SamplePayload>(json);

        roundTrip.ShouldBe(payload);
    }

    [Fact]
    public async Task SerializeAsync_AndDeserializeAsync_RoundTripEnvelope()
    {
        AzureStorageJsonSerializer serializer = new(AzureStorageTestOptions.CreateUnitOptions());
        WorkItem<SampleWorkItem> item = new("work-1", new SampleWorkItem("sync", 2), correlationId: "corr-1");
        var envelope = AzureQueueWorkStore<SampleWorkItem>.AzureQueuedWorkEnvelope.CreateInline(
            item,
            payloadInline: Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(item.Payload)),
            enqueuedUtc: DateTimeOffset.Parse("2026-03-08T12:00:00Z", null, System.Globalization.DateTimeStyles.RoundtripKind));

        using MemoryStream stream = new();
        await serializer.SerializeAsync(stream, envelope, TestContext.Current.CancellationToken).ConfigureAwait(false);
        stream.Position = 0;

        var roundTrip = await serializer
            .DeserializeAsync<AzureQueueWorkStore<SampleWorkItem>.AzureQueuedWorkEnvelope>(stream, TestContext.Current.CancellationToken)
            .ConfigureAwait(false);

        roundTrip.ShouldNotBeNull();
        roundTrip!.Id.ShouldBe("work-1");
        roundTrip.PayloadInline.ShouldNotBeNullOrWhiteSpace();
        roundTrip.CorrelationId.ShouldBe("corr-1");
    }
}
