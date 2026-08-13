using KasaServiceTracker.Models;
using KasaServiceTracker.Repositories;

namespace KasaServiceTracker.Services;

public sealed class ServiceOrderService
{
    private readonly IServiceOrderRepository _repository;
    private readonly IOrderExportService? _exporter;

    public ServiceOrderService(IServiceOrderRepository repository, IOrderExportService? exporter = null)
    {
        _repository = repository;
        _exporter = exporter;
    }

    public IReadOnlyList<ServiceOrder> GetAllOrders() =>
        _repository.GetAll()
            .OrderByDescending(order => order.CreatedAt)
            .ToList();

    public IReadOnlyList<ServiceOrder> GetActiveOrdersForCurrentWeek(DateTime? now = null)
    {
        var today = (now ?? DateTime.Now).Date;
        var daysSinceMonday = ((int)today.DayOfWeek + 6) % 7;
        var start = today.AddDays(-daysSinceMonday);
        var end = start.AddDays(7);
        return _repository.GetAll()
            .Where(order => order.Status != ServiceStatus.Terminado
                            && order.CreatedAt >= start && order.CreatedAt < end)
            .OrderByDescending(order => order.CreatedAt)
            .ToList();
    }

    public ServiceOrder CreateOrder(
        string plate,
        string brand,
        string model,
        int year,
        string serviceDescription,
        decimal estimatedCost)
    {
        plate = NormalizeRequired(plate, "placas").ToUpperInvariant();
        brand = Upper(NormalizeRequired(brand, "marca"));
        model = Upper(NormalizeRequired(model, "modelo"));
        serviceDescription = Upper(NormalizeRequired(serviceDescription, "servicio"));

        var currentYear = DateTime.Now.Year + 1;
        if (year < 1950 || year > currentYear)
        {
            throw new ArgumentOutOfRangeException(nameof(year),
                $"El año debe estar entre 1950 y {currentYear}.");
        }

        if (estimatedCost < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(estimatedCost),
                "El costo estimado no puede ser negativo.");
        }

        var orders = _repository.GetAll().ToList();
        var now = DateTime.Now;
        var order = new ServiceOrder
        {
            Folio = GenerateFolio(orders, now),
            Vehicle = new Vehicle
            {
                Plate = plate,
                Brand = brand,
                Model = model,
                Year = year
            },
            ServiceDescription = serviceDescription,
            EstimatedCost = estimatedCost,
            CreatedAt = now,
            Status = ServiceStatus.Recibido,
            History = new List<StatusChange>
            {
                new()
                {
                    Status = ServiceStatus.Recibido,
                    ChangedAt = now,
                    Note = "ORDEN REGISTRADA."
                }
            }
        };

        orders.Add(order);
        _repository.SaveAll(orders);
        _exporter?.ExportAll(orders);
        return order;
    }

    public ServiceOrder EditOrder(string folio, string plate, string brand, string model,
        int year, string serviceDescription, decimal estimatedCost)
    {
        var orders = _repository.GetAll().ToList();
        var order = orders.FirstOrDefault(item => item.Folio.Equals(folio.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? throw new KeyNotFoundException($"No existe la orden con folio {folio}.");
        var currentYear = DateTime.Now.Year + 1;
        if (year < 1950 || year > currentYear) throw new ArgumentOutOfRangeException(nameof(year), $"El año debe estar entre 1950 y {currentYear}.");
        if (estimatedCost < 0) throw new ArgumentOutOfRangeException(nameof(estimatedCost), "El costo estimado no puede ser negativo.");

        order.Vehicle.Plate = Upper(NormalizeRequired(plate, "placas"));
        order.Vehicle.Brand = Upper(NormalizeRequired(brand, "marca"));
        order.Vehicle.Model = Upper(NormalizeRequired(model, "modelo"));
        order.Vehicle.Year = year;
        order.ServiceDescription = Upper(NormalizeRequired(serviceDescription, "servicio"));
        order.EstimatedCost = estimatedCost;
        order.History.Add(new StatusChange { Status = order.Status, ChangedAt = DateTime.Now, Note = "CORRECCIÓN DE REGISTRO. DATOS DE LA ORDEN ACTUALIZADOS." });
        _repository.SaveAll(orders);
        _exporter?.ExportAll(orders);
        return order;
    }

    public ServiceOrder GetOrderByFolio(string folio)
    {
        folio = NormalizeRequired(folio, "folio");
        return _repository.GetByFolio(folio)
               ?? throw new KeyNotFoundException($"No existe la orden con folio {folio}.");
    }

    public IReadOnlyList<ServiceOrder> SearchByPlate(string plate)
    {
        plate = NormalizeRequired(plate, "placas");
        return _repository.GetAll()
            .Where(order => order.Vehicle.Plate.Contains(
                plate, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(order => order.CreatedAt)
            .ToList();
    }

    public ServiceOrder UpdateStatus(string folio, ServiceStatus newStatus, string? note)
    {
        var orders = _repository.GetAll().ToList();
        var order = orders.FirstOrDefault(item =>
            item.Folio.Equals(folio.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? throw new KeyNotFoundException($"No existe la orden con folio {folio}.");

        if (!Enum.IsDefined(typeof(ServiceStatus), newStatus))
        {
            throw new ArgumentOutOfRangeException(nameof(newStatus), "Estado inválido.");
        }

        if (order.Status == ServiceStatus.Terminado)
        {
            throw new InvalidOperationException("Una orden terminada ya no puede cambiar de estado.");
        }

        if ((int)newStatus < (int)order.Status)
        {
            throw new InvalidOperationException(
                "El flujo de servicio no permite regresar a un estado anterior.");
        }

        if (newStatus == order.Status)
        {
            throw new InvalidOperationException("La orden ya se encuentra en ese estado.");
        }

        order.Status = newStatus;
        order.History.Add(new StatusChange
        {
            Status = newStatus,
            ChangedAt = DateTime.Now,
            Note = string.IsNullOrWhiteSpace(note) ? "CAMBIO DE ESTADO." : Upper(note.Trim())
        });

        _repository.SaveAll(orders);
        _exporter?.ExportAll(orders);
        return order;
    }

    public ServiceOrder CompleteAfterTicket(string folio)
    {
        var orders = _repository.GetAll().ToList();
        var order = orders.FirstOrDefault(item =>
            item.Folio.Equals(folio.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? throw new KeyNotFoundException($"No existe la orden con folio {folio}.");

        if (order.Status != ServiceStatus.Entregado)
        {
            throw new InvalidOperationException(
                "La orden debe estar en estado Entregado antes de finalizarla.");
        }

        order.Status = ServiceStatus.Terminado;
        order.History.Add(new StatusChange
        {
            Status = ServiceStatus.Terminado,
            ChangedAt = DateTime.Now,
            Note = "Orden finalizada automáticamente después de imprimir el ticket doble."
        });

        _repository.SaveAll(orders);
        _exporter?.ExportAll(orders);
        return order;
    }

    private static string GenerateFolio(IReadOnlyCollection<ServiceOrder> orders, DateTime now)
    {
        var prefix = $"KS-{now:yyyyMMdd}-";
        var max = orders
            .Where(order => order.Folio.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Select(order =>
            {
                var suffix = order.Folio[prefix.Length..];
                return int.TryParse(suffix, out var value) ? value : 0;
            })
            .DefaultIfEmpty(0)
            .Max();

        return $"{prefix}{max + 1:000}";
    }

    private static string NormalizeRequired(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"El campo {fieldName} es obligatorio.", fieldName);
        }

        return value.Trim();
    }

    private static string Upper(string value) => value.ToUpperInvariant();
}
