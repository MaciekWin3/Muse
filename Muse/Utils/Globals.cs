namespace Muse.Utils;

public static class Globals
{
    public static string MuseDirectory
    {
        get => museDirectory;
        set => museDirectory = value
            ?? throw new ArgumentNullException(nameof(MuseDirectory));
    }

    private static string museDirectory = string.Empty;

    public const int BUTTONS_FRAME_HEIGHT = 3;
    public const int BUTTONS_HEIGHT = 2;
    public const int BUTTONS_WIDTH = 1;
    public const int PROGRESS_BAR_HEIGHT = 3;
    public const int VOLUME_SLIDER_HEIGHT = 4;
}
