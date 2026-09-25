using System.Globalization;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Grid;
using PointF = Syncfusion.Drawing.PointF;
using RectangleF = Syncfusion.Drawing.RectangleF;

namespace Car_kilometer.Services;

public sealed record ReportRequest(
    IReadOnlyList<RideItem> Rides,
    DateTime From,
    DateTime To,
    string FirstName,
    string LastName);

/// <summary>Builds the mileage report sent by email.</summary>
public sealed class PdfReportService
{
    const float Margin = 40;
    const float FooterHeight = 36;

    static readonly PdfColor Brand = new(79, 70, 229);
    static readonly PdfColor BrandDark = new(46, 31, 143);
    static readonly PdfColor Ink = new(18, 20, 43);
    static readonly PdfColor Muted = new(95, 102, 122);
    static readonly PdfColor Line = new(228, 231, 239);
    static readonly PdfColor Stripe = new(246, 246, 252);
    static readonly PdfColor Soft = new(238, 240, 255);

    /// <summary>Writes the report to <paramref name="filePath"/>, off the UI thread.</summary>
    public Task CreateAsync(ReportRequest request, string filePath) => Task.Run(() => Create(request, filePath));

    static void Create(ReportRequest request, string filePath)
    {
        var culture = CultureInfo.CurrentCulture;
        var rides = request.Rides.OrderBy(r => r.Date).ToList();

        using var regularData = OpenFont("Pdf.Manrope-Regular.ttf");
        using var boldData = OpenFont("Pdf.Manrope-ExtraBold.ttf");
        var text = new PdfTrueTypeFont(regularData, 9, embed: true, subset: true);
        var bold = new PdfTrueTypeFont(boldData, 9, embed: true, subset: true);

        using var document = new PdfDocument();
        document.PageSettings.Size = PdfPageSize.A4;
        document.PageSettings.Margins.All = 0;
        document.DocumentInformation.Title = AppResources.PdfTitle;
        document.DocumentInformation.Creator = AppResources.AppName;

        var page = document.Pages.Add();
        var size = page.GetClientSize();
        document.Template.Bottom = CreateFooter(size.Width, new PdfTrueTypeFont(text, 8));

        // Header band
        var g = page.Graphics;
        var band = new RectangleF(0, 0, size.Width, 112);
        g.DrawRectangle(new PdfLinearGradientBrush(band, Brand, BrandDark, PdfLinearGradientMode.ForwardDiagonal), band);
        g.DrawString(AppResources.PdfTitle, new PdfTrueTypeFont(bold, 22), PdfBrushes.White, new PointF(Margin, 32));
        g.DrawString(
            string.Format(culture, AppResources.PdfPeriod, Format.Date(request.From), Format.Date(request.To)),
            new PdfTrueTypeFont(text, 10), new PdfSolidBrush(new PdfColor(217, 219, 255)), new PointF(Margin, 66));

        var right = new PdfStringFormat(PdfTextAlignment.Right);
        var driver = $"{request.LastName.ToUpper(culture)} {request.FirstName}".Trim();
        g.DrawString(driver, new PdfTrueTypeFont(bold, 12), PdfBrushes.White, new PointF(size.Width - Margin, 34), right);
        g.DrawString(
            string.Format(culture, AppResources.PdfGeneratedOn, Format.Date(DateTime.Now)),
            new PdfTrueTypeFont(text, 9), new PdfSolidBrush(new PdfColor(217, 219, 255)), new PointF(size.Width - Margin, 56), right);

        // Summary tiles
        var totalKm = rides.Sum(r => r.DistanceKm);
        var totalTime = TimeSpan.FromTicks(rides.Sum(r => r.Duration.Ticks));
        var tiles = new[]
        {
            (AppResources.StatRides, rides.Count.ToString(culture)),
            (AppResources.StatDistance, Format.Km(totalKm)),
            (AppResources.StatDrivingTime, Format.Duration(totalTime)),
        };
        const float gap = 12;
        var tileWidth = (size.Width - 2 * Margin - 2 * gap) / 3;
        for (int i = 0; i < tiles.Length; i++)
        {
            var box = new RectangleF(Margin + i * (tileWidth + gap), 132, tileWidth, 58);
            var path = RoundedRectangle(box, 10);
            g.DrawPath(new PdfSolidBrush(Soft), path);
            g.DrawString(tiles[i].Item1.ToUpper(culture), new PdfTrueTypeFont(bold, 7.5f), new PdfSolidBrush(Muted), new PointF(box.X + 14, box.Y + 12));
            g.DrawString(tiles[i].Item2, new PdfTrueTypeFont(bold, 15), new PdfSolidBrush(Ink), new PointF(box.X + 14, box.Y + 27));
        }

        // Rides table
        var grid = CreateGrid(rides, totalKm, totalTime, text, bold, culture);
        var layout = new PdfGridLayoutFormat
        {
            Layout = PdfLayoutType.Paginate,
            Break = PdfLayoutBreakType.FitPage,
            PaginateBounds = new RectangleF(Margin, Margin, size.Width - 2 * Margin, size.Height - 2 * Margin - FooterHeight),
        };
        var result = grid.Draw(page, new RectangleF(Margin, 214, size.Width - 2 * Margin, size.Height - 214 - Margin), layout);

        // Signature, below the table (or on a new page when there is no room left)
        var signaturePage = result.Page;
        var y = result.Bounds.Bottom + 36;
        if (y + 70 > signaturePage.GetClientSize().Height - FooterHeight)
        {
            signaturePage = document.Pages.Add();
            y = Margin;
        }
        var sg = signaturePage.Graphics;
        var signatureX = size.Width - Margin - 200;
        sg.DrawString(AppResources.PdfSignature, new PdfTrueTypeFont(bold, 9), new PdfSolidBrush(Muted), new PointF(signatureX, y));
        sg.DrawLine(new PdfPen(Line, 1), new PointF(signatureX, y + 56), new PointF(size.Width - Margin, y + 56));

        using var output = File.Create(filePath);
        document.Save(output);
        document.Close(true);
    }

