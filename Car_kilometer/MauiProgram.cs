using Car_kilometer.ViewModels;
using Car_kilometer.Views;
using Microsoft.Extensions.Logging;
using Shiny;

namespace Car_kilometer
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp
                .CreateBuilder()
                .UseMauiApp<App>()
                .UseShiny()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("Manrope-Regular.ttf", "ManropeRegular");
                    fonts.AddFont("Manrope-SemiBold.ttf", "ManropeSemiBold");
                    fonts.AddFont("Manrope-ExtraBold.ttf", "ManropeExtraBold");
                    fonts.AddFont("CarKmIcons.ttf", "Icons");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            RemoveNativeInputBorders();

            var services = builder.Services;

            // GPS readings are delivered to RideGpsDelegate, also in the background
            services.AddGps<RideGpsDelegate>();

            services.AddSingleton<RideRepository>();
            services.AddSingleton<RideTracker>();
            services.AddSingleton<PdfReportService>();
            services.AddSingleton<PlaceNameService>();
            services.AddSingleton<UserSettings>();
            services.AddSingleton<DialogService>();

            services.AddSingleton<AppShell>();
            services.AddTransient<HomePage>().AddTransient<HomeViewModel>();
            services.AddTransient<RidePage>().AddTransient<RideViewModel>();
            services.AddTransient<HistoryPage>().AddTransient<HistoryViewModel>();
            services.AddTransient<RideEditorPage>().AddTransient<RideEditorViewModel>();
            services.AddTransient<ExportPage>().AddTransient<ExportViewModel>();

            return builder.Build();
        }

        // The text fields are drawn inside a styled Border (InputField style): drop the platform's own frame or underline.
        static void RemoveNativeInputBorders()
        {
            Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("Borderless", (handler, _) =>
            {
#if IOS
                handler.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None;
#elif ANDROID
                handler.PlatformView.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
#endif
            });

            Microsoft.Maui.Handlers.DatePickerHandler.Mapper.AppendToMapping("Borderless", (handler, _) =>
            {
#if IOS
                handler.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None;
#elif ANDROID
                handler.PlatformView.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
#endif
            });
        }
    }
}
