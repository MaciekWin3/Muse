using NAudio.Wave;

namespace Muse.Player
{
    public struct SongInfo(AudioFileReader audioFileReader)
    {
        public readonly string Name => Path.GetFileName(audioFileReader.FileName);
        public readonly double TotalTimeInSeconds => audioFileReader.TotalTime.TotalSeconds;
        public readonly double CurrentTime => (audioFileReader.CurrentTime.Hours * 60 * 60) + (audioFileReader.CurrentTime.Minutes * 60) + audioFileReader.CurrentTime.Seconds;
    }
}
