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
}