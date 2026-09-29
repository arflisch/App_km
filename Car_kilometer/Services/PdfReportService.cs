using System.Globalization;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Grid;
using PointF = Syncfusion.Drawing.PointF;
using RectangleF = Syncfusion.Drawing.RectangleF;
using SizeF = Syncfusion.Drawing.SizeF;

namespace Car_kilometer.Services;

public sealed record ReportRequest(
    IReadOnlyList<RideItem> Rides,
    DateTime From,
    DateTime To,
    string FirstName,
    string LastName);

public sealed record RoadbookRequest(
    RoadbookProgress Progress,
    string FirstName,
    string LastName,
    IReadOnlyList<string> Guides);

/// <summary>Builds the PDF documents sent by email: the mileage report and the learner driver's roadbook.</summary>
public sealed class PdfReportService
{
    const float Margin = 40;
    const float FooterHeight = 36;
    const float TableTop = 214;

    static readonly PdfColor Brand = new(79, 70, 229);
    static readonly PdfColor BrandDark = new(46, 31, 143);
    static readonly PdfColor OnBrandMuted = new(217, 219, 255);
    static readonly PdfColor Ink = new(18, 20, 43);
    static readonly PdfColor Muted = new(95, 102, 122);
    static readonly PdfColor Line = new(228, 231, 239);
    static readonly PdfColor Stripe = new(246, 246, 252);
    static readonly PdfColor Soft = new(238, 240, 255);

    /// <summary>Writes the mileage report to <paramref name="filePath"/>, off the UI thread.</summary>
    public Task CreateAsync(ReportRequest request, string filePath) => Task.Run(() => CreateMileageReport(request, filePath));

    /// <summary>Writes the roadbook (same columns as the official paper one) to <paramref name="filePath"/>, off the UI thread.</summary>
    public Task CreateRoadbookAsync(RoadbookRequest request, string filePath) => Task.Run(() => CreateRoadbook(request, filePath));

    static void CreateMileageReport(ReportRequest request, string filePath)
    {
        var rides = request.Rides.OrderBy(r => r.Date).ToList();
        var totalKm = rides.Sum(r => r.DistanceKm);
        var totalTime = TimeSpan.FromTicks(rides.Sum(r => r.Duration.Ticks));

        using var report = new Report(AppResources.PdfTitle);
        report.DrawHeader(
            AppResources.PdfTitle,
            report.Text(AppResources.PdfPeriod, Format.Date(request.From), Format.Date(request.To)),
            [Driver(request.FirstName, request.LastName), report.Text(AppResources.PdfGeneratedOn, Format.Date(DateTime.Now))]);
        report.DrawTiles(
        [
            (AppResources.StatRides, rides.Count.ToString(report.Culture)),
            (AppResources.StatDistance, Format.Km(totalKm)),
            (AppResources.StatDrivingTime, Format.Duration(totalTime)),
        ]);

        var grid = report.CreateGrid(
            [
                (AppResources.PdfColumnNumber, 34, true), (AppResources.PdfColumnDate, 68, false), (AppResources.PdfColumnDescription, 0, false),
                (AppResources.PdfColumnWeather, 64, false), (AppResources.PdfColumnDuration, 56, true), (AppResources.PdfColumnDistance, 78, true),
            ]);
        for (int i = 0; i < rides.Count; i++)
        {
            var ride = rides[i];
            report.AddRow(grid, i,
            [
                (i + 1).ToString(report.Culture), Format.Date(ride.LocalDate), ride.DisplayDescription,
                WeatherInfo.Label(ride.Weather), Format.Duration(ride.Duration), Format.Number(ride.DistanceKm),
            ]);
        }
        report.AddTotals(grid, [(AppResources.PdfTotal, 4), (Format.Duration(totalTime), 1), (Format.Number(totalKm), 1)]);

        report.DrawTableAndSignatures(grid, [AppResources.PdfSignature], alignRight: true);
        report.Save(filePath);
    }

