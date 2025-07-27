using System.Security.Claims;

namespace ReportCenter.Core.Identity.Interfaces;

public interface ICurrentIdentity
{
    public void SetCurrentIdentity(ClaimsPrincipal? principal);
    public string? GetNameIdentifier();
}
