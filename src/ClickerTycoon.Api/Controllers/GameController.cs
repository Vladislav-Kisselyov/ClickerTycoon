using System.Threading;
using ClickerTycoon.Application.Common.Cqrs;
using ClickerTycoon.Application.Features.GameStateQuery;
using ClickerTycoon.Application.Features.Monetization;
using ClickerTycoon.Application.Features.PerformClick;
using ClickerTycoon.Application.Features.PurchaseUpgrade;
using ClickerTycoon.Application.Features.ResetProgress;
using Microsoft.AspNetCore.Mvc;

namespace ClickerTycoon.Api.Controllers;

[ApiController]
[Route("api/game/{playerId:guid}")]
public class GameController : ControllerBase
{
    private readonly IDispatcher _dispatcher;

    public GameController(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    [HttpGet]
    public async Task<IActionResult> GetState(Guid playerId, CancellationToken ct)
    {
        var state = await _dispatcher.SendAsync(new GetGameStateQuery(playerId), ct);
        return Ok(state);
    }

    /// <summary>The single main action of the game - "погладить кота".</summary>
    [HttpPost("click")]
    public async Task<IActionResult> Click(Guid playerId, CancellationToken ct)
    {
        var result = await _dispatcher.SendAsync(new PerformClickCommand(playerId), ct);
        return Ok(result);
    }

    [HttpPost("upgrades/{upgradeId}")]
    public async Task<IActionResult> PurchaseUpgrade(Guid playerId, string upgradeId, CancellationToken ct)
    {
        var result = await _dispatcher.SendAsync(new PurchaseUpgradeCommand(playerId, upgradeId), ct);
        return Ok(result);
    }

    [HttpPost("monetization/{offerId}")]
    public async Task<IActionResult> ActivateMonetization(Guid playerId, string offerId, CancellationToken ct)
    {
        var result = await _dispatcher.SendAsync(new ActivateMonetizationOfferCommand(playerId, offerId), ct);
        return Ok(result);
    }

    /// <summary>Wipes the save for this player back to a brand-new game (same player id).</summary>
    [HttpPost("reset")]
    public async Task<IActionResult> ResetProgress(Guid playerId, CancellationToken ct)
    {
        var state = await _dispatcher.SendAsync(new ResetProgressCommand(playerId), ct);
        return Ok(state);
    }
}
