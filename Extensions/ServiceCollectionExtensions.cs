using Microsoft.EntityFrameworkCore;
using TaskManagerAPI.Data;
using TaskManagerAPI.Data.Interceptors;
using TaskManagerAPI.MappingProfiles;
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
            services.AddScoped<AuditInterceptor>();
            
            services.AddDbContext<AppDbContext>((ServiceProvider, options) =>
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
                .AddInterceptors(
                    ServiceProvider.GetRequiredService<AuditInterceptor>() 
                )
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

        // ── AutoMapper ─────────────────────────────────────────────────────────
        // Scans the assembly for all Profile classes
        // and registers them automatically
        public static IServiceCollection AddAutoMapperProfiles(
            this IServiceCollection services)
        {
            services.AddAutoMapper(cfg => { }, typeof(UserMappingProfile).Assembly);
            return services;
        }
    }

    
}