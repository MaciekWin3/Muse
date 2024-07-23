using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Muse;
using Muse.Player;

using IHost host = CreateHostBuilder(args).Build();
using var scope = host.Services.CreateScope();

var services = scope.ServiceProvider;

//// Audio Test
//var file = @"C:\Users\macie\Music\Miszmasz\Dua Lipa - Houdini (Official Music Video).mp3";
//var reader = new Mp3FileReader(file);
//var waveOut = new WaveOutEvent(); // or WaveOutEvent()
//waveOut.Init(reader);
//waveOut.Play();
//await Task.Delay(1000);
//waveOut.Stop();
//await Task.Delay(1000);
//waveOut.Play();
//Console.ReadKey();

try
{
    services.GetRequiredService<App>().Run(args);
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
}

static IHostBuilder CreateHostBuilder(string[] args)
{
    return Host.CreateDefaultBuilder(args)
        .ConfigureServices((_, services) =>
        {
            services.AddSingleton<App>();
            services.AddSingleton<IPlayer, Player>();
        });
}