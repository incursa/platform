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

using System.Diagnostics.CodeAnalysis;
using Shouldly;
using Xunit;

namespace Incursa.Platform.Tests.TestUtilities;

[SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Test naming uses underscores for readability.")]
public abstract class SchedulerBehaviorTestsBase : IAsyncLifetime
{
    private readonly ISchedulerBehaviorHarness harness;

    protected SchedulerBehaviorTestsBase(ISchedulerBehaviorHarness harness)
    {
        this.harness = harness ?? throw new ArgumentNullException(nameof(harness));
    }

    protected ISchedulerBehaviorHarness Harness => harness;

    public ValueTask InitializeAsync() => harness.InitializeAsync();

    public async ValueTask DisposeAsync()
    {
        await harness.DisposeAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    /// <summary>When schedule Timer Async And Claim Timers Async With Due Timer Returns Claimed Id, then it behaves as expected.</summary>
    /// <intent>Document expected behavior for schedule Timer Async And Claim Timers Async With Due Timer Returns Claimed Id.</intent>
    /// <scenario>Given schedule Timer Async And Claim Timers Async With Due Timer Returns Claimed Id.</scenario>
    /// <behavior>Then the operation matches the expected outcome.</behavior>
    [Fact]
    public async Task ScheduleTimerAsync_AndClaimTimersAsync_WithDueTimer_ReturnsClaimedId()
    {
        await harness.ResetAsync();

        var timerId = await harness.SchedulerClient.ScheduleTimerAsync(
            "scheduler.topic",
            "payload",
            DateTimeOffset.UtcNow.AddSeconds(-5),
            CancellationToken.None);

        var claimed = await harness.SchedulerClient.ClaimTimersAsync(
            OwnerToken.GenerateNew(),
            leaseSeconds: 30,
            batchSize: 10,
            CancellationToken.None);

        claimed.Count.ShouldBe(1);
        claimed[0].ToString().ShouldBe(timerId, StringCompareShould.IgnoreCase);
    }

    /// <summary>When claim Timers Async Respects Batch Size, then it behaves as expected.</summary>
    /// <intent>Document expected behavior for claim Timers Async Respects Batch Size.</intent>
    /// <scenario>Given claim Timers Async Respects Batch Size.</scenario>
    /// <behavior>Then the operation matches the expected outcome.</behavior>
    [Fact]
    public async Task ClaimTimersAsync_RespectsBatchSize()
    {
        await harness.ResetAsync();

        await harness.SchedulerClient.ScheduleTimerAsync("topic.one", "payload", DateTimeOffset.UtcNow.AddSeconds(-5), CancellationToken.None);
        await harness.SchedulerClient.ScheduleTimerAsync("topic.two", "payload", DateTimeOffset.UtcNow.AddSeconds(-5), CancellationToken.None);

        var claimed = await harness.SchedulerClient.ClaimTimersAsync(
            OwnerToken.GenerateNew(),
            leaseSeconds: 30,
            batchSize: 1,
            CancellationToken.None);

        claimed.Count.ShouldBe(1);
    }

    /// <summary>When ack Timers Async With Valid Owner Makes Timer Unavailable, then it behaves as expected.</summary>
    /// <intent>Document expected behavior for ack Timers Async With Valid Owner Makes Timer Unavailable.</intent>
    /// <scenario>Given ack Timers Async With Valid Owner Makes Timer Unavailable.</scenario>
    /// <behavior>Then the operation matches the expected outcome.</behavior>
    [Fact]
    public async Task AckTimersAsync_WithValidOwner_MakesTimerUnavailable()
    {
        await harness.ResetAsync();

        await harness.SchedulerClient.ScheduleTimerAsync("topic.ack", "payload", DateTimeOffset.UtcNow.AddSeconds(-5), CancellationToken.None);

        var owner = OwnerToken.GenerateNew();
        var claimed = await harness.SchedulerClient.ClaimTimersAsync(owner, leaseSeconds: 30, batchSize: 10, CancellationToken.None);
        claimed.Count.ShouldBe(1);

        await harness.SchedulerClient.AckTimersAsync(owner, claimed, CancellationToken.None);

        var reClaimed = await harness.SchedulerClient.ClaimTimersAsync(
            OwnerToken.GenerateNew(),
            leaseSeconds: 30,
            batchSize: 10,
            CancellationToken.None);

        reClaimed.Count.ShouldBe(0);
    }

    /// <summary>When abandon Timers Async With Valid Owner Returns Timer To Ready, then it behaves as expected.</summary>
    /// <intent>Document expected behavior for abandon Timers Async With Valid Owner Returns Timer To Ready.</intent>
    /// <scenario>Given abandon Timers Async With Valid Owner Returns Timer To Ready.</scenario>
    /// <behavior>Then the operation matches the expected outcome.</behavior>
    [Fact]
    public async Task AbandonTimersAsync_WithValidOwner_ReturnsTimerToReady()
    {
        await harness.ResetAsync();

        await harness.SchedulerClient.ScheduleTimerAsync("topic.abandon", "payload", DateTimeOffset.UtcNow.AddSeconds(-5), CancellationToken.None);

        var owner = OwnerToken.GenerateNew();
        var claimed = await harness.SchedulerClient.ClaimTimersAsync(owner, leaseSeconds: 30, batchSize: 10, CancellationToken.None);
        claimed.Count.ShouldBe(1);

        await harness.SchedulerClient.AbandonTimersAsync(owner, claimed, CancellationToken.None);

        var reClaimed = await harness.SchedulerClient.ClaimTimersAsync(
            OwnerToken.GenerateNew(),
            leaseSeconds: 30,
            batchSize: 10,
            CancellationToken.None);

        reClaimed.Count.ShouldBe(1);
    }

    /// <summary>When reap Expired Timers Async Returns Expired Claims To Ready, then it behaves as expected.</summary>
    /// <intent>Document expected behavior for reap Expired Timers Async Returns Expired Claims To Ready.</intent>
    /// <scenario>Given reap Expired Timers Async Returns Expired Claims To Ready.</scenario>
    /// <behavior>Then the operation matches the expected outcome.</behavior>
    [Fact]
    public async Task ReapExpiredTimersAsync_ReturnsExpiredClaimsToReady()
    {
        await harness.ResetAsync();

        await harness.SchedulerClient.ScheduleTimerAsync("topic.reap", "payload", DateTimeOffset.UtcNow.AddSeconds(-5), CancellationToken.None);

        var owner = OwnerToken.GenerateNew();
        var claimed = await harness.SchedulerClient.ClaimTimersAsync(owner, leaseSeconds: 1, batchSize: 10, CancellationToken.None);
        claimed.Count.ShouldBe(1);

        await Task.Delay(TimeSpan.FromMilliseconds(1200), CancellationToken.None);
        await harness.SchedulerClient.ReapExpiredTimersAsync(CancellationToken.None);

        var reClaimed = await harness.SchedulerClient.ClaimTimersAsync(
            OwnerToken.GenerateNew(),
            leaseSeconds: 30,
            batchSize: 10,
            CancellationToken.None);

        reClaimed.Count.ShouldBe(1);
    }

    /// <summary>When trigger Job Async And Claim Job Runs Async Returns Claimed Run, then it behaves as expected.</summary>
    /// <intent>Document expected behavior for trigger Job Async And Claim Job Runs Async Returns Claimed Run.</intent>
    /// <scenario>Given trigger Job Async And Claim Job Runs Async Returns Claimed Run.</scenario>
    /// <behavior>Then the operation matches the expected outcome.</behavior>
    [Fact]
    public async Task TriggerJobAsync_AndClaimJobRunsAsync_ReturnsClaimedRun()
    {
        await harness.ResetAsync();

        const string jobName = "job-trigger";
        await harness.SchedulerClient.CreateOrUpdateJobAsync(jobName, "job.topic", "*/1 * * * * *", "payload", CancellationToken.None);
        await harness.SchedulerClient.TriggerJobAsync(jobName, CancellationToken.None);

        var claimed = await harness.SchedulerClient.ClaimJobRunsAsync(
            OwnerToken.GenerateNew(),
            leaseSeconds: 30,
            batchSize: 10,
            CancellationToken.None);

        claimed.Count.ShouldBe(1);
    }

    /// <summary>When create Or Update Job Async Existing Job Uses Updated Topic And Payload, then it behaves as expected.</summary>
    /// <intent>Document expected behavior for create Or Update Job Async Existing Job Uses Updated Topic And Payload.</intent>
    /// <scenario>Given create Or Update Job Async Existing Job Uses Updated Topic And Payload.</scenario>
    /// <behavior>Then the operation matches the expected outcome.</behavior>
    [Fact]
    public async Task CreateOrUpdateJobAsync_ExistingJob_UsesUpdatedTopicAndPayload()
    {
        await harness.ResetAsync();

        const string jobName = "job-update";
        await harness.SchedulerClient.CreateOrUpdateJobAsync(jobName, "topic.old", "*/1 * * * * *", "payload-old", CancellationToken.None);
        await harness.SchedulerClient.CreateOrUpdateJobAsync(jobName, "topic.new", "*/1 * * * * *", "payload-new", CancellationToken.None);
        await harness.SchedulerClient.TriggerJobAsync(jobName, CancellationToken.None);

        await using var lease = await AcquireLeaseAsync("scheduler-update-lease");
        var runs = await harness.SchedulerStore.ClaimDueJobRunsAsync(lease, 10, CancellationToken.None);

        runs.Count.ShouldBe(1);
        runs[0].Topic.ShouldBe("topic.new");
        runs[0].Payload.ShouldBe("payload-new");
    }

    /// <summary>When delete Job Async Removes Pending Runs, then it behaves as expected.</summary>
    /// <intent>Document expected behavior for delete Job Async Removes Pending Runs.</intent>
    /// <scenario>Given delete Job Async Removes Pending Runs.</scenario>
    /// <behavior>Then the operation matches the expected outcome.</behavior>
    [Fact]
    public async Task DeleteJobAsync_RemovesPendingRuns()
    {
        await harness.ResetAsync();

        const string jobName = "job-delete";
        await harness.SchedulerClient.CreateOrUpdateJobAsync(jobName, "job.topic", "*/1 * * * * *", "payload", CancellationToken.None);
        await harness.SchedulerClient.TriggerJobAsync(jobName, CancellationToken.None);
        await harness.SchedulerClient.DeleteJobAsync(jobName, CancellationToken.None);

        await using var lease = await AcquireLeaseAsync("scheduler-delete-lease");
        var runs = await harness.SchedulerStore.ClaimDueJobRunsAsync(lease, 10, CancellationToken.None);
        runs.Count.ShouldBe(0);
    }

    /// <summary>When create Job Runs From Due Jobs Async Creates Runs From Due Schedule, then it behaves as expected.</summary>
    /// <intent>Document expected behavior for create Job Runs From Due Jobs Async Creates Runs From Due Schedule.</intent>
    /// <scenario>Given create Job Runs From Due Jobs Async Creates Runs From Due Schedule.</scenario>
    /// <behavior>Then the operation matches the expected outcome.</behavior>
    [Fact]
    public async Task CreateJobRunsFromDueJobsAsync_CreatesRunsFromDueSchedule()
    {
        await harness.ResetAsync();

        await harness.SchedulerClient.CreateOrUpdateJobAsync("job-due", "topic.due", "*/1 * * * * *", "payload", CancellationToken.None);

        await using var lease = await AcquireLeaseAsync("scheduler-due-lease");
        var created = 0;

        var timeoutAt = DateTimeOffset.UtcNow.AddSeconds(4);
        while (DateTimeOffset.UtcNow < timeoutAt && created == 0)
        {
            created = await harness.SchedulerStore.CreateJobRunsFromDueJobsAsync(lease, CancellationToken.None);
            if (created == 0)
            {
                await Task.Delay(250, CancellationToken.None);
            }
        }

        created.ShouldBeGreaterThan(0);
    }

    /// <summary>When get Next Event Time Async With Scheduled Timer Returns Approximate Due Time, then it behaves as expected.</summary>
    /// <intent>Document expected behavior for get Next Event Time Async With Scheduled Timer Returns Approximate Due Time.</intent>
    /// <scenario>Given get Next Event Time Async With Scheduled Timer Returns Approximate Due Time.</scenario>
    /// <behavior>Then the operation matches the expected outcome.</behavior>
    [Fact]
    public async Task GetNextEventTimeAsync_WithScheduledTimer_ReturnsApproximateDueTime()
    {
        await harness.ResetAsync();

        var dueTime = DateTimeOffset.UtcNow.AddSeconds(3);
        await harness.SchedulerClient.ScheduleTimerAsync("topic.next", "payload", dueTime, CancellationToken.None);

        var next = await harness.SchedulerStore.GetNextEventTimeAsync(CancellationToken.None);
        next.ShouldNotBeNull();
        next.Value.UtcDateTime.ShouldBeInRange(
            dueTime.UtcDateTime.AddSeconds(-1),
            dueTime.UtcDateTime.AddSeconds(1));
    }

    private async Task<ISystemLease> AcquireLeaseAsync(string resourceName)
    {
        var lease = await harness.LeaseFactory.AcquireAsync(
            resourceName,
            TimeSpan.FromSeconds(30),
            cancellationToken: CancellationToken.None).ConfigureAwait(false);

        lease.ShouldNotBeNull();
        return lease;
    }
}
