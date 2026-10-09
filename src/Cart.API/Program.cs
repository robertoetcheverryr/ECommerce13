using Cart.API.Services;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddSingleton<ICartService>(sp =>
{
    var connectionString = sp.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection")
        ?? "Data Source=carts.db";
    return new CartService(connectionString);
});

builder.Services.AddHealthChecks()
    .AddCheck<ApiStatusCheck>("api", tags: ["live"])
    .AddCheck<SqliteHealthCheck>("sqlite", tags: ["ready"]);

var app = builder.Build();

// Open the SQLite file and create the cart tables if they are missing.
var connectionString = app.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=carts.db";
try
{
    new DatabaseInitializer(connectionString).Initialize();
}
catch (SqliteException)
{
    // Error 14 (missing directory) must not kill the host. Ready check will be Unhealthy.
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

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
