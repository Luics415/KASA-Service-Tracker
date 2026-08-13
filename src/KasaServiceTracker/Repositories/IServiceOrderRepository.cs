using KasaServiceTracker.Models;

namespace KasaServiceTracker.Repositories;

public interface IServiceOrderRepository
{
    IReadOnlyList<ServiceOrder> GetAll();
    ServiceOrder? GetByFolio(string folio);
    void SaveAll(IEnumerable<ServiceOrder> orders);
}
