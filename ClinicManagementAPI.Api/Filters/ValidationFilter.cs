using System.ComponentModel.DataAnnotations;

namespace ClinicManagementAPI.Api.Filters;

public class ValidationFilter<T> : IEndpointFilter where T : class
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var model = context.Arguments.OfType<T>().FirstOrDefault();

        if (model is null)
        {
            return Results.Problem(
                title: "Validation Error",
                detail: "Request body is required",
                statusCode: 400);
        }

        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(model);
        bool isValid = Validator.TryValidateObject(model, validationContext, validationResults, true);
        if (!isValid)
        {
            var errors = validationResults
                .ToDictionary(
                    v => v.MemberNames.FirstOrDefault() ?? "Unknown",
                    v => new[] { v.ErrorMessage ?? "Invalid value" });

            return Results.ValidationProblem(errors);
        }

        return await next(context);
    }
}