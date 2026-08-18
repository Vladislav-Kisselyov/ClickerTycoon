using System.Threading;
using ClickerTycoon.Application.Common;
using ClickerTycoon.Application.Common.Cqrs;
using ClickerTycoon.Application.Common.Dto;
using ClickerTycoon.Application.Common.Interfaces;
using ClickerTycoon.Domain.Exceptions;
using ClickerTycoon.Domain.Services;

namespace ClickerTycoon.Application.Features.Monetization;

public record ActivateMonetizationOfferCommand(Guid PlayerId, string OfferId) : ICommand<MonetizationResultDto>;

public class ActivateMonetizationOfferCommandHandler : ICommandHandler<ActivateMonetizationOfferCommand, MonetizationResultDto>
{
    private readonly IGameStateRepository _repository;
    private readonly IGameConfigProvider _configProvider;
    private readonly IClock _clock;

    public ActivateMonetizationOfferCommandHandler(IGameStateRepository repository, IGameConfigProvider configProvider, IClock clock)
    {
        _repository = repository;
        _configProvider = configProvider;
        _clock = clock;
    }

    public async Task<MonetizationResultDto> HandleAsync(ActivateMonetizationOfferCommand command, CancellationToken ct = default)
    {
        var state = await _repository.FindAsync(command.PlayerId, ct)
                    ?? throw new PlayerNotFoundException(command.PlayerId);

        var config = _configProvider.GetConfig();
        var now = _clock.UtcNow;

        GameCalculationEngine.ApplyAutomationTick(state, config, now);

        string message;
        switch (command.OfferId)
        {
            case "ad-boost":
                GameCalculationEngine.ActivateAdBoost(state, config, now);
                message = $"Реклама просмотрена (симуляция). Доход x{config.Monetization.AdBoostMultiplier:0.#} на {config.Monetization.AdBoostDurationSeconds} секунд.";
                break;

            case "premium-camera":
                if (state.PremiumCameraPurchased)
                    throw new MonetizationOfferAlreadyPurchasedException(command.OfferId);
                GameCalculationEngine.PurchasePremiumCamera(state);
                message = "Premium Camera приобретена (симуляция оплаты). Постоянный бонус к клику активирован.";
                break;

            case "starter-pack":
                if (state.StarterPackPurchased)
                    throw new MonetizationOfferAlreadyPurchasedException(command.OfferId);
                GameCalculationEngine.PurchaseStarterPack(state, config, now);
                message = "Мемный стартовый набор приобретён (симуляция оплаты). Ресурс и временный буст начислены.";
                break;

            default:
                throw new MonetizationOfferNotFoundException(command.OfferId);
        }

        await _repository.SaveAsync(state, ct);

        var dto = GameStateMapper.ToDto(state, config, now);
        return new MonetizationResultDto(command.OfferId, message, dto);
    }
}
