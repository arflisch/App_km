using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Grid;
using Syncfusion.Pdf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Car_kilometer.RidesPage;
using Car_kilometer.NewFolder;

namespace Car_kilometer.Services
{
    public class PdfService
    {
        public void CreateRidesPdf(string filePath, Statistic statistic, string firstName, string lastName)
        {
            // Créer un document PDF
            using (PdfDocument pdfDocument = new PdfDocument())
            {
                // Ajouter une page au document PDF
                PdfPage page = pdfDocument.Pages.Add();

                // Définir une police pour le texte
                PdfFont font = new PdfStandardFont(PdfFontFamily.Helvetica, 12);

                // Ajouter un titre
                page.Graphics.DrawString("List of Rides", new PdfStandardFont(PdfFontFamily.Helvetica, 20), PdfBrushes.Black, new Syncfusion.Drawing.PointF(0, 0));

                // Afficher le prénom et le nom en haut à droite
                string nameText = $"{lastName.ToUpper()} {firstName}";
                float nameWidth = font.MeasureString(nameText).Width;
                page.Graphics.DrawString(nameText, font, PdfBrushes.Black,
                    new Syncfusion.Drawing.PointF(page.GetClientSize().Width - nameWidth - 10, 0));

                // Créer un tableau pour afficher les données des rides
                PdfGrid pdfGrid = new PdfGrid();

                // Ajouter des colonnes au tableau
                pdfGrid.Columns.Add(6);
                pdfGrid.Headers.Add(1);

                // Ajouter des en-têtes au tableau
                PdfGridRow pdfGridHeader = pdfGrid.Headers[0];
                pdfGridHeader.Cells[0].Value = "No.";
                pdfGridHeader.Cells[1].Value = "Date";
                pdfGridHeader.Cells[2].Value = "Description";
                pdfGridHeader.Cells[3].Value = "Meteo";
                pdfGridHeader.Cells[4].Value = "Duration";
                pdfGridHeader.Cells[5].Value = "Distance (km)";

                int rideNumber = 1;

                pdfGrid.Columns[0].Width = 30;

                // Ajouter les données des rides au tableau
                foreach (var ride in statistic.Rides)
                {
                    PdfGridRow row = pdfGrid.Rows.Add();
                    row.Cells[0].Value = rideNumber.ToString();
                    row.Cells[1].Value = ride.Date.ToString("yyyy-MM-dd");
                    row.Cells[2].Value = ride.Description;
                    row.Cells[3].Value = ride.WeatherCondition.ToString();
                    row.Cells[4].Value = $"{(int)TimeSpan.FromSeconds(ride.Duration).TotalHours}h{TimeSpan.FromSeconds(ride.Duration).Minutes:D2}";
                    row.Cells[5].Value = ride.Distance.ToString("F1");

                    rideNumber++;
                }

                // Dessiner le tableau sur la page PDF
                pdfGrid.Draw(page, new Syncfusion.Drawing.PointF(0, 50));

                // Ajouter le total des kilomètres en bas à droite du tableau
                string totalKmText = $"Total: {statistic.TotalDistance:F1} km";

                // Mesurer la taille du texte pour le placer correctement
                float totalKmTextWidth = font.MeasureString(totalKmText).Width;

                // Dessiner le texte aligné à droite
                page.Graphics.DrawString(totalKmText, font, PdfBrushes.Black,
                    new Syncfusion.Drawing.PointF(page.GetClientSize().Width - totalKmTextWidth - 10, pdfGrid.Rows.Count * 20 + 60));

                // Enregistrer le PDF dans un fichier
                using (FileStream outputFileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    pdfDocument.Save(outputFileStream);
                }

                // Optionnel : afficher un message de confirmation
                Console.WriteLine($"PDF created at: {filePath}");
            }
        }

    }
}
