using Microsoft.EntityFrameworkCore;
using MobilityTicketing.Lecture07.Contracts;
using Xunit;

namespace MobilityTicketing.Lecture07.Tests;

[Trait("Category", "Student")]
public class StudentTests : Fixture
{
    [Fact]
    public async Task RetryAfterCancellationReturnsOriginalReceiptWithNewApplication()
    {
        var first = await App.BookTripAsync("T102", "student-retry");
        await App.CancelTripAsync("T102");
        Clock.Now = Clock.Now.AddMinutes(1);
        var retry = await App.BookTripAsync("T102", "student-retry");
        Assert.Equal(BookingStatus.AlreadyBooked, retry.Status);
        Assert.Equal(first.Receipt, retry.Receipt);
    }

    [Fact]
    public async Task SameRequestForDifferentTripsHasExactlyOneWinner()
    {
        var results = await Task.WhenAll(App.BookTripAsync("T102", "student-race"),
            App.BookTripAsync("T201", "student-race"));
        Assert.Single(results, r => r.Status == BookingStatus.Booked);
        Assert.Single(results, r => r.Status == BookingStatus.RequestConflict);
        await using var db = Db();
        Assert.Equal(1, await db.Bookings.CountAsync(b => b.RequestId == "student-race"));
    }

    [Fact]
    public async Task FailedPublicationCanBeRetriedWithoutPartialSnapshot()
    {
        var failing = new StudentApplication(Connection, Clock, new ApplicationHooks {
            BeforePublishCommit = () => throw new InvalidOperationException("simulated failure")
        });
        await Assert.ThrowsAsync<InvalidOperationException>(() => failing.PublishSnapshotAsync("student-failure", TimeWindow.Morning));
        await Assert.ThrowsAsync<SnapshotNotFoundException>(() => App.ReadSnapshotAsync("student-failure"));
        Assert.Equal(PublishResult.Published, await App.PublishSnapshotAsync("student-failure", TimeWindow.Morning));
    }

    [Fact]
    public async Task EmptyWindowIsRejectedBeforeAnyStateChange()
    {
        var before = await State();
        var window = new TimeWindow(Utc(6, 0), Utc(6, 0));
        await Assert.ThrowsAsync<ArgumentException>(() => App.PublishSnapshotAsync("student-empty", window));
        Assert.Equal(before, await State());
    }
}
