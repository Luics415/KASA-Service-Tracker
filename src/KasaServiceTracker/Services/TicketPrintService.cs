using System.Diagnostics;
using System.Globalization;
using System.Text;
using KasaServiceTracker.Models;

namespace KasaServiceTracker.Services;

public sealed class TicketPrintService
{
    private readonly string _ticketsDirectory;

    public TicketPrintService(string ticketsDirectory)
    {
        _ticketsDirectory = ticketsDirectory;
        Directory.CreateDirectory(_ticketsDirectory);
    }

    public string BuildTicket(ServiceOrder order, string copyLabel)
    {
        var now = DateTime.Now;
        var builder = new StringBuilder();

        builder.AppendLine("========================================");
        builder.AppendLine("          KASA SERVICE TRACKER");
        builder.AppendLine($"            COPIA {copyLabel.ToUpperInvariant()}");
        builder.AppendLine("========================================");
        builder.AppendLine($"Folio:        {order.Folio}");
        builder.AppendLine($"Fecha:        {now:dd/MM/yyyy HH:mm}");
        builder.AppendLine($"Placas:       {order.Vehicle.Plate}");
        builder.AppendLine($"Vehículo:     {order.Vehicle.Brand} {order.Vehicle.Model} {order.Vehicle.Year}");
        builder.AppendLine($"Servicio:     {order.ServiceDescription}");
        builder.AppendLine($"Costo est.:   {order.EstimatedCost.ToString("C2", CultureInfo.CurrentCulture)}");
        builder.AppendLine("Entrega:      COMPLETADA");
        builder.AppendLine("========================================");
        builder.AppendLine(copyLabel.Equals("Empresa", StringComparison.OrdinalIgnoreCase)
            ? "Conservar como comprobante interno."
            : "Gracias por confiar en nuestro servicio.");
        builder.AppendLine("========================================");

        return builder.ToString();
    }

    public IReadOnlyList<string> PrintDoubleTicket(ServiceOrder order)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var paths = new List<string>();

        foreach (var copy in new[] { "Empresa", "Usuario" })
        {
            var content = BuildTicket(order, copy);
            var fileName = $"{order.Folio}-{timestamp}-{copy}.txt";
            var path = Path.Combine(_ticketsDirectory, fileName);
            File.WriteAllText(path, content, Encoding.UTF8);
            paths.Add(path);

            TrySendToDefaultPrinter(path);
        }

        return paths;
    }

    private static void TrySendToDefaultPrinter(string path)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "notepad.exe",
                Arguments = $"/p \"{path}\"",
                UseShellExecute = true,
                CreateNoWindow = true
            });
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"El ticket se guardó en '{path}', pero no pudo enviarse a la impresora predeterminada: {ex.Message}", ex);
        }
    }
}
