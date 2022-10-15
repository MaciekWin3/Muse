using Terminal.Gui;

namespace Muse.Windows;

public class MainWindow : Window
{
    private readonly View parent;

    public MainWindow(View parent) : base("Muse")
    {
        this.parent = parent;
        InitControls();
    }

    public void InitControls()
    {
        Add(InitLabel());
        Add(InitPlayButton());
        Add(InitPauseButton());
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

    private Button InitPlayButton()
    {
        return new Button("|>")
        {
            X = Pos.Center(),
            Y = 5,
            Height = 1
        };
    } 
    
    private Button InitPauseButton()
    {
        return new Button("||")
        {
            X = Pos.Center() + 10,
            Y = 5,
            Height = 1
        };
    }
}