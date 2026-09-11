var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddSingleton<Users.API.Services.IUserService, Users.API.Services.UserService>();

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

builder.Services.AddSingleton<Users.API.Services.IUserService, Users.API.Services.UserService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

app.Run();
