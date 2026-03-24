using FinAssist.Core.Entities;

namespace FinAssist.Core.Interfaces;

public interface IExportService
{
    byte[] ExporterExcel(IEnumerable<Besoin> besoins);
    byte[] ExporterPdf(IEnumerable<Besoin> besoins);
}