    static void CreateRoadbook(RoadbookRequest request, string filePath)
    {
        var progress = request.Progress;
        var rides = progress.Rides;

        using var report = new Report(AppResources.PdfRoadbookTitle);
        report.DrawHeader(
            AppResources.PdfRoadbookTitle,
            report.Text(AppResources.PdfRoadbookSubtitle, Format.Date(progress.Start)),
            [
                Driver(request.FirstName, request.LastName),
                report.Text(AppResources.PdfGuides, string.Join(", ", request.Guides)),
                report.Text(AppResources.PdfGeneratedOn, Format.Date(DateTime.Now)),
            ]);
        report.DrawTiles(
        [
            (AppResources.PdfSessions, rides.Count.ToString(report.Culture)),
            (AppResources.StatDistance, report.Text(AppResources.RoadbookProgress, Format.Number(progress.Km), Format.Number(progress.GoalKm, 0))),
            (AppResources.StatDrivingTime, Format.Duration(progress.Time)),
        ]);

        // The columns of the "tableau de progression générale" of the official AWSR roadbook
        var grid = report.CreateGrid(
            [
                (AppResources.PdfColumnSession, 46, true), (AppResources.PdfColumnDate, 66, false), (AppResources.PdfColumnStart, 60, false),
                (AppResources.PdfColumnGuide, 64, false), (AppResources.PdfColumnRoute, 0, false), (AppResources.PdfColumnConditions, 70, false),
                (AppResources.PdfColumnKm, 48, true),
            ]);
        for (int i = 0; i < rides.Count; i++)
        {
            var ride = rides[i];
            var roads = RoadTypeInfo.Describe(ride.RoadTypes);
            report.AddRow(grid, i,
            [
                (i + 1).ToString(report.Culture),
                Format.Date(ride.LocalDate),
                $"{Format.Time(ride.LocalDate)}\n{Format.Duration(ride.Duration)}",
                ride.Guide,
                roads.Length > 0 ? $"{ride.DisplayDescription}\n{roads}" : ride.DisplayDescription,
                WeatherInfo.Label(ride.Weather),
                Format.Number(ride.DistanceKm),
            ]);
        }
        report.AddTotals(grid, [(AppResources.PdfTotals, 2), (Format.Duration(progress.Time), 1), (string.Empty, 3), (Format.Number(progress.Km), 1)]);

        string[] signatures =
        [
            AppResources.PdfLearnerSignature,
            .. request.Guides.Select(guide => report.Text(AppResources.PdfGuideSignature, guide)),
        ];
        report.DrawTableAndSignatures(grid, signatures, alignRight: false);
        report.Save(filePath);
    }

    static string Driver(string firstName, string lastName) =>
        $"{lastName.ToUpper(CultureInfo.CurrentCulture)} {firstName}".Trim();

    /// <summary>An A4 document with the app's header band, summary tiles, table and footer.</summary>
    sealed class Report : IDisposable
    {
        readonly Stream _regularData = OpenFont("Pdf.Manrope-Regular.ttf");
        readonly Stream _boldData = OpenFont("Pdf.Manrope-ExtraBold.ttf");
        readonly PdfTrueTypeFont _text;
        readonly PdfTrueTypeFont _bold;
        readonly PdfDocument _document = new();
        readonly PdfPage _page;
        readonly SizeF _size;

        public Report(string title)
        {
            _text = new PdfTrueTypeFont(_regularData, 9, embed: true, subset: true);
            _bold = new PdfTrueTypeFont(_boldData, 9, embed: true, subset: true);

            _document.PageSettings.Size = PdfPageSize.A4;
            _document.PageSettings.Margins.All = 0;
            _document.DocumentInformation.Title = title;
            _document.DocumentInformation.Creator = AppResources.AppName;

            _page = _document.Pages.Add();
            _size = _page.GetClientSize();
            _document.Template.Bottom = CreateFooter(_size.Width, new PdfTrueTypeFont(_text, 8));
        }

        public CultureInfo Culture { get; } = CultureInfo.CurrentCulture;

        public string Text(string format, params object[] values) => string.Format(Culture, format, values);

