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
                configuration.GetConnectionString("DefaultConnection"),
                sqlOptions =>
                {
                  // Set Split Query as global default for all queries (Has pros and cons)
                  sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                })
                // ── Lazy Loading ───────────────────────────────────────
                // UseLazyLoadingProxies() wraps every entity in a proxy
                // Navigation properties are loaded automatically when accessed
                // Requires: virtual keyword on ALL navigation properties
                .UseLazyLoadingProxies()
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
            services.AddScoped<ITaskService, TaskService>();
            // Every new service gets registered here — Program.cs stays clean
            return services;
    }
    }
}