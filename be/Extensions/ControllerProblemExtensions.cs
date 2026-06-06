using System.Diagnostics;
using be.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace be.Extensions;

public static class ControllerProblemExtensions
{
    public static ObjectResult ApiProblem(
        this ControllerBase controller,
        int statusCode,
        string title,
        string type,
        string? detail = null)
    {
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Type = type,
            Detail = detail,
            Instance = controller.HttpContext.Request.Path
        };

        AddTraceId(problem, controller.HttpContext);

        return new ObjectResult(problem)
        {
            StatusCode = statusCode,
            ContentTypes = { "application/problem+json" }
        };
    }

    public static ObjectResult ApiValidationProblem(
        this ControllerBase controller,
        IDictionary<string, string[]> errors,
        string title = "Validation failed.",
        string detail = "One or more validation errors occurred.")
    {
        var problem = new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = title,
            Type = ApiProblemTypes.ValidationFailed,
            Detail = detail,
            Instance = controller.HttpContext.Request.Path
        };

        AddTraceId(problem, controller.HttpContext);

        return new ObjectResult(problem)
        {
            StatusCode = StatusCodes.Status400BadRequest,
            ContentTypes = { "application/problem+json" }
        };
    }

    private static void AddTraceId(ProblemDetails problem, HttpContext httpContext)
    {
        problem.Extensions["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier;
    }
}
