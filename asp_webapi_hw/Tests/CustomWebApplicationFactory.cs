using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace asp_webapi_hw.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            // Перекриваємо конфіг — гарантуємо стабільний секрет для тестів
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"]                      = "test-secret-key-minimum-32-characters!!",
                ["Jwt:Issuer"]                      = "homework_project",
                ["Jwt:Audience"]                    = "homework_project",
                ["Jwt:AccessTokenLifetimeMinutes"]  = "15",
                ["Jwt:RefreshTokenLifetimeDays"]    = "7",
            });
        });
    }
}
