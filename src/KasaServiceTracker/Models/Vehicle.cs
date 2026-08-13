namespace KasaServiceTracker.Models;

public sealed class Vehicle
{
    public string Plate { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }

    public override string ToString() => $"{Brand} {Model} {Year} | Placas: {Plate}";
}
