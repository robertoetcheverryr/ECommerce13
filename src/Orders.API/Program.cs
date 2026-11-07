
using System.Diagnostics;
using Orders.API;
using Orders.API.Services;
using Orders.API.ExceptionHandlers;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;
using Serilog.Context;
using Serilog.Formatting.Json;

var builder = WebApplication.CreateBuilder(args);

// Configuración de Serilog
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "Orders.API")
    .WriteTo.Console()
    .WriteTo.File(
        new JsonFormatter(),
        "logs/orders-api-.json",
        rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddControllers();

builder.Services.AddExceptionHandler<OrdersExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddScoped<IOrdersService, OrdersService>();

builder.Services.AddHealthChecks();

builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<CorrelationIdHandler>();

builder.Services.AddHttpClient<IProductsClient, ProductsClient>(client =>
{
    var baseUrl = builder.Configuration["Services:ProductsApi:BaseUrl"];
    client.BaseAddress = new Uri(baseUrl!);
}).AddHttpMessageHandler<CorrelationIdHandler>();

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

    options.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault();

    if (string.IsNullOrWhiteSpace(correlationId))
        correlationId = Guid.NewGuid().ToString();

    context.Items["CorrelationId"] = correlationId;
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

app.UseExceptionHandler();

var connectionString = app.Configuration.GetConnectionString("DefaultConnection")
                       ?? "Data Source=orders.db";

new DatabaseInitializer(connectionString).Initialize();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

// Health Checks
var healthCheckOptions = new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
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
};

app.MapHealthChecks("/health", healthCheckOptions);
app.MapHealthChecks("/health/live", healthCheckOptions);
app.MapHealthChecks("/health/ready", healthCheckOptions);

app.Run();

