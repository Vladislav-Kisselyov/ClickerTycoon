using System.Threading;
using ClickerTycoon.Application.Common.Cqrs;
using ClickerTycoon.Application.Features.Players;
using Microsoft.AspNetCore.Mvc;

namespace ClickerTycoon.Api.Controllers;

[ApiController]
[Route("api/players")]
public class PlayersController : ControllerBase
{
    private readonly IDispatcher _dispatcher;

    public PlayersController(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    /// <summary>
    /// Creates a brand-new save. The frontend calls this once and remembers the
    /// returned id (e.g. in localStorage) purely as a pointer - the actual game
    /// state always lives server-side in the database.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<object>> CreatePlayer(CancellationToken ct)
    {
        var playerId = await _dispatcher.SendAsync(new CreatePlayerCommand(), ct);
        return Ok(new { playerId });
    }
}
