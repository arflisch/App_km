using Car_kilometer.Models;
using Car_kilometer.Services;
using Car_kilometer.ViewModels;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Shiny;

namespace Car_kilometer
{
    public static class MauiProgram
    {

        public static IServiceProvider ServiceProvider { get; private set; }
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp
          .CreateBuilder()
          .UseMauiApp<App>()
            // Initialize the .NET MAUI Community Toolkit by adding the below line of code
          .UseMauiCommunityToolkit()
          .UseShiny()
          .ConfigureFonts(fonts =>
          {
              fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
              fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
          });

#if DEBUG
            builder.Logging.SetMinimumLevel(LogLevel.Trace);
            builder.Logging.AddDebug();
#endif
            builder.Services.AddSingleton<Summary>();
            builder.Services.AddSingleton<SharedDto>();
            builder.Services.AddSingleton<MainPage>();
            builder.Services.AddTransient<MainPageVM>();

            // Register Shiny.
            builder.Services.AddGps<MyGpsDelegate>();
            
            var app = builder.Build();
            ServiceProvider = app.Services;

            return app;
        }
    }
}
