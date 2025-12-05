using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Muse.App;
using Muse.Player;
using Muse.Utils;
using Muse.YouTube;
using Terminal.Gui.App;

using var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddSingleton<MuseApp>();
        services.AddSingleton<IYoutubeDownloadService, YoutubeDownloadService>();
        services.AddSingleton<IPlayerService, PlayerService>();
    })
    .Build();

using var scope = host.Services.CreateScope();
var services = scope.ServiceProvider;

try
{
    string museDirectory = Environment.GetEnvironmentVariable("MUSE_DIRECTORY") ?? string.Empty;
    if (string.IsNullOrEmpty(museDirectory))
    {
        HandleMuseEnvironmentVariableMissing();
    }
    else
    {
        Globals.MuseDirectory = museDirectory;
    }

    InitApp();
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
}

void InitApp()
{
    var museApp = services.GetRequiredService<MuseApp>();
    Application.Init();
    Application.Run(museApp);
    Application.Shutdown();
}

static void HandleMuseEnvironmentVariableMissing()
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("MUSE_DIRECTORY environment variable is not set.");
    Console.ResetColor();
    if (OperatingSystem.IsWindows())
    {
        Console.WriteLine("Please set it using the following command in PowerShell:");
        Console.WriteLine("[Environment]::SetEnvironmentVariable(\"MUSE_DIRECTORY\", \"C:\\Path\\To\\Your\\Music\", \"User\")");
    }
    else
    {
        Console.WriteLine("Please set it using the following command in your shell:");
        Console.WriteLine("export MUSE_DIRECTORY=\"/path/to/your/music\"");
    }
    Environment.Exit(1);
}