// <copyright file="ProofGeneratedTypesTests.cs" company="Incursa">
// CONFIDENTIAL - Copyright (c) Incursa. All rights reserved.
// See NOTICE.md for full restrictions and usage terms.
// </copyright>

namespace Incursa.Integrations.ElectronicNotary.Tests;

using System;
using System.Globalization;
using System.Text.Json;
using FluentAssertions;
using Incursa.Integrations.ElectronicNotary.Proof.Contracts;
using Incursa.Integrations.ElectronicNotary.Proof.Types;

[TestClass]
public sealed class ProofGeneratedTypesTests
{
    [TestMethod]
    public void ProofTransactionIdRejectsInvalidValues()
    {
        Action parse = () => ProofTransactionId.Parse("invalid_123");
        parse.Should().Throw<ArgumentOutOfRangeException>();
        Assert.IsFalse(ProofTransactionId.TryParse("invalid_123", out _));
    }

    [TestMethod]
    public void ProofDocumentRequirementJsonRoundTripWorks()
    {
        var json = JsonSerializer.Serialize(ProofDocumentRequirement.Notarization);
        Assert.AreEqual("\"notarization\"", json);

        var parsed = JsonSerializer.Deserialize<ProofDocumentRequirement>(json);
        Assert.AreEqual(ProofDocumentRequirement.Notarization, parsed);
    }

    [TestMethod]
    public void ProofTransactionDetailedStatusJsonRoundTripWorks()
    {
        var json = JsonSerializer.Serialize(ProofTransactionDetailedStatus.MeetingInProgress);
        Assert.AreEqual("\"meeting_in_progress\"", json);

        var parsed = JsonSerializer.Deserialize<ProofTransactionDetailedStatus>(json);
        Assert.AreEqual(ProofTransactionDetailedStatus.MeetingInProgress, parsed);
    }

    [TestMethod]
    public void WebhookEventUsesTypedTransactionIdInJsonRoundTrip()
    {
        var evt = new TransactionCompletedEvent
        {
            TransactionId = ProofTransactionId.Parse("ot_wd3y67d"),
            DateOccurred = "2026-02-06T00:00:00Z",
        };

        var json = JsonSerializer.Serialize(evt);
        json.Should().Contain("\"transaction_id\":\"ot_wd3y67d\"");

        TransactionCompletedEvent? parsed = JsonSerializer.Deserialize<TransactionCompletedEvent>(json);
        parsed.Should().NotBeNull();
        parsed!.TransactionId.Should().Be(ProofTransactionId.Parse("ot_wd3y67d"));
        parsed.DateOccurred.Should().Be("2026-02-06T00:00:00Z");
    }

    [TestMethod]
    public void CreateTransactionRequestCanBeConstructedFromGeneratedDtos()
    {
        var signers = new[]
        {
            SignerInput.Create("signer@example.com", null, "external-123"),
        };
        var documents = new[]
        {
            DocumentInput.Create("https://example.test/documents/1.pdf", ProofDocumentRequirement.Notarization),
        };

        var request = CreateTransactionRequest.Create(
            signers,
            documents,
            true);

        request.Signers.Length.Should().Be(1);
        request.Documents.Should().NotBeNull();
        request.Documents![0].Requirement.Should().Be(ProofDocumentRequirement.Notarization);
        request.Draft.Should().BeTrue();
    }

    [TestMethod]
    public void TransactionCanDeserializeFromProofJson()
    {
        var json = """
                   {
                     "id": "ot_wd3y67d",
                     "detailed_status": "sent_to_signer",
                     "status": "active",
                     "created_at": "2026-01-01T00:00:00Z",
                     "updated_at": "2026-01-02T00:00:00Z"
                   }
                   """;

        var transaction = JsonSerializer.Deserialize<Transaction>(json);
        transaction.Should().NotBeNull();
        transaction!.Id.Should().Be(ProofTransactionId.Parse("ot_wd3y67d"));
        transaction.DetailedStatus.Should().Be(ProofTransactionDetailedStatus.SentToSigner);
        transaction.Status.Should().Be("active");
        transaction.CreatedAt.Should().Be(DateTimeOffset.Parse("2026-01-01T00:00:00Z", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));
        transaction.UpdatedAt.Should().Be(DateTimeOffset.Parse("2026-01-02T00:00:00Z", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));
    }
}
