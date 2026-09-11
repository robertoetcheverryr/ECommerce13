using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using static Users.API.Tests.UserResponseAssertions;

namespace Users.API.Tests;

/* IClassFixture<> tells xUnit:
"Create ONE single instance of WebApplicationFactory and share it across all tests in this class"
This way we don't restart the API from scratch for every test (that would be very slow).
*/
public class UsersEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public UsersEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_WithValidData_ShouldReturnCreated_WithUser()
    {
        var request = new
        {
            nombre = "María",
            apellido = "González",
            email = "maria@email.com",
            password = "MiPassword123!"
        };

        var response = await _client.PostAsJsonAsync("/api/users/register", request);

        var user = await AssertUserCreated(response);
        user.Nombre.Should().Be("María");
        user.Apellido.Should().Be("González");
        user.Email.Should().Be("maria@email.com");
        user.Activo.Should().BeTrue();
        user.Id.Should().NotBeEmpty();
        user.FechaRegistro.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Login_WithValidData_ShouldReturnOk_WithRegisteredUser()
    {
        var register = new
        {
            nombre = "Ana",
            apellido = "Pérez",
            email = "ana@email.com",
            password = "OtraPassword123!"
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/users/register", register);
        var created = await AssertUserCreated(registerResponse);

        var response = await _client.PostAsJsonAsync("/api/users/login", new
        {
            email = register.email,
            password = register.password
        });

        var user = await AssertUserOk(response);
        user.Id.Should().Be(created.Id);
        user.Nombre.Should().Be(register.nombre);
        user.Apellido.Should().Be(register.apellido);
        user.Email.Should().Be(register.email);
    }
}
