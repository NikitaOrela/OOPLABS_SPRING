using CarRental.Domain.Enums;

namespace CarRental.Domain.Entities;

public class Car
{
    public int Id { get; set; }
    public string Vin { get; set; } = string.Empty;
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int PowerHp { get; set; }
    public decimal DailyTariff { get; set; }
    public CarStatus Status { get; set; } = CarStatus.Available;
}
