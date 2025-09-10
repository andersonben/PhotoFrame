using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using PhotoFrame.Data;
using PhotoFrame.Service.Services;
using PhotoFrame.Service.Configuration;

namespace PhotoFrame.Service
{
    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            try
            {
                var host = CreateHostBuilder(args).Build();
                
                // Ensure database is created
                using (var scope = host.Services.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<PhotoFrameDbContext>();
                    await context.Database.EnsureCreatedAsync();
                }

                await host.RunAsync();
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fatal error: {ex}");
                return 1;
            }
        }

        static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSystemd() // Enable systemd integration for Linux daemon
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                    config.AddEnvironmentVariables();
                    config.AddCommandLine(args);
                })
                .ConfigureServices((context, services) =>
                {
                    // Configuration
                    services.Configure<DisplaySettings>(
                        context.Configuration.GetSection(DisplaySettings.SectionName));

                    // Database
                    services.AddDbContext<PhotoFrameDbContext>(options =>
                        options.UseSqlite(context.Configuration.GetConnectionString("DefaultConnection")));

                    // Services
                    services.AddSingleton<WaveshareEInkDisplayService>();
                    services.AddSingleton<EInkDisplayService>(provider => provider.GetRequiredService<WaveshareEInkDisplayService>());
                    services.AddHostedService<PhotoDisplayService>();
                })
                .ConfigureLogging((context, logging) =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.AddSystemdConsole(); // For systemd journal logging
                    
                    if (context.HostingEnvironment.IsDevelopment())
                    {
                        logging.AddDebug();
                    }
                });
    }
}