    static PdfGrid CreateGrid(List<RideItem> rides, double totalKm, TimeSpan totalTime, PdfFont text, PdfFont bold, CultureInfo culture)
    {
        var grid = new PdfGrid { RepeatHeader = true, AllowRowBreakAcrossPages = false };
        grid.Style.Font = text;
        grid.Style.CellPadding = new PdfPaddings(6, 6, 5, 5);
        grid.Columns.Add(6);

        float[] widths = [26, 68, 0, 64, 56, 66];
        for (int i = 0; i < widths.Length; i++)
        {
            if (widths[i] > 0)
                grid.Columns[i].Width = widths[i];
        }
        var numbers = new PdfStringFormat(PdfTextAlignment.Right, PdfVerticalAlignment.Middle);
        var words = new PdfStringFormat(PdfTextAlignment.Left, PdfVerticalAlignment.Middle) { WordWrap = PdfWordWrapType.Word };
        foreach (var column in new[] { 0, 4, 5 })
            grid.Columns[column].Format = numbers;
        foreach (var column in new[] { 1, 2, 3 })
            grid.Columns[column].Format = words;

        var noBorder = PdfPens.Transparent;
        var header = grid.Headers.Add(1)[0];
        string[] titles =
        [
            AppResources.PdfColumnNumber, AppResources.PdfColumnDate, AppResources.PdfColumnDescription,
            AppResources.PdfColumnWeather, AppResources.PdfColumnDuration, AppResources.PdfColumnDistance,
        ];
        for (int i = 0; i < titles.Length; i++)
        {
            var cell = header.Cells[i];
            cell.Value = titles[i];
            cell.Style = new PdfGridCellStyle
            {
                Font = new PdfTrueTypeFont((PdfTrueTypeFont)bold, 8.5f),
                TextBrush = PdfBrushes.White,
                BackgroundBrush = new PdfSolidBrush(Brand),
                Borders = new PdfBorders { All = new PdfPen(Brand, 0.5f) },
                CellPadding = new PdfPaddings(6, 6, 7, 7),
            };
        }

        var rowPen = new PdfPen(Line, 0.6f);
        for (int index = 0; index < rides.Count; index++)
        {
            var ride = rides[index];
            var row = grid.Rows.Add();
            row.Cells[0].Value = (index + 1).ToString(culture);
            row.Cells[1].Value = Format.Date(ride.LocalDate);
            row.Cells[2].Value = ride.DisplayDescription;
            row.Cells[3].Value = WeatherInfo.Label(ride.Weather);
            row.Cells[4].Value = Format.Duration(ride.Duration);
            row.Cells[5].Value = Format.Number(ride.DistanceKm);

            var background = index % 2 == 1 ? new PdfSolidBrush(Stripe) : PdfBrushes.White;
            foreach (PdfGridCell cell in row.Cells)
            {
                cell.Style.BackgroundBrush = background;
                cell.Style.TextBrush = new PdfSolidBrush(Ink);
                cell.Style.Borders = new PdfBorders { Left = noBorder, Right = noBorder, Top = noBorder, Bottom = rowPen };
            }
        }

        var total = grid.Rows.Add();
        total.Cells[0].Value = AppResources.PdfTotal;
        total.Cells[0].ColumnSpan = 4;
        total.Cells[0].StringFormat = new PdfStringFormat(PdfTextAlignment.Left, PdfVerticalAlignment.Middle);
        total.Cells[4].Value = Format.Duration(totalTime);
        total.Cells[5].Value = Format.Number(totalKm);
        foreach (PdfGridCell cell in total.Cells)
        {
            cell.Style.Font = new PdfTrueTypeFont((PdfTrueTypeFont)bold, 9.5f);
            cell.Style.BackgroundBrush = new PdfSolidBrush(Soft);
            cell.Style.TextBrush = new PdfSolidBrush(BrandDark);
            cell.Style.Borders = new PdfBorders { All = new PdfPen(Soft, 0.5f) };
            cell.Style.CellPadding = new PdfPaddings(6, 6, 8, 8);
        }

        return grid;
    }

    static PdfPageTemplateElement CreateFooter(float width, PdfFont font)
    {
        var footer = new PdfPageTemplateElement(new RectangleF(0, 0, width, FooterHeight));
        var brush = new PdfSolidBrush(Muted);
        footer.Graphics.DrawLine(new PdfPen(Line, 0.8f), new PointF(Margin, 4), new PointF(width - Margin, 4));
        footer.Graphics.DrawString(AppResources.AppName, font, brush, new PointF(Margin, 12));

        var pages = new PdfCompositeField(font, brush, AppResources.PdfPageNumber, new PdfPageNumberField(font, brush), new PdfPageCountField(font, brush));
        var label = string.Format(CultureInfo.CurrentCulture, AppResources.PdfPageNumber, "00", "00");
        pages.Draw(footer.Graphics, width - Margin - font.MeasureString(label).Width, 12);
        return footer;
    }

    static PdfPath RoundedRectangle(RectangleF r, float radius)
    {
        var d = radius * 2;
        var path = new PdfPath();
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    static Stream OpenFont(string name) =>
        typeof(PdfReportService).Assembly.GetManifestResourceStream(name)
        ?? throw new InvalidOperationException($"Missing embedded font {name}");
}
