namespace Library.Presentation;

// Minimal ASP.NET Core host placeholder.
// Full controllers, DI, EF Core and Mediator wiring will be added in Lab 1.
public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var app = builder.Build();
        app.MapGet("/", () => "Library API skeleton — Lab 1");
        app.Run();
    }
}
