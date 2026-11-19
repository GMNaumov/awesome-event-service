using System.ComponentModel.DataAnnotations;
using EventApi.Endpoints;
using EventApi.Errors;
using EventApi.Services;
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
builder.Services.AddProblemDetails();
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
