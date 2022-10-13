using System.Net.Mime;
using Muse.Windows;
using Terminal.Gui;

namespace Muse;

public class App
{
   public void Run(string[] args)
   {
      Application.Init();
      var win = new MainWindow(null);
      Colors.Base.Normal = Application.Driver.MakeAttribute(Color.Green, Color.Black);
      Colors.Menu.Normal = Application.Driver.MakeAttribute(Color.Blue, Color.BrightYellow);
      Application.Top.Add(CreateMenuBar());
      Application.Top.Add(win);
      Application.Run();
   }

   private MenuBar CreateMenuBar()
   {
      return new MenuBar(new MenuBarItem[]
      {
         new MenuBarItem("_File", new MenuItem[]
         {
            new MenuItem("_Quit", "", () => Quit())
         }),
         new MenuBarItem("_Help", new MenuItem[]
         {
            new MenuItem("_About", "", ()
               => MessageBox.Query(49, 5, "About", "Written by Maciej Winnik", "Ok"))
         })
      });
   }

   private Label InitLabel()
   {
      var label2 = new Label("Hello, World from Sopot!")
      {
         X = Pos.Center(),
         Y = Pos.Center() - 1,
         Height = 1
      };

      return label2;
   }
   
   void Quit()
   {
      Application.RequestStop();
   }
}