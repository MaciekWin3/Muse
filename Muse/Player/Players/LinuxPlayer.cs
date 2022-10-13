using Muse.Player.Interfaces;

namespace Muse.Player.Players;

public class LinuxPlayer : UnixPlayerBase, IPlayer
{
   protected override string GetBashCommand(string fileName)
   {
      if (Path.GetExtension(fileName).ToLower().Equals(".mp3"))
      {
         return "mpg123 -q";
      }
      
      return "aplay -q";
   }

   public override Task SetVolume(byte percent)
   {
      if (percent > 100)
      {
         throw new ArgumentOutOfRangeException(nameof(percent), "Percent can't exceed 100");
      }

      var tempProcess = StartBashProcess($"osascript -e \"set volume output volume {percent}\"");
      tempProcess.WaitForExit();
      
      return Task.CompletedTask;
   }
}