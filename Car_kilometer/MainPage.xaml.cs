using System;
using System.Threading;
using Car_kilometer.Services;
using Car_kilometer.NewFolder;

namespace Car_kilometer
{
    public partial class MainPage : ContentPage
    {
        private Summary _summary;

        public MainPage()
        {
            InitializeComponent();

            // Initialiser le service Summary
            _summary = MauiProgram.ServiceProvider.GetRequiredService<Summary>();

            // Appeler la méthode GetStatisticAsync dans le constructeur
            InitializeDatabaseAsync();

            // S'abonner à l'événement OnAppearing
            this.Appearing += MainPage_Appearing;
        }

        // Méthode appelée lors de l'apparition de la page
        private async void MainPage_Appearing(object sender, EventArgs e)
        {
            // Appeler la méthode GetStatisticAsync lors de l'apparition de la page
            await _summary.GetStatisticAsync();
            TotalTimeValue.Text = _summary.Statistic.TotalSecondDurations.ToString();
            TotalRideValue.Text = _summary.Statistic.TotalRides.ToString();
        }

        // Méthode pour initialiser la base de données (appelée dans le constructeur)
        private async void InitializeDatabaseAsync()
        {
            // Appeler la méthode GetStatisticAsync pour créer la base de données Realm
            await _summary.GetStatisticAsync();
        }
    }

}
