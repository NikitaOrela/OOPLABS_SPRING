namespace CarRental.Presentation;

// Minimal ASP.NET Core host placeholder.
// Full controllers, DI, EF Core and Mediator wiring will be added in Lab 2.
public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var app = builder.Build();
        app.MapGet("/", () => "Car Rental API skeleton — Lab 2");
        app.Run();
    }
}
