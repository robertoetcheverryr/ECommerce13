var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);
});

// Disable automatic 400 so we can return our custom error format (PRD-002)
builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

// Services
// BTW remember that a Singleton means that there will be a single instance in all of the code of this
// thus, everybody who asks for IProductService or ProductService will get the same exact object
builder.Services.AddSingleton<Products.API.Services.IProductService, Products.API.Services.ProductService>();

// Exception handlers (order matters: most specific first, generic last)
builder.Services.AddExceptionHandler<Products.API.ExceptionHandlers.NotFoundExceptionHandler>();
builder.Services.AddExceptionHandler<Products.API.ExceptionHandlers.ValidationExceptionHandler>();
builder.Services.AddExceptionHandler<Products.API.ExceptionHandlers.BusinessRuleExceptionHandler>();
builder.Services.AddExceptionHandler<Products.API.ExceptionHandlers.GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program
{
}