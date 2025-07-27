using System.Security.Claims;
using ReportCenter.Core.Identity.Interfaces;

namespace ReportCenter.Core.Identity.Services;

public class CurrentIdentity : ICurrentIdentity
{
    private ClaimsPrincipal? _principal;

    public void SetCurrentIdentity(ClaimsPrincipal? principal) => _principal = principal;

    public string? GetNameIdentifier()
    {
        var nameIdentifier = _principal?.Identities
            .FirstOrDefault()
            ?.FindFirst(claim => claim.Type == ClaimTypes.NameIdentifier)
            ?.Value;

        return nameIdentifier;
    }
}
