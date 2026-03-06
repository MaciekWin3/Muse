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

    private PlayMode _playMode = PlayMode.None;

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
        uiEventBus.Subscribe<PlayModeChanged>(msg =>
        {
            _playMode = msg.NewMode;
        });

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

            int currentIndex = listView.SelectedItem;
            int newIndex = currentIndex + msg.Offset;

            if (newIndex < 0)
            {
                if (_playMode == PlayMode.Repeat)
                {
                    newIndex = count - 1;
                }
                else
                {
                    return; // Don't wrap
                }
            }
            else if (newIndex >= count)
            {
                if (_playMode == PlayMode.Repeat)
                {
                    newIndex = 0;
                }
                else
                {
                    return; // Don't wrap
                }
            }

            listView.SelectedItem = newIndex;

            if (newIndex < 0 || newIndex >= songs.Count)
            {
                MessageBox.ErrorQuery("Error", "Unable to obtain song info.", "Ok");
                return;
            }

            var song = songs[newIndex];

            var loadResult = playerService.Load(song.FullName);
            if (!loadResult.Success)
            {
                MessageBox.ErrorQuery("Error", $"Cannot load file: {loadResult.Error}", "Ok");
                return;
            }
            playerService.Play();
        });

        uiEventBus.Subscribe<DeleteSongRequested>(_ =>
        {
            int selectedIndex = listView.SelectedItem;
            if (selectedIndex >= 0 && selectedIndex < songs.Count)
            {
                var song = songs[selectedIndex];
                var result = MessageBox.Query("Delete", $"Are you sure you want to delete {song.Name}?", "Yes", "No");
                if (result == 0) // Yes
                {
                    try
                    {
                        if (File.Exists(song.FullName))
                        {
                            // If it's currently playing, we should probably stop it
                            playerService.Stop();
                            File.Delete(song.FullName);
                            uiEventBus.Publish(new RefreshPlaylistsRequested());
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.ErrorQuery("Error", $"Failed to delete file: {ex.Message}", "Ok");
                    }
                }
            }
        });

    }

    private void RegisterEvents()
    {
        listView.OpenSelectedItem += (sender, e) =>
        {
            int index = e.Item;
            if (index >= 0 && index < songs.Count)
            {
                uiEventBus.Publish(new SongSelected(songs[index].FullName));
            }
        };
    }
}
