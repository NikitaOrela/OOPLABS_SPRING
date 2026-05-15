using CarRental.Application.Abstractions;
using CarRental.Application.Services;
using CarRental.Domain.Interfaces;
using CarRental.Infrastructure.Persistence;
using CarRental.Presentation.ErrorHandling;

namespace CarRental.Presentation;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        ConfigureServices(builder.Services);

        var app = builder.Build();
        app.MapGet("/", () => "Car Rental API — Lab 2");
        app.MapControllers();
        app.Run();
    }

    public static void ConfigureServices(IServiceCollection services)
    {
        // In-memory repositories must be singletons so state survives across requests.
        services.AddSingleton<IUserRepository, InMemoryUserRepository>();
        services.AddSingleton<ICarRepository, InMemoryCarRepository>();
        services.AddSingleton<IRentalRequestRepository, InMemoryRentalRequestRepository>();
        services.AddSingleton<IClock, SystemClock>();

        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ICarService, CarService>();
        services.AddScoped<IRentalRequestService, RentalRequestService>();

        services.AddControllers(options =>
        {
            options.Filters.Add<DomainExceptionFilter>();
        });
    }
}
