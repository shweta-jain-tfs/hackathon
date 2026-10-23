using Pharos.Common.Web.Utilities;

namespace Pharos.Common.Web.Tests.Utilities;

public class AsyncThrottlerTests
{
    [Fact]
    public async Task PushAsync_ExecutesFirstActionImmediately_AndLastActionOnFlush()
    {
        using var throttler = new AsyncThrottler(TimeSpan.FromHours(1));
        var executed = new List<int>();

        for (var i = 1; i <= 9; i++)
        {
            var value = i;

            await throttler.PushAsync(() =>
            {
                executed.Add(value);
                return Task.CompletedTask;
            });
        }

        executed.Should().Equal(1);

        await throttler.FlushAsync();

        executed.Should().Equal(1, 9);
    }

    [Fact]
    public async Task FlushAsync_WhenNoActionIsPending_DoesNothing()
    {
        using var throttler = new AsyncThrottler(TimeSpan.FromHours(1));
        var executionCount = 0;

        await throttler.PushAsync(() =>
        {
            executionCount++;
            return Task.CompletedTask;
        });

        await throttler.FlushAsync();

        executionCount.Should().Be(1);

        await throttler.FlushAsync();

        executionCount.Should().Be(1);
    }

    [Fact]
    public void Push_WhenNotThrottled_ExecutesActionImmediately()
    {
        using var throttler = new AsyncThrottler(TimeSpan.FromHours(1));
        var executed = false;

        throttler.Push(() => executed = true);

        executed.Should().BeTrue();
    }

    [Fact]
    public async Task PushAsync_WhenActionThrowsImmediately_PropagatesException()
    {
        using var throttler = new AsyncThrottler(TimeSpan.FromHours(1));

        var act = () => throttler.PushAsync(
            () => Task.FromException(new InvalidOperationException()));

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}