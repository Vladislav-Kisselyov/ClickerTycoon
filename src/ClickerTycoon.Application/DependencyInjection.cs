using ClickerTycoon.Application.Common.Cqrs;
using ClickerTycoon.Application.Common.Dto;
using ClickerTycoon.Application.Features.GameStateQuery;
using ClickerTycoon.Application.Features.Monetization;
using ClickerTycoon.Application.Features.PerformClick;
using ClickerTycoon.Application.Features.Players;
using ClickerTycoon.Application.Features.PurchaseUpgrade;
using ClickerTycoon.Application.Features.ResetProgress;
using Microsoft.Extensions.DependencyInjection;

namespace ClickerTycoon.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IDispatcher, Dispatcher>();

        services.AddScoped<ICommandHandler<CreatePlayerCommand, Guid>, CreatePlayerCommandHandler>();
        services.AddScoped<IQueryHandler<GetGameStateQuery, GameStateDto>, GetGameStateQueryHandler>();
        services.AddScoped<ICommandHandler<PerformClickCommand, ClickResultDto>, PerformClickCommandHandler>();
        services.AddScoped<ICommandHandler<PurchaseUpgradeCommand, PurchaseUpgradeResultDto>, PurchaseUpgradeCommandHandler>();
        services.AddScoped<ICommandHandler<ActivateMonetizationOfferCommand, MonetizationResultDto>, ActivateMonetizationOfferCommandHandler>();
        services.AddScoped<ICommandHandler<ResetProgressCommand, GameStateDto>, ResetProgressCommandHandler>();

        return services;
    }
}
