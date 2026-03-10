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
    private List<Track> songs = [];

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
        uiEventBus.Subscribe<ChangeSongIndexRequested>(async msg =>
        {
            var count = listView.Source.Count;

            if (count <= 1 || msg.Offset == 0)
            {
                return;
            }

            int currentIndex = listView.SelectedItem ?? 0;
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
                MessageBox.ErrorQuery(null, "Error", "Unable to obtain song info.", "Ok");
                return;
            }

            var track = songs[newIndex];

            var loadResult = await playerService.Load(track);
            if (!loadResult.Success)
            {
                MessageBox.ErrorQuery(null, "Error", $"Cannot load file: {loadResult.Error}", "Ok");
                return;
            }
            playerService.Play();
        });

        uiEventBus.Subscribe<DeleteSongRequested>(_ =>
        {
            int selectedIndex = listView.SelectedItem ?? -1;
            if (selectedIndex >= 0 && selectedIndex < songs.Count)
            {
                var track = songs[selectedIndex];
                if (track.Source != TrackSource.Local)
                {
                    // Cannot delete remote track from disk
                    return;
                }
                var result = MessageBox.Query(null, "Delete", $"Are you sure you want to delete {track.Name}?", "Yes", "No");
                if (result == 0) // Yes
                {
                    try
                    {
                        if (File.Exists(track.Path))
                        {
                            // If it's currently playing, we should probably stop it
                            playerService.Stop();
                            File.Delete(track.Path);
                            uiEventBus.Publish(new RefreshPlaylistsRequested());
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.ErrorQuery(null, "Error", $"Failed to delete file: {ex.Message}", "Ok");
                    }
                }
            }
        });

    }

    private void RegisterEvents()
    {
        listView.Activated += (sender, e) =>
        {
            int index = listView.SelectedItem ?? -1;
            if (index >= 0 && index < songs.Count)
            {
                uiEventBus.Publish(new SongSelected(songs[index]));
            }
        };
    }
}