        /// <summary>Gradient band with the title on the left and up to three lines on the right.</summary>
        public void DrawHeader(string title, string subtitle, string[] rightLines)
        {
            var g = _page.Graphics;
            var band = new RectangleF(0, 0, _size.Width, 112);
            g.DrawRectangle(new PdfLinearGradientBrush(band, Brand, BrandDark, PdfLinearGradientMode.ForwardDiagonal), band);
            g.DrawString(title, new PdfTrueTypeFont(_bold, 22), PdfBrushes.White, new PointF(Margin, 32));
            g.DrawString(subtitle, new PdfTrueTypeFont(_text, 10), new PdfSolidBrush(OnBrandMuted), new PointF(Margin, 66));

            var right = new PdfStringFormat(PdfTextAlignment.Right);
            var y = rightLines.Length > 2 ? 28f : 34f;
            for (int i = 0; i < rightLines.Length; i++)
            {
                var font = i == 0 ? new PdfTrueTypeFont(_bold, 12) : new PdfTrueTypeFont(_text, 9);
                var brush = i == 0 ? PdfBrushes.White : new PdfSolidBrush(OnBrandMuted);
                g.DrawString(rightLines[i], font, brush, new PointF(_size.Width - Margin, y), right);
                y += i == 0 ? 22 : 16;
            }
        }

        public void DrawTiles((string Label, string Value)[] tiles)
        {
            var g = _page.Graphics;
            const float gap = 12;
            var width = (_size.Width - 2 * Margin - (tiles.Length - 1) * gap) / tiles.Length;
            for (int i = 0; i < tiles.Length; i++)
            {
                var box = new RectangleF(Margin + i * (width + gap), 132, width, 58);
                g.DrawPath(new PdfSolidBrush(Soft), RoundedRectangle(box, 10));
                g.DrawString(tiles[i].Label.ToUpper(Culture), new PdfTrueTypeFont(_bold, 7.5f), new PdfSolidBrush(Muted), new PointF(box.X + 14, box.Y + 12));
                g.DrawString(tiles[i].Value, new PdfTrueTypeFont(_bold, 15), new PdfSolidBrush(Ink), new PointF(box.X + 14, box.Y + 27));
            }
        }

        /// <summary>A table with a brand-colored header. A width of 0 lets the column take the remaining space.</summary>
        public PdfGrid CreateGrid((string Title, float Width, bool IsNumber)[] columns)
        {
            var grid = new PdfGrid { RepeatHeader = true, AllowRowBreakAcrossPages = false };
            grid.Style.Font = _text;
            grid.Style.CellPadding = new PdfPaddings(6, 6, 5, 5);
            grid.Columns.Add(columns.Length);

            var numbers = new PdfStringFormat(PdfTextAlignment.Right, PdfVerticalAlignment.Middle);
            var words = new PdfStringFormat(PdfTextAlignment.Left, PdfVerticalAlignment.Middle) { WordWrap = PdfWordWrapType.Word };
            var header = grid.Headers.Add(1)[0];
            for (int i = 0; i < columns.Length; i++)
            {
                if (columns[i].Width > 0)
                    grid.Columns[i].Width = columns[i].Width;
                grid.Columns[i].Format = columns[i].IsNumber ? numbers : words;

                var cell = header.Cells[i];
                cell.Value = columns[i].Title;
                cell.Style = new PdfGridCellStyle
                {
                    Font = new PdfTrueTypeFont(_bold, 8.5f),
                    TextBrush = PdfBrushes.White,
                    BackgroundBrush = new PdfSolidBrush(Brand),
                    Borders = new PdfBorders { All = new PdfPen(Brand, 0.5f) },
                    CellPadding = new PdfPaddings(6, 6, 7, 7),
                };
            }
            return grid;
        }

        public void AddRow(PdfGrid grid, int index, string[] values)
        {
            var row = grid.Rows.Add();
            var background = index % 2 == 1 ? new PdfSolidBrush(Stripe) : PdfBrushes.White;
            var noBorder = PdfPens.Transparent;
            var rowPen = new PdfPen(Line, 0.6f);
            for (int i = 0; i < values.Length; i++)
            {
                var cell = row.Cells[i];
                cell.Value = values[i];
                cell.Style.BackgroundBrush = background;
                cell.Style.TextBrush = new PdfSolidBrush(Ink);
                cell.Style.Borders = new PdfBorders { Left = noBorder, Right = noBorder, Top = noBorder, Bottom = rowPen };
            }
        }

