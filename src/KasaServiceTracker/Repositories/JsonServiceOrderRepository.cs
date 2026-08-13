using System.Text.Json;
using System.Text.Json.Serialization;
using KasaServiceTracker.Models;

namespace KasaServiceTracker.Repositories;

public sealed class JsonServiceOrderRepository : IServiceOrderRepository
{
    private readonly string _filePath;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public JsonServiceOrderRepository(string filePath)
    {
        _filePath = filePath;
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    public IReadOnlyList<ServiceOrder> GetAll()
    {
        if (!File.Exists(_filePath))
        {
            return Array.Empty<ServiceOrder>();
        }

        var json = File.ReadAllText(_filePath);
        if (string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<ServiceOrder>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<ServiceOrder>>(json, _jsonOptions)
                   ?? new List<ServiceOrder>();
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException(
                $"No fue posible leer el archivo de datos '{_filePath}'.", ex);
        }
    }

    public ServiceOrder? GetByFolio(string folio) =>
        GetAll().FirstOrDefault(order =>
            order.Folio.Equals(folio.Trim(), StringComparison.OrdinalIgnoreCase));

    public void SaveAll(IEnumerable<ServiceOrder> orders)
    {
        var json = JsonSerializer.Serialize(orders, _jsonOptions);
        File.WriteAllText(_filePath, json);
    }
}
