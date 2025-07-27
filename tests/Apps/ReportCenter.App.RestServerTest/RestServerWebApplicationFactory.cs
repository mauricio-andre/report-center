using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using ReportCenter.App.RestServer;
using ReportCenter.App.RestServer.Authentication;
using ReportCenter.Common.Consts;
using ReportCenter.Commons.Test.Database;
using ReportCenter.Commons.Test.Helpers;
using ReportCenter.Core.Data;

namespace ReportCenter.App.RestServerTest;

public class RestServerWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Adiciona a configuração em memória
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(JwtHelper.Options);
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:CoreDbContext", "Host" }
            });
        });

        return base.CreateHost(builder);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remover configurações para reincluir com configurações de teste
            var authDescriptor = services
                .Where(service =>
                    service.ServiceType == typeof(IConfigureOptions<Microsoft.AspNetCore.Authentication.AuthenticationOptions>)
                    || service.ServiceType == typeof(IDbContextFactory<CoreDbContext>)
                    || service.ServiceType == typeof(CoreDbContext)
                )
                .ToList();

            foreach (var item in authDescriptor)
                services.Remove(item);

            // Adicionar autenticação customizada para os testes
            services
                .AddAuthentication(schemes =>
                {
                    schemes.DefaultAuthenticateScheme = AuthenticationDefaults.AuthenticationScheme;
                    schemes.DefaultChallengeScheme = AuthenticationDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.Authority = JwtHelper.Options["Authentication:Bearer:Authority"];
                    options.Audience = JwtHelper.Options["Authentication:Bearer:Audience"];
                    options.RequireHttpsMetadata = false;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = JwtHelper.Options["Authentication:Bearer:Authority"],
                        ValidateAudience = true,
                        ValidAudience = JwtHelper.Options["Authentication:Bearer:Audience"],
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("chave-secreta-mock-chave-secreta-mock-chave-secreta-mock")),
                        ClockSkew = TimeSpan.Zero
                    };
                })
                .AddScheme<AuthenticationOptions, AuthenticationHandler>(
                    AuthenticationDefaults.AuthenticationScheme,
                    AuthenticationDefaults.DisplayName,
                    null);

            // Adiciona fabricas para o banco de dados
            services
                .AddScoped<CoreDbContext, SqliteCoreDbContext>()
                .AddDbContextFactory<CoreDbContext, SqliteCoreDbContextFactory>(
                    options => { },
                    ServiceLifetime.Scoped);

            // Cria estrutura para o banco de dados
            var sqliteConnectionPull = new SqliteConnectionPull();
            services.AddSingleton(_ => sqliteConnectionPull);
            using (var scope = services.BuildServiceProvider().CreateScope())
            {
                var coreDbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<CoreDbContext>>();
                using var coreContext = coreDbContextFactory.CreateDbContext();
                coreContext.Database.EnsureCreated();
            }

            // Cria Mock dos providers
            // services.AddSingleton(_ => Substitute.For<Interface>());
        });
    }
}
