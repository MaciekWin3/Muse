namespace Muse.Player.Interfaces;

public interface IPlayer
{
   event EventHandler PlaybackFinished;
   bool Playing { get; }
   bool Paused { get; }

   Task Play(string fileName);
   Task Pause();
   Task Resume();
   Task Stop();
   Task SetVolume(byte percent);
}