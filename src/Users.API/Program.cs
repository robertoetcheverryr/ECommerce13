using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);
    // NonNullable is required to avoid Swagger thinking something is nullable when it clearly
    // does not have the ? operator
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

// Spec 5.5 inbound CorrelationId. LogContext / Serilog still TODO (copy from Products next).
app.Use(async (context, next) =>
{
    var correlationId = Users.API.CorrelationId.Resolve(context.Request);
    Users.API.CorrelationId.Assign(context, correlationId);
    await next();
});
app.UseExceptionHandler();
app.UseAuthorization();
app.MapControllers();

var writeHealthResponse = (HttpContext context, HealthReport report) =>
    context.Response.WriteAsJsonAsync(new { status = report.Status.ToString() });

app.MapHealthChecks("/health", new HealthCheckOptions { ResponseWriter = writeHealthResponse });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { ResponseWriter = writeHealthResponse });
app.MapHealthChecks("/health/live", new HealthCheckOptions { ResponseWriter = writeHealthResponse });

app.Run();
