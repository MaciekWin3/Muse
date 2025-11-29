using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Muse;
using Muse.Player;
using Terminal.Gui.App;

using var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddSingleton<MuseApp>();
        services.AddSingleton<IPlayerService, PlayerService>();
    })
    .Build();

using var scope = host.Services.CreateScope();
var services = scope.ServiceProvider;

try
{
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

    /*
    var menuBar = museApp.InitMenuBar();
    var statusBar = museApp.InitStatusBar();
    museApp.Add(menuBar, statusBar);
    museApp.Add(new MainWindow(services.GetRequiredService<IPlayer>()));
    */

    Application.Run(museApp);
    Application.Shutdown();
}
