using System.Text.Json;
using ClickerTycoon.Domain.Exceptions;

namespace ClickerTycoon.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (PlayerNotFoundException ex)
        {
            await WriteError(context, StatusCodes.Status404NotFound, ex.Message, ex.GetType().Name);
        }
        catch (UpgradeNotFoundException ex)
        {
            await WriteError(context, StatusCodes.Status404NotFound, ex.Message, ex.GetType().Name);
        }
        catch (MonetizationOfferNotFoundException ex)
        {
            await WriteError(context, StatusCodes.Status404NotFound, ex.Message, ex.GetType().Name);
        }
        catch (GameDomainException ex)
        {
            // All other expected business-rule violations (insufficient resource,
            // upgrade locked, already purchased, invalid state, ...) => 400.
            await WriteError(context, StatusCodes.Status400BadRequest, ex.Message, ex.GetType().Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Необработанная ошибка при обработке запроса {Path}", context.Request.Path);
            await WriteError(context, StatusCodes.Status500InternalServerError,
                "Произошла внутренняя ошибка сервера. Попробуйте повторить действие позже.", "InternalServerError");
        }
    }

    private static async Task WriteError(HttpContext context, int statusCode, string message, string code)
    {
        if (context.Response.HasStarted) return;

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var payload = JsonSerializer.Serialize(new { error = message, code });
        await context.Response.WriteAsync(payload);
    }
}
