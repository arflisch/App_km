using Car_kilometer.NewFolder;
using Car_kilometer.Services;
using Microsoft.Extensions.Logging;
using Realms.Exceptions;
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
          .UseShiny()
          .ConfigureFonts(fonts =>
          {
              fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
              fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
          });

#if DEBUG
            builder.Logging.AddDebug();
#endif
            builder.Services.AddSingleton<Summary>();
            builder.Services.AddGps<MyGpsDelegate>();
            var app = builder.Build();
            ServiceProvider = app.Services;

            return app;
        }
    }
}
