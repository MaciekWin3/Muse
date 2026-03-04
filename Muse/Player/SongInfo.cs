using NAudio.Wave;

namespace Muse.Player
{
    public struct SongInfo
    {
        public string Name { get; }
        public int TotalTimeInSeconds { get; }
        public int CurrentTime { get; }

        public SongInfo(WaveStream waveStream)
        {
            // Try to get filename if it's a reader that has it
            string path = "";
            if (waveStream is AudioFileReader afr) path = afr.FileName;
            else if (waveStream is MediaFoundationReader mfr) {
                // MediaFoundationReader doesn't expose filename directly in some versions
                // but we can try to get it via reflection if needed, or just skip
            }
            
            Name = string.IsNullOrEmpty(path) ? "Unknown" : Path.GetFileName(path);
            TotalTimeInSeconds = (int)waveStream.TotalTime.TotalSeconds;
            CurrentTime = (int)waveStream.CurrentTime.TotalSeconds;
        }
        
        public SongInfo(WaveStream waveStream, string fileName)
        {
            Name = Path.GetFileName(fileName);
            TotalTimeInSeconds = (int)waveStream.TotalTime.TotalSeconds;
            CurrentTime = (int)waveStream.CurrentTime.TotalSeconds;
        }
    }
}
