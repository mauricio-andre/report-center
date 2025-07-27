using System.Diagnostics;
using ReportCenter.Core.Identity.Interfaces;

namespace ReportCenter.App.RestServer.Middlewares;

public class IdentityMiddleware
{
    private readonly RequestDelegate _next;

    public IdentityMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, ICurrentIdentity currentIdentity)
    {
        currentIdentity.SetCurrentIdentity(context.User);
        var nameIdentifier = currentIdentity.GetNameIdentifier();
        if (!string.IsNullOrEmpty(nameIdentifier))
            Activity.Current?.AddTag("appUser", nameIdentifier);

        await _next(context);
    }
}
