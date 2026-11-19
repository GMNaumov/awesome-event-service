using System.ComponentModel.DataAnnotations;
using System.Text;

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
                "Исправьте ошибки валидации.",
            StatusCodes.Status400BadRequest =>
                "Ожидается JSON-объект с значениями ожидаемых типов.",
            StatusCodes.Status404NotFound =>
                "Событие с указанным идентификатором не найдено.",
            StatusCodes.Status405MethodNotAllowed =>
                "Используйте один из доступных методов HTTP.",
            StatusCodes.Status415UnsupportedMediaType =>
                "Отправьте тело запроса в формате JSON с заголовком Content-Type: application/json и кодировкой UTF-8.",
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
