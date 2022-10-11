using System.Net.Mime;
using Terminal.Gui;

namespace Muse;

public class App
{
   public App()
   {
      
   }

   public void Run(string[] args)
   {
      Application.Init();
      var label = new Label("Hello, World!")
      {
         X = Pos.Center(),
         Y = Pos.Center(),
         Height = 1
      };
      Application.Top.Add(label);
      Application.Run();
   }
}