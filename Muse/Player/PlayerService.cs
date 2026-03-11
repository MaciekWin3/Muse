using Muse.Utils;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
using LibVLCSharp.Shared;

namespace Muse.Player;

public class PlayerService : IPlayerService, IDisposable
{
    private readonly LibVLC _libVLC;
    private MediaPlayer? _mediaPlayer;
    private float _volume = 0.5f;
    private readonly YoutubeClient youtubeClient = new();

    public Track? CurrentTrack { get; private set; }
    public string? CurrentFilePath { get; private set; }
    
    public PlaybackState State
    {
        get
        {
            if (_mediaPlayer == null) return PlaybackState.Stopped;
            return _mediaPlayer.State switch
            {
                VLCState.Playing => PlaybackState.Playing,
                VLCState.Paused => PlaybackState.Paused,
                _ => PlaybackState.Stopped
            };
        }
    }

    public PlayerService()
    {
        Core.Initialize();
        _libVLC = new LibVLC();
        _volume = Globals.Volume;
        _mediaPlayer = new MediaPlayer(_libVLC);
        _mediaPlayer.Volume = (int)(_volume * 100);
    }

    public async Task<Result> Load(Track track)
    {
        try
        {
            Stop();

            string urlToLoad = track.Path;

            if (track.Source == TrackSource.YouTube)
            {
                if (string.IsNullOrWhiteSpace(track.YouTubeId))
                {
                    return Result.Fail("YouTube track is missing a valid YouTube ID.");
                }

                var manifest = await youtubeClient.Videos.Streams.GetManifestAsync(track.YouTubeId);
                var audioOnlyStreams = manifest.GetAudioOnlyStreams();
                var streamInfo = audioOnlyStreams.GetWithHighestBitrate();
                if (streamInfo == null)
                {
                    return Result.Fail("No suitable audio-only stream was found for the YouTube video.");
                }
                urlToLoad = streamInfo.Url;
            }

            var media = track.Source == TrackSource.YouTube 
                ? new Media(_libVLC, new Uri(urlToLoad))
                : new Media(_libVLC, urlToLoad, FromType.FromPath);

            _mediaPlayer!.Media = media;
            CurrentTrack = track;
            CurrentFilePath = track.Path;
            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to load audio: {ex.Message}");
        }
    }

    public Result Play()
    {
        if (_mediaPlayer is not null && _mediaPlayer.Media is not null)
        {
            _mediaPlayer.Play();
            return Result.Ok();
        }
        return Result.Fail("Unable to play audio file");
    }

    public Result Pause()
    {
        if (_mediaPlayer is not null)
        {
            _mediaPlayer.Pause();
            return Result.Ok();
        }
        return Result.Fail("MediaPlayer is not initialized");
    }

    public Result Stop()
    {
        if (_mediaPlayer is not null)
        {
            _mediaPlayer.Stop();
        }
        return Result.Ok();
    }

    public Result SetVolume(float volume)
    {
        _volume = volume;
        Globals.Volume = volume;
        
        if (_mediaPlayer != null)
        {
            _mediaPlayer.Volume = (int)(volume * 100);
        }
        
        return Result.Ok();
    }

    public Result<int> GetVolume()
    {
        return Result.Ok((int)(_volume * 10));
    }

    public Result<SongInfo> GetSongInfo()
    {
        if (_mediaPlayer is not null && _mediaPlayer.Media is not null)
        {
            var length = _mediaPlayer.Length; // in ms
            var time = _mediaPlayer.Time; // in ms
            
            var totalTime = length > 0 ? (int)(length / 1000) : 0;
            var currentTime = time > 0 ? (int)(time / 1000) : 0;
            
            // if streaming, length might be -1 or 0 initially
            if (length == -1) totalTime = 0;
            if (time == -1) currentTime = 0;
            
            string name = CurrentTrack != null ? CurrentTrack.Name : "Unknown";

            return Result.Ok(new SongInfo(name, totalTime, currentTime));
        }
        return Result.Fail<SongInfo>("Unable to get song info");
    }

    public Result ChangeCurrentSongTime(int seconds)
    {
        if (_mediaPlayer is not null)
        {
            _mediaPlayer.Time = seconds * 1000;
            return Result.Ok();
        }
        return Result.Fail("Unable to change current song time");
    }

    public void Dispose()
    {
        _mediaPlayer?.Stop();
        _mediaPlayer?.Dispose();
        _libVLC?.Dispose();
    }
}