        /// <summary>The bold last row; each value spans the given number of columns.</summary>
        public void AddTotals(PdfGrid grid, (string Value, int Span)[] cells)
        {
            var row = grid.Rows.Add();
            var column = 0;
            foreach (var (value, span) in cells)
            {
                row.Cells[column].Value = value;
                row.Cells[column].ColumnSpan = span;
                column += span;
            }
            row.Cells[0].StringFormat = new PdfStringFormat(PdfTextAlignment.Left, PdfVerticalAlignment.Middle);
            foreach (PdfGridCell cell in row.Cells)
            {
                cell.Style.Font = new PdfTrueTypeFont(_bold, 9.5f);
                cell.Style.BackgroundBrush = new PdfSolidBrush(Soft);
                cell.Style.TextBrush = new PdfSolidBrush(BrandDark);
                cell.Style.Borders = new PdfBorders { All = new PdfPen(Soft, 0.5f) };
                cell.Style.CellPadding = new PdfPaddings(6, 6, 8, 8);
            }
        }

        /// <summary>Draws the table over as many pages as needed, then the signature boxes below it.</summary>
        public void DrawTableAndSignatures(PdfGrid grid, string[] signatures, bool alignRight)
        {
            var layout = new PdfGridLayoutFormat
            {
                Layout = PdfLayoutType.Paginate,
                Break = PdfLayoutBreakType.FitPage,
                PaginateBounds = new RectangleF(Margin, Margin, _size.Width - 2 * Margin, _size.Height - 2 * Margin - FooterHeight),
            };
            var result = grid.Draw(_page, new RectangleF(Margin, TableTop, _size.Width - 2 * Margin, _size.Height - TableTop - Margin), layout);

            // Below the table, or on a new page when there is no room left
            var page = result.Page;
            var y = result.Bounds.Bottom + 36;
            if (y + 70 > page.GetClientSize().Height - FooterHeight)
            {
                page = _document.Pages.Add();
                y = Margin;
            }

            const float gap = 24;
            var width = alignRight ? 200 : (_size.Width - 2 * Margin - (signatures.Length - 1) * gap) / signatures.Length;
            var x = alignRight ? _size.Width - Margin - width : Margin;
            foreach (var label in signatures)
            {
                page.Graphics.DrawString(label, new PdfTrueTypeFont(_bold, 9), new PdfSolidBrush(Muted), new RectangleF(x, y, width, 24));
                page.Graphics.DrawLine(new PdfPen(Line, 1), new PointF(x, y + 56), new PointF(x + width, y + 56));
                x += width + gap;
            }
        }

        public void Save(string filePath)
        {
            using var output = File.Create(filePath);
            _document.Save(output);
            _document.Close(true);
        }

        public void Dispose()
        {
            _document.Dispose();
            _regularData.Dispose();
            _boldData.Dispose();
        }
    }

    static PdfPageTemplateElement CreateFooter(float width, PdfFont font)
    {
        var footer = new PdfPageTemplateElement(new RectangleF(0, 0, width, FooterHeight));
        var brush = new PdfSolidBrush(Muted);
        footer.Graphics.DrawLine(new PdfPen(Line, 0.8f), new PointF(Margin, 4), new PointF(width - Margin, 4));
        footer.Graphics.DrawString(AppResources.AppName, font, brush, new PointF(Margin, 12));

        var pages = new PdfCompositeField(font, brush, AppResources.PdfPageNumber, new PdfPageNumberField(font, brush), new PdfPageCountField(font, brush))
        {
            Bounds = new RectangleF(Margin, 12, width - 2 * Margin, FooterHeight - 12),
            StringFormat = new PdfStringFormat(PdfTextAlignment.Right),
        };
        pages.Draw(footer.Graphics, 0, 0);
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
