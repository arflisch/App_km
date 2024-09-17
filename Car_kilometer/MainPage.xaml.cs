using System;
using System.Threading;
using Car_kilometer.Services;
using Car_kilometer.NewFolder;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Grid;
using Syncfusion.Drawing;
using System.Reflection;
using System.Xml.Linq;
using static Car_kilometer.RidesPage;

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
            InitializeDatabaseAsync();

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
            TotalKmValue.Text = _summary.Statistic.TotalDistance.ToString("F1");
            TotalTimeValue.Text = string.Format("{0}h{1:00}", (int)time.TotalHours, time.Minutes);
            TotalRideValue.Text = _summary.Statistic.TotalRides.ToString();
        }


        // Méthode pour initialiser la base de données (appelée dans le constructeur)
        private async void InitializeDatabaseAsync()
        {
            // Appeler la méthode GetStatisticAsync pour créer la base de données Realm
            await _summary.GetStatisticAsync();
        }

        private async void ViewRidesButton_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new RidesPage());
        }

        private async void CreatePdfButton_Clicked(object sender, EventArgs e)
        {
            string recipientEmail = await DisplayPromptAsync("Enter Email", "Please enter the recipient's email address:",
                                                             maxLength: 50,
                                                             keyboard: Keyboard.Email);

            if (string.IsNullOrEmpty(recipientEmail))
            {
                await DisplayAlert("Error", "No email address provided. Cannot send the PDF.", "OK");
                return;
            }

            var folder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var filePath = Path.Combine(folder, "RidesList.pdf");
            var statistic = await _summary.GetStatisticAsync();

            var pdfService = new PdfService();
            pdfService.CreateRidesPdf(filePath, statistic);

            if (!File.Exists(filePath))
            {
                await DisplayAlert("Error", "PDF file was not generated successfully.", "OK");
                return;
            }

            if (Email.Default.IsComposeSupported)
            {
                string subject = "Your Ride List PDF";
                string body = "Please find attached the PDF with the list of your rides.";

                var message = new EmailMessage
                {
                    Subject = subject,
                    Body = body,
                    BodyFormat = EmailBodyFormat.PlainText,
                    To = new List<string> { recipientEmail }
                };

                message.Attachments?.Add(new EmailAttachment(filePath));

                await Email.Default.ComposeAsync(message);
            }
            else
            {
                await DisplayAlert("Error", "Email is not supported on this device.", "OK");
            }
        }


    }

}
