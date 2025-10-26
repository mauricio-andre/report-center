using CqrsProject.App.GrpcServer.Methods.V1.Examples;
using ReportCenter.App.GrpcServer.Authentication;
using ReportCenter.Common.Consts;
using ReportCenter.Common.Diagnostics;
using ReportCenter.CustomStringLocalizer.Extensions;
using ReportCenter.OpenTelemetry.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddSingleton<IHttpContextAccessor, HttpContextAccessor>()
    .AddSingleton(_ => new ReportCenterActivitySource(builder.Configuration.GetValue<string>("ServiceName")!));

// configuration authentication
builder.Services
    .AddAuthentication(schemes =>
    {
        schemes.DefaultAuthenticateScheme = AuthenticationDefaults.AuthenticationScheme;
        schemes.DefaultChallengeScheme = AuthenticationDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration.GetValue<string>("Authentication:Bearer:Authority");
        options.Audience = builder.Configuration.GetValue<string>("Authentication:Bearer:Audience");
        options.TokenValidationParameters.ClockSkew = TimeSpan.Zero;
    })
    .AddScheme<AuthenticationOptions, AuthenticationHandler>(
        AuthenticationDefaults.AuthenticationScheme,
        AuthenticationDefaults.DisplayName,
        null);

builder.Services.AddAuthorization();

// Configure providers
builder.Services.AddCustomStringLocalizerProvider();
builder.AddOpenTelemetryProvider();

builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// Add gRPC Services
if (app.Environment.IsDevelopment())
    app.MapGrpcReflectionService();

app.MapGrpcService<ExamplesGrpcService>();

await app.RunAsync();
