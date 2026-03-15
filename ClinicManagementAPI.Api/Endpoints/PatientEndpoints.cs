using ClinicManagementAPI.Core.DTOs;
using ClinicManagementAPI.Core.DTOs.Patients;
using ClinicManagementAPI.Core.Interfaces;

namespace ClinicManagementAPI.Api.Endpoints;

public static class PatientEndpoints
{
    public static void MapPatientEndpoints(this WebApplication app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/patients")
            .WithTags("Patients")
            .RequireAuthorization();

        // GET /api/patients — Admin + Receptionist
        group.MapGet("/", GetAll)
            .RequireAuthorization(policy => policy.RequireRole("Admin", "Receptionist"));

        // GET /api/patients/{id} — Admin + Receptionist
        group.MapGet("/{id:int}", GetById)
            .RequireAuthorization(policy => policy.RequireRole("Admin", "Receptionist"));

        // POST /api/patients — Admin + Receptionist
        group.MapPost("/", Create)
            .RequireAuthorization(policy => policy.RequireRole("Admin", "Receptionist"));

        // PUT /api/patients/{id} — Admin + Receptionist
        group.MapPut("/{id:int}", Update)
            .RequireAuthorization(policy => policy.RequireRole("Admin", "Receptionist"));

        // DELETE /api/patients/{id} — Admin ONLY
        group.MapDelete("/{id:int}", Delete)
            .RequireAuthorization(policy => policy.RequireRole("Admin"));
    }

    private static async Task<IResult> GetAll(
        [AsParameters] PaginationRequest pagination,
        IPatientService patientService,
        CancellationToken cancellationToken)
    {
        var result = await patientService.GetAllAsync(pagination, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error!, statusCode: result.StatusCode);
    }

    private static async Task<IResult> GetById(
        int id, IPatientService patientService, CancellationToken cancellationToken)
    {
        var result = await patientService.GetByIdAsync(id, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error!, statusCode: result.StatusCode);
    }

    private static async Task<IResult> Create(
        CreatePatientRequest request,
        IPatientService patientService,
        CancellationToken cancellationToken)
    {
        var result = await patientService.CreateAsync(request, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/patients/{result.Value!.Id}", result.Value)
            : Results.Problem(result.Error!, statusCode: result.StatusCode);
    }

    private static async Task<IResult> Update(
        int id,
        UpdatePatientRequest request,
        IPatientService patientService,
        CancellationToken cancellationToken)
    {
        var result = await patientService.UpdateAsync(id, request, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error!, statusCode: result.StatusCode);
    }

    private static async Task<IResult> Delete(
        int id, IPatientService patientService, CancellationToken cancellationToken)
    {
        var result = await patientService.DeleteAsync(id, cancellationToken);

        return result.IsSuccess
            ? Results.NoContent()
            : Results.Problem(result.Error!, statusCode: result.StatusCode);
    }
}
