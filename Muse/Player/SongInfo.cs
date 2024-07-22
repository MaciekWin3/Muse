using NAudio.Wave;

namespace Muse.Player
{
    public struct SongInfo(AudioFileReader audioFileReader)
    {
        public readonly string Name => Path.GetFileName(audioFileReader.FileName);
        public readonly int TotalTimeInSeconds => (int)audioFileReader.TotalTime.TotalSeconds;
        public readonly int CurrentTime => (audioFileReader.CurrentTime.Hours * 60 * 60) + (audioFileReader.CurrentTime.Minutes * 60) + audioFileReader.CurrentTime.Seconds;
    }
}
