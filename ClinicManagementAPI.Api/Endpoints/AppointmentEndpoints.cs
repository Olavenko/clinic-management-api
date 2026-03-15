using ClinicManagementAPI.Core.DTOs;
using ClinicManagementAPI.Core.DTOs.Appointments;
using ClinicManagementAPI.Core.Interfaces;

namespace ClinicManagementAPI.Api.Endpoints;

public static class AppointmentEndpoints
{
    public static void MapAppointmentEndpoints(this WebApplication app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/appointments")
            .WithTags("Appointments");

        // GET /api/appointments — Admin + Receptionist
        group.MapGet("/", GetAll)
            .RequireAuthorization(policy => policy.RequireRole("Admin", "Receptionist"));

        // GET /api/appointments/{id} — Admin + Receptionist
        group.MapGet("/{id:int}", GetById)
            .RequireAuthorization(policy => policy.RequireRole("Admin", "Receptionist"));

        // GET /api/appointments/patient/{patientId} — Admin + Receptionist
        group.MapGet("/patient/{patientId:int}", GetByPatient)
            .RequireAuthorization(policy => policy.RequireRole("Admin", "Receptionist"));

        // GET /api/appointments/doctor/{doctorId} — Admin + Receptionist
        group.MapGet("/doctor/{doctorId:int}", GetByDoctor)
            .RequireAuthorization(policy => policy.RequireRole("Admin", "Receptionist"));

        // POST /api/appointments — Admin + Receptionist
        group.MapPost("/", Create)
            .RequireAuthorization(policy => policy.RequireRole("Admin", "Receptionist"));

        // PUT /api/appointments/{id} — Admin + Receptionist
        group.MapPut("/{id:int}", Update)
            .RequireAuthorization(policy => policy.RequireRole("Admin", "Receptionist"));

        // PATCH /api/appointments/{id}/status — Admin + Receptionist
        group.MapPatch("/{id:int}/status", UpdateStatus)
            .RequireAuthorization(policy => policy.RequireRole("Admin", "Receptionist"));

        // DELETE /api/appointments/{id} — Admin ONLY
        group.MapDelete("/{id:int}", Delete)
            .RequireAuthorization(policy => policy.RequireRole("Admin"));
    }

    private static async Task<IResult> GetAll(
        [AsParameters] AppointmentFilterRequest filter,
        IAppointmentService appointmentService,
        CancellationToken cancellationToken)
    {
        var result = await appointmentService.GetAllAsync(filter, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error!, statusCode: result.StatusCode);
    }

    private static async Task<IResult> GetById(
        int id, IAppointmentService appointmentService, CancellationToken cancellationToken)
    {
        var result = await appointmentService.GetByIdAsync(id, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error!, statusCode: result.StatusCode);
    }

    private static async Task<IResult> GetByPatient(
        int patientId,
        [AsParameters] PaginationRequest pagination,
        IAppointmentService appointmentService,
        CancellationToken cancellationToken)
    {
        var result = await appointmentService.GetByPatientAsync(patientId, pagination, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error!, statusCode: result.StatusCode);
    }

    private static async Task<IResult> GetByDoctor(
        int doctorId,
        [AsParameters] PaginationRequest pagination,
        IAppointmentService appointmentService,
        CancellationToken cancellationToken)
    {
        var result = await appointmentService.GetByDoctorAsync(doctorId, pagination, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error!, statusCode: result.StatusCode);
    }

    private static async Task<IResult> Create(
        CreateAppointmentRequest request,
        IAppointmentService appointmentService,
        CancellationToken cancellationToken)
    {
        var result = await appointmentService.CreateAsync(request, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/appointments/{result.Value!.Id}", result.Value)
            : Results.Problem(result.Error!, statusCode: result.StatusCode);
    }

    private static async Task<IResult> Update(
        int id,
        UpdateAppointmentRequest request,
        IAppointmentService appointmentService,
        CancellationToken cancellationToken)
    {
        var result = await appointmentService.UpdateAsync(id, request, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error!, statusCode: result.StatusCode);
    }

    private static async Task<IResult> UpdateStatus(
        int id,
        UpdateAppointmentStatusRequest request,
        IAppointmentService appointmentService,
        CancellationToken cancellationToken)
    {
        var result = await appointmentService.UpdateStatusAsync(id, request, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error!, statusCode: result.StatusCode);
    }

    private static async Task<IResult> Delete(
        int id, IAppointmentService appointmentService, CancellationToken cancellationToken)
    {
        var result = await appointmentService.DeleteAsync(id, cancellationToken);

        return result.IsSuccess
            ? Results.NoContent()
            : Results.Problem(result.Error!, statusCode: result.StatusCode);
    }
}
