namespace KasaServiceTracker.Models;

public sealed class ServiceOrder
{
    public string Folio { get; init; } = string.Empty;
    public Vehicle Vehicle { get; init; } = new();
    public string ServiceDescription { get; set; } = string.Empty;
    public decimal EstimatedCost { get; set; }
    public DateTime CreatedAt { get; init; }
    public ServiceStatus Status { get; set; }
    public List<StatusChange> History { get; init; } = new();
}
