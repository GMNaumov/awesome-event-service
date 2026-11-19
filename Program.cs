using System.ComponentModel.DataAnnotations;
using System.Text;
using EventApi.Dtos;
using EventApi.Endpoints;
using EventApi.Errors;
using EventApi.Services;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi(options => options.AddSchemaTransformer((schema, context, _) =>
{
    if (context.JsonPropertyInfo?.AttributeProvider?.IsDefined(typeof(RequiredAttribute), inherit: true) == true)
    {
        schema.Type &= ~JsonSchemaType.Null;
    }

    return Task.CompletedTask;
}));
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        var problem = context.ProblemDetails;
        var request = context.HttpContext.Request;
        problem.Instance ??= request.PathBase.Add(request.Path).ToUriComponent();
        problem.Detail ??= problem.Status switch
        {
            StatusCodes.Status400BadRequest when problem is HttpValidationProblemDetails =>
                "Correct the fields listed in errors.",
            StatusCodes.Status400BadRequest =>
                "Send a non-null JSON object with values of the expected types.",
            StatusCodes.Status404NotFound =>
                "No endpoint matches the request path.",
            StatusCodes.Status405MethodNotAllowed =>
                "Use one of the HTTP methods listed in the Allow header.",
            StatusCodes.Status415UnsupportedMediaType =>
                "Send a JSON request body with Content-Type: application/json and UTF-8 encoding.",
            _ => null
        };
    };
});
builder.Services.AddSingleton<IProblemDetailsWriter, FallbackProblemDetailsWriter>();
builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = false);
builder.Services.AddSingleton<IEventService, InMemoryEventService>();

var app = builder.Build();

app.UseExceptionHandler(new ExceptionHandlerOptions
{
    StatusCodeSelector = exception => exception is BadHttpRequestException badRequest
        ? badRequest.StatusCode
        : StatusCodes.Status500InternalServerError
});
app.UseStatusCodePages();
app.UseHttpsRedirection();

app.Use(async (context, next) =>
{
    var request = context.Request;
    if (context.GetEndpoint()?.Metadata.GetMetadata<IAcceptsMetadata>()?.RequestType == typeof(EventRequest) &&
        context.Features.Get<IHttpRequestBodyDetectionFeature>()?.CanHaveBody == true &&
        MediaTypeHeaderValue.TryParse(request.ContentType, out var contentType) &&
        contentType.Charset.HasValue)
    {
        var charset = HeaderUtilities.RemoveQuotes(contentType.Charset);
        try
        {
            _ = Encoding.GetEncoding(charset.Value!);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException)
        {
            context.Response.StatusCode = StatusCodes.Status415UnsupportedMediaType;
            return;
        }

        // The JSON body reader expects an unquoted encoding name.
        if (charset != contentType.Charset)
        {
            contentType.Charset = charset;
            request.ContentType = contentType.ToString();
        }
    }

    await next(context);
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Event API v1");
    });
}

app.MapEventEndpoints();

app.Run();
