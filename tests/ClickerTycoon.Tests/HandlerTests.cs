using ClickerTycoon.Application.Features.GameStateQuery;
using ClickerTycoon.Application.Features.PerformClick;
using ClickerTycoon.Application.Features.Players;
using ClickerTycoon.Application.Features.PurchaseUpgrade;
using ClickerTycoon.Application.Features.ResetProgress;
using ClickerTycoon.Domain.Exceptions;
using ClickerTycoon.Tests.Fakes;
using Xunit;

namespace ClickerTycoon.Tests;

public class HandlerTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task CreatePlayer_ThenGetState_ReturnsFreshStageOne()
    {
        var repo = new InMemoryGameStateRepository();
        var config = TestGameConfigFactory.CreateConfig();

        var createHandler = new CreatePlayerCommandHandler(repo);
        var playerId = await createHandler.HandleAsync(new CreatePlayerCommand());

        var queryHandler = new GetGameStateQueryHandler(repo, new StaticGameConfigProvider(config), new FixedClock(Now));
        var state = await queryHandler.HandleAsync(new GetGameStateQuery(playerId));

        Assert.Equal(1, state.CurrentStage);
        Assert.Equal(0, state.Resource);
    }

    [Fact]
    public async Task GetGameState_ForUnknownPlayer_ThrowsPlayerNotFound()
    {
        var repo = new InMemoryGameStateRepository();
        var config = TestGameConfigFactory.CreateConfig();
        var handler = new GetGameStateQueryHandler(repo, new StaticGameConfigProvider(config), new FixedClock(Now));

        await Assert.ThrowsAsync<PlayerNotFoundException>(() =>
            handler.HandleAsync(new GetGameStateQuery(Guid.NewGuid())));
    }

    [Fact]
    public async Task PerformClick_IncreasesResourceAndReturnsUpdatedState()
    {
        var repo = new InMemoryGameStateRepository();
        var config = TestGameConfigFactory.CreateConfig();
        var createHandler = new CreatePlayerCommandHandler(repo);
        var playerId = await createHandler.HandleAsync(new CreatePlayerCommand());

        var clickHandler = new PerformClickCommandHandler(
            repo, new StaticGameConfigProvider(config), new FixedClock(Now), new FixedRandomProvider(0.99));

        var result = await clickHandler.HandleAsync(new PerformClickCommand(playerId));

        Assert.Equal(1m, result.AmountGained);
        Assert.Equal(1m, result.NewResource);
        Assert.False(result.IsCritical);
    }

    [Fact]
    public async Task PurchaseUpgrade_WithoutEnoughResource_ThrowsAndDoesNotChangeState()
    {
        var repo = new InMemoryGameStateRepository();
        var config = TestGameConfigFactory.CreateConfig();
        var createHandler = new CreatePlayerCommandHandler(repo);
        var playerId = await createHandler.HandleAsync(new CreatePlayerCommand());

        var purchaseHandler = new PurchaseUpgradeCommandHandler(
            repo, new StaticGameConfigProvider(config), new FixedClock(Now));

        await Assert.ThrowsAsync<InsufficientResourceException>(() =>
            purchaseHandler.HandleAsync(new PurchaseUpgradeCommand(playerId, "click-percent")));
    }

    [Fact]
    public async Task ResetProgress_AfterClicksAndPurchases_ReturnsFreshState()
    {
        var repo = new InMemoryGameStateRepository();
        var config = TestGameConfigFactory.CreateConfig();
        var staticConfig = new StaticGameConfigProvider(config);
        var clock = new FixedClock(Now);

        var createHandler = new CreatePlayerCommandHandler(repo);
        var playerId = await createHandler.HandleAsync(new CreatePlayerCommand());

        var clickHandler = new PerformClickCommandHandler(repo, staticConfig, clock, new FixedRandomProvider(0.99));
        await clickHandler.HandleAsync(new PerformClickCommand(playerId));
        await clickHandler.HandleAsync(new PerformClickCommand(playerId));

        var resetHandler = new ResetProgressCommandHandler(repo, staticConfig, clock);
        var state = await resetHandler.HandleAsync(new ResetProgressCommand(playerId));

        Assert.Equal(0, state.Resource);
        Assert.Equal(0, state.TotalClicks);
        Assert.Equal(1, state.CurrentStage);
        Assert.Empty(state.Upgrades);
    }

    [Fact]
    public async Task ResetProgress_ForUnknownPlayer_ThrowsPlayerNotFound()
    {
        var repo = new InMemoryGameStateRepository();
        var config = TestGameConfigFactory.CreateConfig();
        var resetHandler = new ResetProgressCommandHandler(repo, new StaticGameConfigProvider(config), new FixedClock(Now));

        await Assert.ThrowsAsync<PlayerNotFoundException>(() =>
            resetHandler.HandleAsync(new ResetProgressCommand(Guid.NewGuid())));
    }
}
