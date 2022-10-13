using Muse.Player.Interfaces;

namespace Muse.Player.Players;

public class MacPlayer : UnixPlayerBase, IPlayer
{
   protected override string GetBashCommand(string fileName)
   {
      return "afplay";
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