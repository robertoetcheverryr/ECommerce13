using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Users.API.Exceptions;
using static Users.API.Tests.ErrorResponseAssertions;
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
        var email = $"maria-{Guid.NewGuid()}@email.com";
        var request = new
        {
            nombre = "María",
            apellido = "González",
            email,
            password = "MiPassword123!"
        };

        var response = await _client.PostAsJsonAsync("/api/users/register", request);

        var user = await AssertUserCreated(response);
        user.Nombre.Should().Be("María");
        user.Apellido.Should().Be("González");
        user.Email.Should().Be(email);
        user.Activo.Should().BeTrue();
        user.Id.Should().NotBeEmpty();
        user.FechaRegistro.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Register_WithInvalidData_ShouldReturnBadRequest_WithUsr002()
    {
        var request = new
        {
            nombre = "",
            apellido = "",
            email = "not-an-email",
            password = ""
        };

        var response = await _client.PostAsJsonAsync("/api/users/register", request);

        await AssertBadRequestWithFieldErrors(response, "/api/users/register", ErrorCodes.USR_002);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ShouldReturnConflict_WithUsr001()
    {
        var email = $"dup-{Guid.NewGuid()}@email.com";
        var request = new
        {
            nombre = "María",
            apellido = "González",
            email,
            password = "MiPassword123!"
        };

        var first = await _client.PostAsJsonAsync("/api/users/register", request);
        await AssertUserCreated(first);

        var second = await _client.PostAsJsonAsync("/api/users/register", request);

        await AssertConflict(
            second,
            "/api/users/register",
            ErrorCodes.USR_001,
            string.Format(ErrorCodes.USR_001_Message, email),
            ErrorCodes.USR_001_Detail);
    }

    [Fact]
    public async Task Login_WithValidData_ShouldReturnOk_WithRegisteredUser()
    {
        var register = new
        {
            nombre = "Ana",
            apellido = "Pérez",
            email = $"ana-{Guid.NewGuid()}@email.com",
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

    [Fact]
    public async Task Login_WithWrongPassword_ShouldReturnUnauthorized_WithUsr003()
    {
        var email = $"wrong-{Guid.NewGuid()}@email.com";
        var register = await _client.PostAsJsonAsync("/api/users/register", new
        {
            nombre = "Ana",
            apellido = "Pérez",
            email,
            password = "OtraPassword123!"
        });
        await AssertUserCreated(register);

        var response = await _client.PostAsJsonAsync("/api/users/login", new
        {
            email,
            password = "NotThePassword123!"
        });

        await AssertUnauthorized(
            response,
            "/api/users/login",
            ErrorCodes.USR_003,
            ErrorCodes.USR_003_Message);
    }
}
