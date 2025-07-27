using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace ReportCenter.App.RestServer.Authentication;

public class AuthenticationOptions : AuthenticationSchemeOptions
{
    public string JwtTokenScheme { get; set; } = JwtBearerDefaults.AuthenticationScheme;
}
