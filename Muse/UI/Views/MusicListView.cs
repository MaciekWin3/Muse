using Muse.Player;
using Muse.UI.Bus;
using Muse.Utils;
using System.Collections.ObjectModel;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Muse.UI.Views;

public sealed class MusicListView : FrameView
{
    private readonly IUiEventBus uiEventBus;
    private readonly ListView listView;
    private readonly IPlayerService playerService;

    public MusicListView(IUiEventBus uiEventBus, IPlayerService playerService, Pos x, Pos y, int bottomReserved)
    {
        this.uiEventBus = uiEventBus;
        this.playerService = playerService;
        X = x;
        Y = y;

        Title = "Music List";
        BorderStyle = LineStyle.Rounded;
        Width = Dim.Fill();
        Height = Dim.Fill() - bottomReserved;

        listView = new ListView()
        {
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            Source = new ListWrapper<string>([])
        };

        Add(listView);

        RegisterBusHandlers();
        RegisterEvents();
    }

    private void RegisterBusHandlers()
    {
        uiEventBus.Subscribe<PlaylistUpdated>(msg =>
        {
            Application.Invoke(() =>
            {
                listView.SetSource(
                    new ObservableCollection<string>(msg.Names)
                );
            });
        });
        uiEventBus.Subscribe<ChangeSongIndexRequested>(msg =>
        {
            var count = listView.Source.Count;

            if (count <= 1 || msg.Offset == 0)
            {
                return;
            }

            int currentIndex = listView.SelectedItem;
            int newIndex = (currentIndex + msg.Offset) % count;

            if (newIndex < 0)
            {
                newIndex += count;
            }

            listView.SelectedItem = newIndex;

            var songName = listView.Source.ToList()[listView.SelectedItem] as string;

            switch (songName)
            {
                case null:
                case "":
                    MessageBox.ErrorQuery("Error", "Unable to obtain song name.", "Ok");
                    break;
                default:
                    playerService.Load(Path.Combine(Globals.MuseDirectory, songName));
                    playerService.Play();
                    break;
            }
        });

    }

    private void RegisterEvents()
    {
        listView.OpenSelectedItem += (sender, e) =>
        {
            if (e.Value is string songName)
            {
                uiEventBus.Publish(new SongSelected(Path.Combine(Globals.MuseDirectory, songName)));
            }
        };
    }
}
