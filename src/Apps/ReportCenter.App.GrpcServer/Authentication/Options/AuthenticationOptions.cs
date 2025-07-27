using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace ReportCenter.App.GrpcServer.Authentication;

public class AuthenticationOptions : AuthenticationSchemeOptions
{
    public string JwtTokenScheme { get; set; } = JwtBearerDefaults.AuthenticationScheme;
}
