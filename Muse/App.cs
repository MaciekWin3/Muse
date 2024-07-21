using Muse.Player.Interfaces;
using Muse.Windows;
using Terminal.Gui;

namespace Muse;

public class App
{
    private readonly IPlayer player;

    public App(IPlayer player)
    {
        this.player = player;
    }

    public void Run(string[] args)
    {
        Application.Init();
        var win = new MainWindow(player);
        Application.Run(win);
        //Colors.Base.Normal = Application.Driver.MakeAttribute(Color.Green, Color.Black);
        //Colors.Menu.Normal = Application.Driver.MakeAttribute(Color.Blue, Color.BrightYellow);
        //Application.Top.Add(CreateMenuBar());
        //Application.Top.Add(InitLabel(win));
        Application.Shutdown();
    }

    //private MenuBar CreateMenuBar()
    //{
    //    return new MenuBar(new MenuBarItem[]
    //    {
    //     new MenuBarItem("File", new MenuItem[]
    //     {
    //        new MenuItem("Quit", "", () => Quit())
    //     }),
    //     new MenuBarItem("Help", new MenuItem[]
    //     {
    //        new MenuItem("About", "", ()
    //           => MessageBox.Query(49, 5, "About", "Written by Maciej Winnik", "Ok"))
    //     })
    //    });
    //}

    private Label InitLabel(View parent)
    {
        var label2 = new Label()
        {
            Text = "Hello, World from Sopot!",
            X = Pos.Center(),
            Y = Pos.Bottom(parent),
            Height = 1
        };

        return label2;
    }

    void Quit()
    {
        Application.RequestStop();
    }
}