namespace Muse.Player.Utils;

public class FileUtil
{
    private const string TempDirectoryName = "temp";

    public static string CheckFileToPlay(string orginalFileName)
    {
        var fileNameToReturn = orginalFileName;
        if (orginalFileName.Contains(' '))
        {
            Directory.CreateDirectory(TempDirectoryName);
            fileNameToReturn = TempDirectoryName + Path.DirectorySeparatorChar +
                               Path.GetFileName(orginalFileName).Replace(" ", string.Empty);
            File.Copy(orginalFileName, fileNameToReturn);
        }

        return fileNameToReturn;
    }

    public static void ClearTempFiles()
    {
        if (Directory.Exists(TempDirectoryName))
        {
            Directory.Delete(TempDirectoryName, true);
        }
    }
}