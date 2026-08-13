namespace KasaServiceTracker.Models;

public sealed class StatusChange
{
    public ServiceStatus Status { get; init; }
    public DateTime ChangedAt { get; init; }
    public string Note { get; init; } = string.Empty;
}
