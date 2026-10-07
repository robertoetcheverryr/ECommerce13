using Cart.API.Services;
using Microsoft.Data.Sqlite;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

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
    // Error 14 (missing directory) must not kill the host. Ready check comes later.
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthorization();
app.MapControllers();

app.Run();
