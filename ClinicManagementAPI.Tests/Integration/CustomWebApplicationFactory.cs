using System.Text;
using System.Threading.RateLimiting;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using ClinicManagementAPI.Core.Data;
using ClinicManagementAPI.Core.Models;

namespace ClinicManagementAPI.Tests.Integration;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // === Database Setup ===
            services.RemoveAll(typeof(DbContextOptions<AppDbContext>));
            services.RemoveAll(typeof(DbContextOptions));
            services.RemoveAll(typeof(AppDbContext));

            var dbConnectionDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(System.Data.Common.DbConnection));

            if (dbConnectionDescriptor != null)
            {
                services.Remove(dbConnectionDescriptor);
            }

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase($"InMemoryDbForTesting_{Guid.NewGuid()}")
                .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            services.AddSingleton(options);
            services.AddScoped<AppDbContext>();

            // === JWT Settings Override ===
            var testJwtSettings = new JwtSettings
            {
                Key = "ThisIsATestSecretKeyThatIsAtLeast32Characters!",
                Issuer = "TestIssuer",
                Audience = "TestAudience",
                ExpiryMinutes = 60,
                RefreshTokenExpiryDays = 7
            };

            services.RemoveAll(typeof(JwtSettings));
            services.AddSingleton(testJwtSettings);

            services.Configure<JwtBearerOptions>(
                JwtBearerDefaults.AuthenticationScheme,
                options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = testJwtSettings.Issuer,
                        ValidAudience = testJwtSettings.Audience,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(testJwtSettings.Key))
                    };
                });

            // === Disable Rate Limiting for Tests ===
            services.RemoveAll(typeof(IConfigureOptions<RateLimiterOptions>));
            services.AddRateLimiter(options =>
            {
                options.AddPolicy("auth", _ => RateLimitPartition.GetNoLimiter("test"));
            });

            // === Seed Roles ===
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            db.Database.EnsureCreated();

            foreach (var role in new[] { AppRoles.Admin, AppRoles.Doctor, AppRoles.Receptionist, AppRoles.Patient })
            {
                if (!roleManager.RoleExistsAsync(role).GetAwaiter().GetResult())
                {
                    roleManager.CreateAsync(new IdentityRole(role)).GetAwaiter().GetResult();
                }
            }
        });
    }
}
