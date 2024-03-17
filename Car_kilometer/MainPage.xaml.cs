using Car_kilometer.Services;

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

            //// Appeler la méthode GetStatisticAsync dans le constructeur
            //InitializeDatabaseAsync();

            // S'abonner à l'événement OnAppearing
            this.Appearing += MainPage_Appearing;
        }

        // Méthode appelée lors de l'apparition de la page
        private async void MainPage_Appearing(object sender, EventArgs e)
        {
            // Appeler la méthode GetStatisticAsync lors de l'apparition de la page
            await _summary.GetStatisticAsync();

            // Convertir les secondes en heures et minutes
            TimeSpan time = TimeSpan.FromSeconds(_summary.Statistic.TotalSecondDurations);

            // Afficher les valeurs formatées dans les contrôles d'interface utilisateur
            TotalKmValue.Text = _summary.Statistic.TotalDistance.ToString("0");
            TotalTimeValue.Text = string.Format("{0}h{1:00}", (int)time.TotalHours, time.Minutes);
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
