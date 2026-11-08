
using System.Diagnostics;
using Orders.API;
using Orders.API.Services;
using Orders.API.ExceptionHandlers;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using Serilog.Context;
using Serilog.Formatting.Json;
using Swashbuckle.AspNetCore.Filters;

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

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        // Without this, [ApiController] returns its own ProblemDetails (no errorCode)
        // when the body fails validation; here we answer ORD-002 with the error contract.
        options.InvalidModelStateResponseFactory = context =>
        {
            var errorMessage = string.Join("; ", context.ModelState
                .Values
                .SelectMany(value => value.Errors)
                .Select(error => error.ErrorMessage));

            var response = new Orders.API.DTOs.ErrorResponse
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = "Bad Request",
                Status = StatusCodes.Status400BadRequest,
                Detail = Orders.API.Exceptions.ErrorCodes.ORD_002_Message,
                Instance = context.HttpContext.Request.Path.Value ?? string.Empty,
                ErrorCode = Orders.API.Exceptions.ErrorCodes.ORD_002,
                ErrorMessage = errorMessage,
                CorrelationId = context.HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty
            };

            return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(response);
        };
        // Without this, [ApiController] turns 415 into a ProblemDetails without errorCode; StatusCodeErrorWriter writes it instead
        options.SuppressMapClientErrors = true;
    });

builder.Services.AddExceptionHandler<OrdersExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddScoped<IOrdersService, OrdersService>();

builder.Services.AddHealthChecks()
    .AddCheck<ApiStatusCheck>("api", tags: ["live"])
    .AddCheck<SqliteHealthCheck>("sqlite", tags: ["ready"]);

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
    options.ExampleFilters();
});

builder.Services.AddSwaggerExamplesFromAssemblyOf<Program>();

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

// Open the SQLite file and create the orders tables if they are missing.
var connectionString = app.Configuration.GetConnectionString("DefaultConnection")
                       ?? "Data Source=orders.db";
try
{
    new DatabaseInitializer(connectionString).Initialize();
}
catch (SqliteException ex)
{
    // Error 14 (missing directory) must not kill the host. Ready reports Unhealthy; live still answers.
    Log.Error(ex, "SQLite initialization failed. Ready check will be Unhealthy.");
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.UseStatusCodePages(Orders.API.StatusCodeErrorWriter.WriteAsync);
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

