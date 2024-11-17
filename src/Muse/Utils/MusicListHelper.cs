namespace Muse.Utils
{
    public static class MusicListHelper
    {
        public static IEnumerable<FileInfo> GetMusicList(string directoryPath)
        {
            var d = new DirectoryInfo(directoryPath);

            FileInfo[] Files = d.GetFiles("*.mp*");

            foreach (FileInfo file in Files)
            {
                yield return file;
            }
        }
    }
}
