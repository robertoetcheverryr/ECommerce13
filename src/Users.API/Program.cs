using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using Serilog.Context;
using Serilog.Events;
using Serilog.Formatting.Json;
using Serilog.Sinks.SystemConsole.Themes;
using Users.API.Services;

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

builder.Services.AddSingleton<Users.API.Services.IUserService>(sp =>
{
    var connectionString = sp.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection")
        ?? "Data Source=users.db";
    return new UserService(connectionString);
});

// Exception handlers (order matters: most specific first, generic last)
builder.Services.AddExceptionHandler<Users.API.ExceptionHandlers.NotFoundExceptionHandler>();
builder.Services.AddExceptionHandler<Users.API.ExceptionHandlers.ValidationExceptionHandler>();
builder.Services.AddExceptionHandler<Users.API.ExceptionHandlers.BusinessRuleExceptionHandler>();
builder.Services.AddExceptionHandler<Users.API.ExceptionHandlers.GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddHealthChecks()
    .AddCheck<Users.API.Services.ApiStatusCheck>("api", tags: ["live"])
    .AddCheck<Users.API.Services.SqliteHealthCheck>("sqlite", tags: ["ready"]);

var app = builder.Build();

// Open the SQLite file and create the users table if it is missing.
// a if a is not None else b == a ?? b
var connectionString = app.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=users.db";
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
