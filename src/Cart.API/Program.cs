using Cart.API.ExceptionHandlers;
using Cart.API.Services;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);
    options.SupportNonNullableReferenceTypes();
    options.OperationFilter<Cart.API.CartSwaggerExamplesFilter>();
});
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

// Context accessor allows other services to read the request.
// Needed for the correlation id handler.
// Transient registers the handler itself.
// All of this is needed because a header is NOT part of the body and
// thus we cannot see it inside the request.
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<CorrelationIdHandler>();
// Our first inter-API call!
builder.Services.AddHttpClient<IProductCatalog, ProductCatalogClient>(client =>
{
    var baseUrl = builder.Configuration["Products:BaseUrl"] ?? "http://localhost:5001";
    client.BaseAddress = new Uri(baseUrl);
}).AddHttpMessageHandler<CorrelationIdHandler>();

builder.Services.AddSingleton<ICartService>(sp =>
{
    var connectionString = sp.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection")
        ?? "Data Source=carts.db";
    return new CartService(connectionString, sp.GetRequiredService<IProductCatalog>());
});

// Exception handlers (order matters: most specific first, generic last)
builder.Services.AddExceptionHandler<NotFoundExceptionHandler>();
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
builder.Services.AddExceptionHandler<BusinessRuleExceptionHandler>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

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
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Use(async (context, next) =>
{
    var correlationId = Cart.API.CorrelationId.Resolve(context.Request);
    Cart.API.CorrelationId.Assign(context, correlationId);
    await next();
});
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
