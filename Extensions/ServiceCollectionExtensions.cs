using Microsoft.EntityFrameworkCore;
using TaskManagerAPI.Data;
using TaskManagerAPI.Services.Implementations;
using TaskManagerAPI.Services.Interfaces;
using TaskManagerAPI.UnitOfWork;

namespace TaskManagerAPI.Extensions
{
    public static class ServiceCollectionExtensions
    {
        // Databse
        public static IServiceCollection AddDatabase(
            this IServiceCollection services,
            IConfiguration configuration
        )
        {
            services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"))
                   .LogTo(Console.WriteLine, LogLevel.Information));

            return services;
        }

        // Unit of work
        public static IServiceCollection AddUnitOfWork(
            this IServiceCollection services
        )
        {
            services.AddScoped<IUnitOfWork, UnitOfWorkImp>();
            return services;
        }

        // ── Application Services ─────────────────────────────────────
        public static IServiceCollection AddApplicationServices(
            this IServiceCollection services)
        {
            services.AddScoped<IUserService, UserService>();
            // Every new service gets registered here — Program.cs stays clean
            return services;
    }
    }
}