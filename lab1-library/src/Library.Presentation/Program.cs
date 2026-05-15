using Library.Application.Abstractions;
using Library.Application.Services;
using Library.Domain.Interfaces;
using Library.Infrastructure.Persistence;
using Library.Presentation.ErrorHandling;

namespace Library.Presentation;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        ConfigureServices(builder.Services);

        var app = builder.Build();
        app.MapGet("/", () => "Library API — Lab 1");
        app.MapControllers();
        app.Run();
    }

    public static void ConfigureServices(IServiceCollection services)
    {
        // In-memory repositories must be singletons so state survives across requests.
        services.AddSingleton<IUserRepository, InMemoryUserRepository>();
        services.AddSingleton<IBookRepository, InMemoryBookRepository>();
        services.AddSingleton<IBookRequestRepository, InMemoryBookRequestRepository>();
        services.AddSingleton<IClock, SystemClock>();

        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IBookService, BookService>();
        services.AddScoped<IBookRequestService, BookRequestService>();

        services.AddControllers(options =>
        {
            options.Filters.Add<DomainExceptionFilter>();
        });
    }
}
