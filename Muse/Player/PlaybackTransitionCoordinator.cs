using Muse.UI.Bus;

namespace Muse.Player;

public enum PlaybackTransitionAction
{
    None,
    RepeatCurrent,
    StopAtEnd,
    Advance
}

public sealed class PlaybackTransitionCoordinator
{
    public PlaybackTransitionAction Decide(
        PlaybackState state,
        bool isTransitioning,
        int currentIndex,
        int playlistCount,
        PlayMode playMode,
        bool isShuffle)
    {
        if (state != PlaybackState.Playing || isTransitioning || playlistCount == 0)
        {
            return PlaybackTransitionAction.None;
        }

        if (playMode == PlayMode.RepeatOne)
        {
            return PlaybackTransitionAction.RepeatCurrent;
        }

        if (playMode == PlayMode.None &&
            !isShuffle &&
            currentIndex >= 0 &&
            currentIndex == playlistCount - 1)
        {
            return PlaybackTransitionAction.StopAtEnd;
        }

        return PlaybackTransitionAction.Advance;
    }
}
