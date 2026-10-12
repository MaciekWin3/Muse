using Muse.Player;
using Muse.UI.Bus;
using System.Collections.ObjectModel;
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
    private readonly ObservableCollection<string> songNames = [];

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
            Source = new ListWrapper<string>(songNames)
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

        uiEventBus.Subscribe<SongSelected>(msg =>
        {
            App?.Invoke(() =>
            {
                int index = songs.FindIndex(t => t.Path == msg.Track.Path);
                if (index >= 0)
                {
                    listView.SelectedItem = index;
                }
            });
        });

        uiEventBus.Subscribe<PlaylistUpdated>(msg =>
        {
            // If the view isn't attached to the visual tree yet, App will be null.
            // During startup, this initial publish runs on the main thread anyway,
            // so we can safely fall back to a direct sync update.
            if (App is not null)
            {
                App.Invoke(UpdateList);
            }
            else
            {
                UpdateList();
            }

            void UpdateList()
            {
                songs = [.. msg.Songs];
                songNames.Clear();
                foreach (var s in songs)
                {
                    songNames.Add(s.Name);
                }
            }
        });

        uiEventBus.Subscribe<ChangeSongIndexRequested>(msg =>
        {
            var count = songs.Count;

            if (count <= 1 || msg.Offset == 0)
            {
                return;
            }

            int currentIndex = listView.SelectedItem ?? 0;
            int? nextIndex = new PlaylistNavigator(songs).GetAdjacentIndex(currentIndex, msg.Offset, _playMode);
            if (nextIndex is null)
            {
                return;
            }

            listView.SelectedItem = nextIndex.Value;

            if (nextIndex.Value < 0 || nextIndex.Value >= songs.Count)
            {
                if (App is not null)
                {
                    MessageBox.ErrorQuery(App, "Error", "Unable to obtain song info.", "Ok");
                }
                return;
            }

            var track = songs[nextIndex.Value];
            uiEventBus.Publish(new SongSelected(track));
        });

        uiEventBus.Subscribe<DeleteSongRequested>(_ =>
        {
            int selectedIndex = listView.SelectedItem ?? -1;
            if (selectedIndex >= 0 && selectedIndex < songs.Count && App is not null)
            {
                var track = songs[selectedIndex];
                if (track.Source != TrackSource.Local)
                {
                    return;
                }

                var result = MessageBox.Query(App, "Delete", $"Are you sure you want to delete {track.Name}?", "Yes", "No");
                if (result == 0) // Yes
                {
                    try
                    {
                        if (File.Exists(track.Path))
                        {
                            playerService.Stop();
                            File.Delete(track.Path);
                            uiEventBus.Publish(new RefreshPlaylistsRequested());
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.ErrorQuery(App, "Error", $"Failed to delete file: {ex.Message}", "Ok");
                    }
                }
            }
        });
    }

    private void RegisterEvents()
    {
        listView.ValueChanged += (sender, e) =>
        {
            if (e.NewValue.HasValue)
            {
                int index = e.NewValue.Value;

                if (index >= 0 && index < songs.Count)
                {
                    uiEventBus.Publish(new SongSelected(songs[index]));
                }
            }
        };
    }
}