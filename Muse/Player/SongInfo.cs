using NAudio.Wave;

namespace Muse.Player
{
    public struct SongInfo(AudioFileReader audioFileReader)
    {
        public string Name => audioFileReader.FileName;
        public double TotalTimeInSeconds => audioFileReader.TotalTime.TotalSeconds;
        public double CurrentTime => audioFileReader.CurrentTime.Seconds;
    }
}
