using Muse.UI.Bus;
using Xunit;

namespace Muse.Tests;

public sealed class UiEventBusTests
{
    [Fact]
    public void DisposingSubscriptionStopsFutureMessages()
    {
        var bus = new UiEventBus();
        var received = 0;
        using var subscription = bus.Subscribe<string>(_ => received++);

        bus.Publish("first");
        subscription.Dispose();
        bus.Publish("second");

        Assert.Equal(1, received);
    }

    [Fact]
    public void DisposingSubscriptionTwiceIsSafe()
    {
        var bus = new UiEventBus();
        var received = 0;
        var subscription = bus.Subscribe<string>(_ => received++);

        subscription.Dispose();
        subscription.Dispose();
        bus.Publish("message");

        Assert.Equal(0, received);
    }
}
