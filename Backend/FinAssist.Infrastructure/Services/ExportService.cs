using ClosedXML.Excel;
using FinAssist.Core.Entities;
using FinAssist.Core.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestDocument = QuestPDF.Fluent.Document;

namespace FinAssist.Infrastructure.Services;

public class ExportService : IExportService
{
    public byte[] ExporterExcel(IEnumerable<Besoin> besoins)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Besoins");

        // En-têtes (sans ID)
        ws.Cell(1, 1).Value = "Titre";
        ws.Cell(1, 2).Value = "Statut";
        ws.Cell(1, 3).Value = "Importance";
        ws.Cell(1, 4).Value = "Catégorie";
        ws.Cell(1, 5).Value = "Créateur";
        ws.Cell(1, 6).Value = "Date création";
        ws.Cell(1, 7).Value = "Date modification";

        var headerRow = ws.Range(1, 1, 1, 7);
        headerRow.Style.Font.Bold = true;
        headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#1F4E79");
        headerRow.Style.Font.FontColor = XLColor.White;

        // Données
        int row = 2;
        foreach (var b in besoins)
        {
            ws.Cell(row, 1).Value = b.Titre;
            ws.Cell(row, 2).Value = b.Statut.ToString();
            ws.Cell(row, 3).Value = b.NiveauImportance;
            ws.Cell(row, 4).Value = b.Categorie?.Nom ?? string.Empty;
            ws.Cell(row, 5).Value = b.Utilisateur is not null
                ? $"{b.Utilisateur.Prenom} {b.Utilisateur.Nom}"
                : string.Empty;
            ws.Cell(row, 6).Value = b.DateCreation.ToString("yyyy-MM-dd HH:mm");
            ws.Cell(row, 7).Value = b.DateModification.ToString("yyyy-MM-dd HH:mm");
            row++;
        }

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public byte[] ExporterPdf(IEnumerable<Besoin> besoins)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var liste = besoins.ToList();

        var document = QuestDocument.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Text($"Rapport Besoins — {DateTime.UtcNow:dd/MM/yyyy HH:mm} UTC")
                    .SemiBold().FontSize(13).FontColor(Colors.Blue.Darken3);

                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn(3);    // Titre
                        cols.RelativeColumn(2);    // Statut
                        cols.RelativeColumn(1.5f); // Importance
                        cols.RelativeColumn(2);    // Catégorie
                        cols.RelativeColumn(2);    // Créateur
                        cols.RelativeColumn(2);    // Date création
                    });

                    // En-têtes (sans ID)
                    static IContainer HeaderCell(IContainer c) =>
                        c.Background(Colors.Blue.Darken3).Padding(4);

                    table.Header(header =>
                    {
                        foreach (var titre in new[] { "Titre", "Statut", "Importance", "Catégorie", "Créateur", "Date création" })
                            header.Cell().Element(HeaderCell).Text(titre).FontColor(Colors.White).SemiBold();
                    });

                    // Lignes
                    bool pair = false;
                    foreach (var b in liste)
                    {
                        pair = !pair;
                        var bg = pair ? Colors.Grey.Lighten4 : Colors.White;

                        IContainer DataCell(IContainer c) => c.Background(bg).Padding(4);

                        table.Cell().Element(DataCell).Text(b.Titre);
                        table.Cell().Element(DataCell).Text(b.Statut.ToString());
                        table.Cell().Element(DataCell).Text(b.NiveauImportance);
                        table.Cell().Element(DataCell).Text(b.Categorie?.Nom ?? "-");
                        table.Cell().Element(DataCell).Text(b.Utilisateur is not null
                            ? $"{b.Utilisateur.Prenom} {b.Utilisateur.Nom}" : "-");
                        table.Cell().Element(DataCell).Text(b.DateCreation.ToString("dd/MM/yyyy"));
                    }
                });

                page.Footer().AlignCenter()
                    .Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                        x.Span(" / ");
                        x.TotalPages();
                    });
            });
        });

        return document.GeneratePdf();
    }
}
