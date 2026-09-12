using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using Serilog.Context;
using Serilog.Events;
using Serilog.Formatting.Json;
using Serilog.Sinks.SystemConsole.Themes;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "Users.API")
    .WriteTo.Console(
        theme: AnsiConsoleTheme.Code,
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Service} {Endpoint} {CorrelationId} {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        new JsonFormatter(renderMessage: true),
        path: "logs/users-.json",
        rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);
    options.SupportNonNullableReferenceTypes();
    options.OperationFilter<Users.API.UsersSwaggerExamplesFilter>();
});

builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

builder.Services.AddSingleton<Users.API.Services.IUserService, Users.API.Services.UserService>();

// Exception handlers (order matters: most specific first, generic last)
builder.Services.AddExceptionHandler<Users.API.ExceptionHandlers.NotFoundExceptionHandler>();
builder.Services.AddExceptionHandler<Users.API.ExceptionHandlers.ValidationExceptionHandler>();
builder.Services.AddExceptionHandler<Users.API.ExceptionHandlers.BusinessRuleExceptionHandler>();
builder.Services.AddExceptionHandler<Users.API.ExceptionHandlers.GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks(); // Only the base functionality by dot net, no custom checks yet TODO

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Spec 5.3 + 5.5: Endpoint and CorrelationId on every log of the request.
// Outbound header propagation still TODO (no HttpClient calls in Users yet).
app.Use(async (context, next) =>
{
    var correlationId = Users.API.CorrelationId.Resolve(context.Request);
    Users.API.CorrelationId.Assign(context, correlationId);

    using (LogContext.PushProperty("Endpoint", context.Request.Path.Value ?? string.Empty))
    using (LogContext.PushProperty(Users.API.CorrelationId.LogProperty, correlationId))
    {
        await next();
    }
});
app.UseSerilogRequestLogging();
app.UseExceptionHandler();
app.UseAuthorization();
app.MapControllers();

var writeHealthResponse = (HttpContext context, HealthReport report) =>
    context.Response.WriteAsJsonAsync(new { status = report.Status.ToString() });

app.MapHealthChecks("/health", new HealthCheckOptions { ResponseWriter = writeHealthResponse });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { ResponseWriter = writeHealthResponse });
app.MapHealthChecks("/health/live", new HealthCheckOptions { ResponseWriter = writeHealthResponse });

app.Run();
