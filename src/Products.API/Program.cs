using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using Serilog.Context;
using Serilog.Events;
using Serilog.Formatting.Json;
using Serilog.Sinks.SystemConsole.Themes;

// Start logging before anything else
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "Products.API")
    // Configure the two required sinks, console and JSON file
    .WriteTo.Console(
        theme: AnsiConsoleTheme.Code,
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Service} {Endpoint} {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        new JsonFormatter(renderMessage: true),
        path: "logs/products-.json",
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
    options.OperationFilter<Products.API.ProductsSwaggerExamplesFilter>();
});

// Disable automatic 400 so we can return our custom error format (PRD-002)
builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

// Services
// BTW remember that a Singleton means that there will be a single instance in all of the code of this
// thus, everybody who asks for IProductService or ProductService will get the same exact object
builder.Services.AddSingleton<Products.API.Services.IActiveOrdersChecker, Products.API.Services.NoOpActiveOrdersChecker>();
builder.Services.AddSingleton<Products.API.Services.IProductService, Products.API.Services.ProductService>();

// Exception handlers (order matters: most specific first, generic last)
builder.Services.AddExceptionHandler<Products.API.ExceptionHandlers.NotFoundExceptionHandler>();
builder.Services.AddExceptionHandler<Products.API.ExceptionHandlers.ValidationExceptionHandler>();
builder.Services.AddExceptionHandler<Products.API.ExceptionHandlers.BusinessRuleExceptionHandler>();
builder.Services.AddExceptionHandler<Products.API.ExceptionHandlers.GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks(); // Only the base functionality by dot net, no custom checks yet TODO

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Spec 5.3: Endpoint on every log of the request. Correlation ID is a separate TODO (5.5).
app.Use(async (context, next) =>
{
    using (LogContext.PushProperty("Endpoint", context.Request.Path.Value ?? string.Empty))
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

app.MapHealthChecks("/health", new HealthCheckOptions { ResponseWriter = writeHealthResponse });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { ResponseWriter = writeHealthResponse });
app.MapHealthChecks("/health/live", new HealthCheckOptions { ResponseWriter = writeHealthResponse });

app.Run();

//Removed partial public program... not needed anymore in .NET 10 program is public by default
