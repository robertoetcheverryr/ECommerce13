using Microsoft.Data.Sqlite;
using Microsoft.OpenApi;
using Notifications.API.ExceptionHandlers;
using Notifications.API.Services;
using Serilog;
using Serilog.Context;
using Serilog.Formatting.Json;
using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Swashbuckle.AspNetCore.Filters;
using Notifications.API.Constants;

var builder = WebApplication.CreateBuilder(args);

// Configuración de Serilog
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "Notifications.API")
    .WriteTo.Console()
    .WriteTo.File(
        new JsonFormatter(),
        "logs/notifications-api-.json",
        rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Controllers
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = new List<string>();
            foreach (var entry in context.ModelState)
            {
                // "request" is the [FromBody] parameter. When the body cannot be deserialized MVC adds
                // a generic "The request field is required." on top of the real error, so we skip it.
                if (entry.Key == "request")
                    continue;

                foreach (var error in entry.Value.Errors)
                {
                    errors.AddRange(Notifications.API.ValidationMessages.ToSpanishMessages(entry.Key, error.ErrorMessage));
                }
            }

            var errorMessage = string.Join("; ", errors);

            var response = new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                title = "Bad Request",
                status = StatusCodes.Status400BadRequest,
                detail = "Los datos de la notificación son inválidos.",
                instance = context.HttpContext.Request.Path.Value,
                errorCode = ErrorCodes.ValidationError,
                errorMessage,
                correlationId = context.HttpContext.Request.Headers["X-Correlation-Id"].FirstOrDefault()
            };

            return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(response);
        };
        // Without this, [ApiController] turns 415 into a ProblemDetails without errorCode; StatusCodeErrorWriter writes it instead
        options.SuppressMapClientErrors = true;
    });

// Services
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? "Data Source=notifications.db";

builder.Services.AddSingleton<INotificationService>(sp =>
    new NotificationService(connectionString));

// Swagger
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Notifications API",
        Version = "v1",
        Description = "API para el registro y envío de notificaciones."
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

    options.IncludeXmlComments(xmlPath);
    options.ExampleFilters();
});

builder.Services.AddSwaggerExamplesFromAssemblyOf<Program>();

// Exception Handlers
builder.Services.AddExceptionHandler<NotFoundExceptionHandler>();
builder.Services.AddExceptionHandler<BusinessRuleExceptionHandler>();
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Health Checks
builder.Services.AddHealthChecks()
    .AddCheck<ApiStatusCheck>("api", tags: ["live"])
    .AddCheck<SqliteHealthCheck>("sqlite", tags: ["ready"]);

var app = builder.Build();
try
{
    new DatabaseInitializer(connectionString).Initialize();
}
catch (SqliteException ex)
{
    // Error 14 (missing directory) must not kill the host. Ready reports Unhealthy; live still answers.
    Log.Error(ex, "No se pudo inicializar la base de datos de notificaciones.");
}

app.Use(async (context, next) =>
{
    var incoming = context.Request.Headers["X-Correlation-Id"].FirstOrDefault();
    // Si recibis un "" no contaba como null o empty agregando este check. 
    var correlationId = !string.IsNullOrEmpty(incoming?.Trim())
        ? incoming.Trim()
        : Guid.NewGuid().ToString();

    context.Request.Headers["X-Correlation-Id"] = correlationId;
    // Agregamos el header justo antes de mandar la respuesta, porque si hay un error el exception handler borra los headers y se perdía
    context.Response.OnStarting(() =>
    {
        context.Response.Headers["X-Correlation-Id"] = correlationId;
        return Task.CompletedTask;
    });

    var stopwatch = Stopwatch.StartNew();

    using (LogContext.PushProperty("CorrelationId", correlationId))
    using (LogContext.PushProperty("Endpoint", context.Request.Path))
    {
        Log.Information(
            "Request iniciado: {Method} {Path}",
            context.Request.Method,
            context.Request.Path);

        try
        {
            await next();
        }
        finally
        {
            stopwatch.Stop();

            Log.Information(
                "Request finalizado: {Method} {Path} - StatusCode: {StatusCode} - DuracionMs: {DurationMs}",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds);
        }
    }
});

// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Manejo global de excepciones
app.UseExceptionHandler();
// 404 unknown route, 405 and 415 have no body by default; give them the ErrorResponse contract
app.UseStatusCodePages(Notifications.API.StatusCodeErrorWriter.WriteAsync);


// Controllers
app.MapControllers();


// Health Checks
// Rider was complaining that we were using a local function with return, changed to lambda
var writeHealthResponse = (HttpContext context, HealthReport report) =>
    context.Response.WriteAsJsonAsync(new { status = report.Status.ToString() });

// The request hits MapHealthChecks, which calls the IHealthCheck handlers (ready/live only run the tagged one)
// and gets a HealthReport back. writeHealthResponse is the way back:
// it writes { status } from report.Status, Healthy, Degraded, or Unhealthy.
app.MapHealthChecks("/health", new HealthCheckOptions { ResponseWriter = writeHealthResponse });
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = writeHealthResponse
});
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live"),
    ResponseWriter = writeHealthResponse
});


app.Run();

/// <summary>
/// Punto de entrada de la API de notificaciones.
/// </summary>
public partial class Program { }