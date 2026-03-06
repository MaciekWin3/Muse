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
    private List<FileInfo> songs = [];

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
                songs = [.. msg.Songs];
                listView.SetSource(
                    new ObservableCollection<string>(songs.Select(s => s.Name))
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

            if (listView.SelectedItem is not int currentIndex)
            {
                return;
            }

            int newIndex = (currentIndex + msg.Offset) % count;

            if (newIndex < 0)
            {
                newIndex += count;
            }

            listView.SelectedItem = newIndex;

            if (newIndex < 0 || newIndex >= songs.Count)
            {
                MessageBox.ErrorQuery(null, "Error", "Unable to obtain song info.", "Ok");
                return;
            }

            var song = songs[newIndex];

            var loadResult = playerService.Load(song.FullName);
            if (!loadResult.Success)
            {
                MessageBox.ErrorQuery(null, "Error", $"Cannot load file: {loadResult.Error}", "Ok");
                return;
            }
            playerService.Play();
        });

    }

    private void RegisterEvents()
    {
        listView.Activated += (sender, e) =>
        {
            var index = listView.SelectedItem;
            if (index is int i && i >= 0 && i < songs.Count)
            {
                uiEventBus.Publish(new SongSelected(songs[i].FullName));
            }
        };
    }
}
