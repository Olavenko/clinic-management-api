using ClinicManagementAPI.Core.DTOs;
using ClinicManagementAPI.Core.DTOs.Doctors;
using ClinicManagementAPI.Core.Interfaces;

namespace ClinicManagementAPI.Api.Endpoints;

public static class DoctorEndpoints
{
    public static void MapDoctorEndpoints(this WebApplication app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/doctors")
            .WithTags("Doctors");

        // GET /api/doctors — Public (anyone can see doctors list)
        group.MapGet("/", GetAll);

        // GET /api/doctors/available — Public (patients see available doctors before booking)
        group.MapGet("/available", GetAvailable);

        // GET /api/doctors/{id} — Public
        group.MapGet("/{id:int}", GetById);

        // POST /api/doctors — Admin ONLY
        group.MapPost("/", Create)
            .RequireAuthorization(policy => policy.RequireRole("Admin"));

        // PUT /api/doctors/{id} — Admin ONLY
        group.MapPut("/{id:int}", Update)
            .RequireAuthorization(policy => policy.RequireRole("Admin"));

        // DELETE /api/doctors/{id} — Admin ONLY (Soft Delete)
        group.MapDelete("/{id:int}", Delete)
            .RequireAuthorization(policy => policy.RequireRole("Admin"));
    }

    private static async Task<IResult> GetAll(
        [AsParameters] PaginationRequest pagination,
        IDoctorService doctorService,
        CancellationToken cancellationToken)
    {
        var result = await doctorService.GetAllAsync(pagination, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error!, statusCode: result.StatusCode);
    }

    private static async Task<IResult> GetAvailable(
        [AsParameters] PaginationRequest pagination,
        IDoctorService doctorService,
        CancellationToken cancellationToken)
    {
        var result = await doctorService.GetAvailableAsync(pagination, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error!, statusCode: result.StatusCode);
    }

    private static async Task<IResult> GetById(
        int id, IDoctorService doctorService, CancellationToken cancellationToken)
    {
        var result = await doctorService.GetByIdAsync(id, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error!, statusCode: result.StatusCode);
    }

    private static async Task<IResult> Create(
        CreateDoctorRequest request,
        IDoctorService doctorService,
        CancellationToken cancellationToken)
    {
        var result = await doctorService.CreateAsync(request, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/doctors/{result.Value!.Id}", result.Value)
            : Results.Problem(result.Error!, statusCode: result.StatusCode);
    }

    private static async Task<IResult> Update(
        int id,
        UpdateDoctorRequest request,
        IDoctorService doctorService,
        CancellationToken cancellationToken)
    {
        var result = await doctorService.UpdateAsync(id, request, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error!, statusCode: result.StatusCode);
    }

    private static async Task<IResult> Delete(
        int id, IDoctorService doctorService, CancellationToken cancellationToken)
    {
        var result = await doctorService.DeleteAsync(id, cancellationToken);

        return result.IsSuccess
            ? Results.NoContent()
            : Results.Problem(result.Error!, statusCode: result.StatusCode);
    }
}
