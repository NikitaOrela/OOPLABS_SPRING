using CarRental.Domain.Enums;

namespace CarRental.Presentation.Contracts;

public class CreateCarRequest
{
    public int ManagerId { get; set; }
    public string Vin { get; set; } = string.Empty;
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int PowerHp { get; set; }
    public decimal DailyTariff { get; set; }
}

public class UpdateCarStatusRequest
{
    public int ManagerId { get; set; }
    public CarStatus Status { get; set; }
}

public class CarResponse
{
    public int Id { get; set; }
    public string Vin { get; set; } = string.Empty;
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int PowerHp { get; set; }
    public decimal DailyTariff { get; set; }
    public CarStatus Status { get; set; }
}
