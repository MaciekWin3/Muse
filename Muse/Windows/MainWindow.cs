using Muse.Player.Interfaces;
using Terminal.Gui;

namespace Muse.Windows;

public class MainWindow : Window
{
    private readonly IPlayer player;
    
    public MainWindow(IPlayer player) : base("Muse")
    {
        this.player = player;
        InitControls();
        InitStyles();
    }

    public void InitControls()
    {
        Add(InitLabel());
        Add(InitPlayPauseButton());
    }

    public void InitStyles()
    {
       X = 0;
       Y = 1;
       Width = Dim.Fill();
       Height = Dim.Fill();
    }

    private Label InitLabel()
    {
       return new Label("Hello, World!")
          {
             X = Pos.Center(),
             Y = Pos.Center(),
             Height = 1,
          };
    }
   
    private Button InitPlayPauseButton()
    {
        var button = new Button("||")
        {
            X = Pos.Center(),
            Y = Pos.Bottom(this) - 4,
            Height = 1,
        };
        button.Clicked += () =>
        {
            if (button.Text == "|>")
            {
                button.Text = "||";
                player.Play(@"C:\Users\Maciek\Music\Andzia.mp3");
            }
            else 
            {
                button.Text = "|>";
                player.Pause();
            }
        };

        return button;
    }
}