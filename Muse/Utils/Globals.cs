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
}
