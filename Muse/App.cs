using Muse.Player.Interfaces;
using Muse.Windows;
using Terminal.Gui;

namespace Muse;

{
    private readonly IPlayer player;

    public App(IPlayer player)
    {
        this.player = player;
    }

    public void Run(string[] args)
    {
        Application.Init();
        Add(InitMenuBar());
        var win = new MainWindow(player);
        Add(win);
        Application.Run(this);
        Application.Shutdown();
    }


    private MenuBar InitMenuBar()
    {
        menuBar = new MenuBar
        {
            Menus =
            [
                new("_File", new MenuItem[]
                {
                }),
                new("_Help", new MenuItem[]
                {
                    new("_About", "", () => ShowAsciiArt())
                })
            ]
        };

        return menuBar;
    }

    {
    }
}