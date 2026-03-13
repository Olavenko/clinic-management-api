using ClinicManagementAPI.Core.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Microsoft.AspNetCore.TestHost;

namespace ClinicManagementAPI.Tests.Integration;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Remove all existing DbContext options.
            services.RemoveAll(typeof(DbContextOptions<AppDbContext>));
            services.RemoveAll(typeof(DbContextOptions));
            services.RemoveAll(typeof(AppDbContext));

            var dbConnectionDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(System.Data.Common.DbConnection));

            if (dbConnectionDescriptor != null)
            {
                services.Remove(dbConnectionDescriptor);
            }

            // Create explicit options and add as Singleton to bypass any AddDbContext logic
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase("InMemoryDbForTesting")
                .Options;

            services.AddSingleton(options);
            services.AddScoped<AppDbContext>();
        });
    }
}
