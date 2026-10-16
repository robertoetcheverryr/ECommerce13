
using Microsoft.OpenApi;
using Notifications.API.ExceptionHandlers;
using Notifications.API.Services;
using Serilog;
using Serilog.Context;
using Serilog.Formatting.Json;
using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

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
builder.Services.AddControllers();

// Services
builder.Services.AddSingleton<INotificationService, NotificationService>();

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
});

// Exception Handlers
builder.Services.AddExceptionHandler<NotFoundExceptionHandler>();
builder.Services.AddExceptionHandler<BusinessRuleExceptionHandler>();
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Health Checks
builder.Services.AddHealthChecks();

var app = builder.Build();

app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault()
                        ?? Guid.NewGuid().ToString();

    context.Request.Headers["X-Correlation-Id"] = correlationId;
    context.Response.Headers["X-Correlation-Id"] = correlationId;

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

// HTTPS
app.UseHttpsRedirection();

// Controllers
app.MapControllers();

// Health Checks
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";

        var response = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString()
            })
        };

        await context.Response.WriteAsJsonAsync(response);
    }
});

app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");

app.Run();