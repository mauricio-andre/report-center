using System.Security.Claims;
using ReportCenter.CustomConsoleFormatter.Interfaces;

namespace ReportCenter.App.GrpcServer.Loggers;

public class LoggerPropertiesService : ILoggerPropertiesService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public LoggerPropertiesService(IHttpContextAccessor httpContextAccessor) => _httpContextAccessor = httpContextAccessor;

    public string GetAppUser()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated ?? false)
        {
            var localId = user.Identities
                .FirstOrDefault()
                ?.FindFirst(claim => claim.Type == ClaimTypes.NameIdentifier)
                ?.Value;

            return localId ?? user.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.NameIdentifier)?.Value!;
        }

        return "Unknown";
    }

    public KeyValuePair<string, object?>[] DefaultPropertyList() => [];

    public KeyValuePair<string, object?>[] ScopeObjectStructuring(object value) => [];
}
