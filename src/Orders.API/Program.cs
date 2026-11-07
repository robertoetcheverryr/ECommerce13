using Orders.API.Services;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpClient<IProductsClient, ProductsClient>(client =>
{
    var baseUrl = builder.Configuration["Services:ProductsApi:BaseUrl"];

    client.BaseAddress = new Uri(baseUrl!);
});
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
