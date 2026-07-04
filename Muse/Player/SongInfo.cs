namespace Muse.Player
{
    public readonly struct SongInfo(string name, int totalTimeInSeconds, int currentTimeInSeconds)
    {
        public string Name { get; } = name;
        public int TotalTimeInSeconds { get; } = totalTimeInSeconds;
        public int CurrentTime { get; } = currentTimeInSeconds;
    }
}
