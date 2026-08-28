using Lughat.Engine.Api.Api;

namespace Lughat.Engine.Api.Auth;

/// <summary>
/// Rejects any request that doesn't carry the per-launch bearer token handed to the shell
/// at spawn time — see spec §2 ("Security note") and §9 ("Local API contract").
/// </summary>
public sealed class BearerTokenMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _expectedToken;

    public BearerTokenMiddleware(RequestDelegate next, string expectedToken)
    {
        _next = next;
        _expectedToken = expectedToken;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var header = context.Request.Headers.Authorization.ToString();
        if (header != $"Bearer {_expectedToken}")
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(
                new ErrorResponse("unauthorized", "Missing or invalid bearer token."),
                AppJsonContext.Default.ErrorResponse);
            return;
        }

        await _next(context);
    }
}
