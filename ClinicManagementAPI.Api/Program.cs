using ClinicManagementAPI.Api.Middleware;
using ClinicManagementAPI.Core.Data;
using ClinicManagementAPI.Core.Models;
using ClinicManagementAPI.Core.Services;
using ClinicManagementAPI.Core.Interfaces;
using ClinicManagementAPI.Api.Endpoints;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ClinicDb"))
);

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Register AuthService for DI — any class that needs IAuthService gets AuthService
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>("ClinicDb");

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()
    ?? throw new InvalidOperationException("JWT settings not configured");

// Register JWT settings as Singleton for DI
builder.Services.AddSingleton(jwtSettings);

// Register Authentication service — required for app.UseAuthentication()
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options => options.TokenValidationParameters = new TokenValidationParameters
{
    ValidateIssuer = true,
    ValidateAudience = true,
    ValidateLifetime = true,
    ValidateIssuerSigningKey = true,
    ValidIssuer = jwtSettings.Issuer,
    ValidAudience = jwtSettings.Audience,
    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key))
});

// Register Authorization service — required for app.UseAuthorization()
builder.Services.AddAuthorization();

// Register PatientService for DI — any class that needs IPatientService gets PatientService
builder.Services.AddScoped<IPatientService, PatientService>();

var app = builder.Build();

// Seed roles on startup
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    await DatabaseSeeder.SeedRolesAsync(roleManager);
}

app.UseExceptionHandler();

app.MapHealthChecks("/health");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Authentication and Authorization middleware must be added before endpoint routing
app.UseAuthentication();
app.UseAuthorization();

// Map auth endpoints (register, login, refresh, logout)
app.MapAuthEndpoints();

// Map patient endpoints
app.MapPatientEndpoints();

// Map user endpoints (role management)
app.MapUserEndpoints();

app.Run();
