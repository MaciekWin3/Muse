using Muse.Player;
using Muse.UI.Bus;
using Xunit;

namespace Muse.Tests;

public sealed class PlaybackTransitionCoordinatorTests
{
    private readonly PlaybackTransitionCoordinator coordinator = new();

    [Fact]
    public void RepeatOneRepeatsCurrentTrack()
    {
        var result = coordinator.Decide(PlaybackState.Playing, false, 1, 3, PlayMode.RepeatOne, false);

        Assert.Equal(PlaybackTransitionAction.RepeatCurrent, result);
    }

    [Fact]
    public void NonRepeatingPlaylistStopsAtLastTrack()
    {
        var result = coordinator.Decide(PlaybackState.Playing, false, 2, 3, PlayMode.None, false);

        Assert.Equal(PlaybackTransitionAction.StopAtEnd, result);
    }

    [Fact]
    public void ShuffleAdvancesInsteadOfStoppingAtLastTrack()
    {
        var result = coordinator.Decide(PlaybackState.Playing, false, 2, 3, PlayMode.None, true);

        Assert.Equal(PlaybackTransitionAction.Advance, result);
    }

    [Fact]
    public void PausedPlaybackDoesNotTransition()
    {
        var result = coordinator.Decide(PlaybackState.Paused, false, 2, 3, PlayMode.Repeat, false);

        Assert.Equal(PlaybackTransitionAction.None, result);
    }
}
