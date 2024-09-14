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
        public void CreateRidesPdf(string filePath, IList<Ride> rides)
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

                // Créer un tableau pour afficher les données des rides
                PdfGrid pdfGrid = new PdfGrid();

                // Ajouter des colonnes au tableau
                pdfGrid.Columns.Add(3);
                pdfGrid.Headers.Add(1);

                // Ajouter des en-têtes au tableauje 
                PdfGridRow pdfGridHeader = pdfGrid.Headers[0];
                pdfGridHeader.Cells[0].Value = "Descrition";
                pdfGridHeader.Cells[1].Value = "Distance (km)";
                pdfGridHeader.Cells[2].Value = "Duration";

                // Ajouter les données des rides au tableau
                foreach (var ride in rides)
                {
                    PdfGridRow row = pdfGrid.Rows.Add();
                    row.Cells[0].Value = ride.Description;
                    row.Cells[1].Value = ride.Distance;
                    row.Cells[2].Value = ride.Duration;
                }

                // Dessiner le tableau sur la page PDF
                pdfGrid.Draw(page, new Syncfusion.Drawing.PointF(0, 50));

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
