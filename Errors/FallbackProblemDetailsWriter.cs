using System.Diagnostics;
using Microsoft.Extensions.Options;

namespace EventApi.Errors;

internal sealed class FallbackProblemDetailsWriter(IOptions<ProblemDetailsOptions> options) : IProblemDetailsWriter
{
    public bool CanWrite(ProblemDetailsContext context) => true;

    public ValueTask WriteAsync(ProblemDetailsContext context)
    {
        var httpContext = context.HttpContext;
        var problem = context.ProblemDetails;
        problem.Status ??= httpContext.Response.StatusCode;
        problem.Extensions["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        var result = TypedResults.Problem(problem);
        options.Value.CustomizeProblemDetails?.Invoke(context);

        return new ValueTask(httpContext.Response.WriteAsJsonAsync(
            problem, problem.GetType(), options: null, contentType: result.ContentType,
            cancellationToken: httpContext.RequestAborted));
    }
}
