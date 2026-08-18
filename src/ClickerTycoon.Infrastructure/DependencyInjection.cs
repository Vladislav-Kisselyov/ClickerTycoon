using ClickerTycoon.Application.Common.Interfaces;
using ClickerTycoon.Infrastructure.Configuration;
using ClickerTycoon.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ClickerTycoon.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string sqliteConnectionString,
        string gameConfigFilePath)
    {
        services.AddDbContext<GameDbContext>(options => options.UseSqlite(sqliteConnectionString));

        services.AddScoped<IGameStateRepository, GameStateRepository>();
        services.AddSingleton<IGameConfigProvider>(_ => new JsonGameConfigProvider(gameConfigFilePath));
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IRandomProvider, RandomProvider>();

        return services;
    }
}
