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
public sealed class AzureQueueWorkEnvelopeTests
{
    [Fact]
    public void CreateInline_PreservesPayloadMetadataAndInlinePayload()
    {
        WorkItem<SampleWorkItem> item = new(
            "work-1",
            new SampleWorkItem("compile", 1),
            correlationId: "corr-1",
            idempotencyKey: "idem-1",
            schemaVersion: "v1",
            metadata: new Dictionary<string, string>(StringComparer.Ordinal) { ["source"] = "tests" });

        var envelope = AzureQueueWorkStore<SampleWorkItem>.AzureQueuedWorkEnvelope.CreateInline(
            item,
            payloadInline: "cGF5bG9hZA==",
            enqueuedUtc: DateTimeOffset.Parse("2026-03-08T12:00:00Z", null, System.Globalization.DateTimeStyles.RoundtripKind));

        envelope.Id.ShouldBe("work-1");
        envelope.PayloadInline.ShouldBe("cGF5bG9hZA==");
        envelope.PayloadReference.ShouldBeNull();
        envelope.Metadata["source"].ShouldBe("tests");
    }

    [Fact]
    public void CreateReference_LeavesInlinePayloadEmpty()
    {
        WorkItem<SampleWorkItem> item = new("work-2", new SampleWorkItem("ship", 2));

        var envelope = AzureQueueWorkStore<SampleWorkItem>.AzureQueuedWorkEnvelope.CreateReference(
            item,
            payloadReference: "workpayloads/ref.json",
            enqueuedUtc: DateTimeOffset.Parse("2026-03-08T12:00:00Z", null, System.Globalization.DateTimeStyles.RoundtripKind));

        envelope.PayloadInline.ShouldBeNull();
        envelope.PayloadReference.ShouldBe("workpayloads/ref.json");
    }
}
