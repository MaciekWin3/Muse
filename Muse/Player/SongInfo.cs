namespace Muse.Player
{
    public struct SongInfo
    {
        public string Name { get; }
        public int TotalTimeInSeconds { get; }
        public int CurrentTime { get; }

        public SongInfo(string name, int totalTimeInSeconds, int currentTimeInSeconds)
        {
            Name = name;
            TotalTimeInSeconds = totalTimeInSeconds;
            CurrentTime = currentTimeInSeconds;
        }
    }
}
