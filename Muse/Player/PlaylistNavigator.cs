using Muse.UI.Bus;

namespace Muse.Player;

public sealed class PlaylistNavigator
{
    private readonly IReadOnlyList<Track> tracks;

    public PlaylistNavigator(IReadOnlyList<Track> tracks)
    {
        this.tracks = tracks;
    }

    public int? GetAdjacentIndex(int currentIndex, int offset, PlayMode playMode)
    {
        if (tracks.Count == 0 || offset == 0)
        {
            return null;
        }

        int candidate = currentIndex + offset;
        if (candidate >= 0 && candidate < tracks.Count)
        {
            return candidate;
        }

        if (playMode != PlayMode.Repeat)
        {
            return null;
        }

        int wrapped = candidate % tracks.Count;
        return wrapped < 0 ? wrapped + tracks.Count : wrapped;
    }
}
