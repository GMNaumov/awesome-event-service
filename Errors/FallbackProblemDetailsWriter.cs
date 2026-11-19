using System.Diagnostics;

namespace EventApi.Errors;

internal sealed class FallbackProblemDetailsWriter : IProblemDetailsWriter
{
    public bool CanWrite(ProblemDetailsContext context) => true;

    public ValueTask WriteAsync(ProblemDetailsContext context)
    {
        var httpContext = context.HttpContext;
        var problem = context.ProblemDetails;
        problem.Status ??= httpContext.Response.StatusCode;
        problem.Extensions["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        var result = TypedResults.Problem(problem);
        return new ValueTask(httpContext.Response.WriteAsJsonAsync(
            problem, problem.GetType(), options: null, contentType: result.ContentType));
    }
}
