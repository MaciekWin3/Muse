using Muse.Player;
using Muse.UI.Bus;
using Xunit;

namespace Muse.Tests;

public sealed class PlaylistNavigatorTests
{
    private static IReadOnlyList<Track> Tracks =>
    [
        new() { Name = "one", Path = "one" },
        new() { Name = "two", Path = "two" },
        new() { Name = "three", Path = "three" }
    ];

    [Fact]
    public void RepeatWrapsAtBothPlaylistBoundaries()
    {
        var navigator = new PlaylistNavigator(Tracks);

        Assert.Equal(2, navigator.GetAdjacentIndex(0, -1, PlayMode.Repeat));
        Assert.Equal(0, navigator.GetAdjacentIndex(2, 1, PlayMode.Repeat));
    }

    [Fact]
    public void NonRepeatingPlaylistStopsAtBoundaries()
    {
        var navigator = new PlaylistNavigator(Tracks);

        Assert.Null(navigator.GetAdjacentIndex(0, -1, PlayMode.None));
        Assert.Null(navigator.GetAdjacentIndex(2, 1, PlayMode.None));
        Assert.Equal(1, navigator.GetAdjacentIndex(0, 1, PlayMode.None));
    }

    [Fact]
    public void EmptyPlaylistHasNoAdjacentTrack()
    {
        var navigator = new PlaylistNavigator([]);

        Assert.Null(navigator.GetAdjacentIndex(0, 1, PlayMode.Repeat));
    }
}
