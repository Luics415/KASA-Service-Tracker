using KasaServiceTracker.Models;

namespace KasaServiceTracker.Services;

public interface IOrderExportService
{
    void ExportAll(IEnumerable<ServiceOrder> orders);
}
