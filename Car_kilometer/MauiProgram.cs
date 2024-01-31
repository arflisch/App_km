using Car_kilometer.NewFolder;
using Car_kilometer.Services;
using Microsoft.Extensions.Logging;
using Realms.Exceptions;

namespace Car_kilometer
{
    public static class MauiProgram
    {

        public static IServiceProvider ServiceProvider { get; private set; }
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                })
                .UseMauiMaps();

#if DEBUG
    		builder.Logging.AddDebug();
#endif
            builder.Services.AddSingleton<Summary>();
            var app = builder.Build();
            ServiceProvider = app.Services;
            var folder = Environment.SpecialFolder.LocalApplicationData;
            var path = Environment.GetFolderPath(folder);
            var config = new RealmConfiguration(path + "my.realm")
            {
                IsReadOnly = false,
            };
            Realm localRealm;
            try
            {
                localRealm = Realm.GetInstance(config);
            }
            catch (RealmFileAccessErrorException ex)
            {
                Console.WriteLine($@"Error creating or opening the
                realm file. {ex.Message}");
            }

            return app;
        }
    }
}
