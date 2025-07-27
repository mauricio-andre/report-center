using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using ReportCenter.Common.Consts;

namespace ReportCenter.App.RestServer.Authentication;

public class AuthenticationHandler : AuthenticationHandler<AuthenticationOptions>
{

    public AuthenticationHandler(
        IOptionsMonitor<AuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : base(options, logger, encoder)
    {
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        try
        {
            var result = await Context.AuthenticateAsync(Options.JwtTokenScheme);
            if (result.None) return result;
            if (!result.Succeeded) return result;

            if (result.Principal == null)
                return AuthenticateResult.NoResult();

            var principal = new ClaimsPrincipal();
            principal.AddIdentity(new ClaimsIdentity(
                result.Principal.Claims,
                AuthenticationDefaults.IdentityType,
                ClaimTypes.NameIdentifier,
                ClaimTypes.Role));

            return AuthenticateResult.Success(new AuthenticationTicket(
                principal,
                null,
                AuthenticationDefaults.AuthenticationScheme
            ));
        }
        catch (Exception ex)
        {
            return AuthenticateResult.Fail(ex);
        }
    }
}
