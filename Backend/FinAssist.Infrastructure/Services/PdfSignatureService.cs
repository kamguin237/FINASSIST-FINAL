using FinAssist.Core.Interfaces;
using PdfSharp.Drawing;
using PdfSharp.Pdf.IO;
using System.Text.RegularExpressions;

namespace FinAssist.Infrastructure.Services;

public class PdfSignatureService : IPdfSignatureService
{
    private static string NormalizeBase64(string signatureBase64)
    {
        if (string.IsNullOrWhiteSpace(signatureBase64))
            throw new InvalidOperationException("Signature manuscrite vide.");

        var value = signatureBase64.Trim();

        // Format attendu côté front: data:image/png;base64,AAAA...
        // On retire proprement le préfixe s'il existe.
        var match = Regex.Match(value, @"^data:image\/[a-zA-Z0-9.+-]+;base64,(?<data>.+)$", RegexOptions.IgnoreCase);
        var base64 = match.Success ? match.Groups["data"].Value : value;

        // Nettoyage défensif
        base64 = base64.Trim()
                       .Replace("\r", string.Empty)
                       .Replace("\n", string.Empty)
                       .Replace(" ", "+");

        // Padding base64
        var mod4 = base64.Length % 4;
        if (mod4 > 0)
            base64 = base64.PadRight(base64.Length + (4 - mod4), '=');

        return base64;
    }

    public byte[] IncrusterSignature(
        byte[] pdfOriginal,
        string signatureBase64,
        double positionXPct,
        double positionYPct,
        int largeur,
        int hauteur,
        string signataireNom,
        string signataireRole,
        DateTime horodatage)
    {
        if (pdfOriginal is null || pdfOriginal.Length == 0)
            throw new InvalidOperationException("Le document PDF source est vide.");

        byte[] imageBytes;
        try
        {
            var base64Data = NormalizeBase64(signatureBase64);
            imageBytes = Convert.FromBase64String(base64Data);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Signature base64 invalide: {ex.Message}");
        }

        try
        {
            using var inputStream = new MemoryStream(pdfOriginal);
            var document = PdfReader.Open(inputStream, PdfDocumentOpenMode.Modify);
            if (document.PageCount == 0)
                throw new InvalidOperationException("Le PDF ne contient aucune page.");

            var pageWidth = document.Pages[0].Width.Point;
            var pageHeight = document.Pages[0].Height.Point;

            // positionXPct et positionYPct sont relatifs au document entier (toutes pages confondues)
            // On travaille sur la page 0 uniquement — convertir le % global en position sur cette page
            var totalDocHeight = pageHeight * document.PageCount;

            // Position absolue dans le document (en points, depuis le haut)
            var absYFromTop = (positionYPct / 100.0) * totalDocHeight;

            // Numéro de page (0-based) où se trouve le clic
            var pageIndex = (int)(absYFromTop / pageHeight);
            pageIndex = Math.Max(0, Math.Min(pageIndex, document.PageCount - 1));

            // Position Y relative à la page cible (depuis le haut de cette page)
            var yOnPageFromTop = absYFromTop - (pageIndex * pageHeight);

            // Sélectionner la bonne page
            var page = document.Pages[pageIndex];
            var gfx = XGraphics.FromPdfPage(page);

            // Convertir en coordonnées PdfSharp (origine = bas gauche, Y inversé)
            var x = (positionXPct / 100.0) * pageWidth;
            var y = pageHeight - yOnPageFromTop - hauteur;

            // Clamper dans les limites de la page
            x = Math.Max(0, Math.Min(x, pageWidth - largeur));
            y = Math.Max(0, Math.Min(y, pageHeight - hauteur));

            Console.WriteLine($"[FINASSIST][PdfSignature] pages={document.PageCount} totalH={totalDocHeight:F0}pt | posX%={positionXPct} posY%={positionYPct} | absY={absYFromTop:F1}pt pageIndex={pageIndex} yOnPage={yOnPageFromTop:F1}pt | x={x:F1}pt y={y:F1}pt");

            // Dessiner l'image de signature
            using var imgStream = new MemoryStream(imageBytes);
            var xImage = XImage.FromStream(imgStream);
            gfx.DrawImage(xImage, x, y, largeur, hauteur);

            // Texte de certification (Helvetica = police PDF standard, pas besoin de résolution système)
            try
            {
                var font = new XFont("Helvetica", 6, XFontStyleEx.Regular);
                var brush = XBrushes.DarkGray;
                gfx.DrawString($"Signé par : {signataireNom} ({signataireRole})", font, brush,
                    new XRect(x, y + hauteur + 2, largeur, 10), XStringFormats.TopLeft);
                gfx.DrawString($"Date : {horodatage:dd/MM/yyyy HH:mm:ss}", font, brush,
                    new XRect(x, y + hauteur + 10, largeur, 10), XStringFormats.TopLeft);
            }
            catch
            {
                // Si la police n'est pas disponible, on ignore le texte de certification
            }

            using var outputStream = new MemoryStream();
            document.Save(outputStream);
            return outputStream.ToArray();
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Incrustation PDF impossible: {ex.Message}");
        }
    }
}
