using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Users.API.Exceptions;
using Users.API.Services;
using static Users.API.Tests.ErrorResponseAssertions;
using static Users.API.Tests.UserResponseAssertions;

namespace Users.API.Tests;

/* IClassFixture<> tells xUnit to create one UsersApiFactory and share that
object across the tests in this class. CreateClient() reuses that host.
WithWebHostBuilder and CreateClientWithLogs build another host, and
UsersApiFactory gives that host its own temp database.
The test class constructor still runs per test, which is why seeding runs again.
Roughly comparable to a session-scoped fixture in pytest, not a function-scoped one.
*/
public class UsersEndpointsTests : IClassFixture<UsersApiFactory>
{
    private readonly UsersApiFactory _factory;
    private readonly HttpClient _client;

    public UsersEndpointsTests(UsersApiFactory factory)
    {
        _factory = factory;
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
    
    [Fact]
    public async Task Login_WithUnknownEmail_ShouldStayUnauthorized_AndNotBlockLaterRegister()
    {
        var email = $"missing-{Guid.NewGuid()}@email.com";

        for (var attempt = 0; attempt < 3; attempt++)
            await AssertWrongPassword(email);

        var register = await _client.PostAsJsonAsync("/api/users/register", new
        {
            nombre = "Ana",
            apellido = "Pérez",
            email,
            password = "OtraPassword123!"
        });
        var created = await AssertUserCreated(register);
        created.Activo.Should().BeTrue();

        var login = await _client.PostAsJsonAsync("/api/users/login", new
        {
            email,
            password = "OtraPassword123!"
        });

        var user = await AssertUserOk(login);
        user.Id.Should().Be(created.Id);
        user.Email.Should().Be(email);
    }

    // Two misses, success: the counter resets on a good login.
    [Fact]
    public async Task Login_AfterTwoFailedAttempts_ShouldSucceed_AndResetTheCounter()
    {
        var email = $"reset-{Guid.NewGuid()}@email.com";
        const string password = "OtraPassword123!";
        await Register(email, password);

        await AssertWrongPassword(email);
        await AssertWrongPassword(email);
        await AssertCorrectPassword(email, password);

        await AssertWrongPassword(email);
        await AssertWrongPassword(email);
        await AssertCorrectPassword(email, password);
    }

    // The miss that reaches 3 is still USR-003. The next login, right or wrong password, is USR-004.
    [Fact]
    public async Task Login_OnThirdFailedAttempt_ShouldStayUnauthorized_ThenForbid()
    {
        var email = $"lock-{Guid.NewGuid()}@email.com";
        const string password = "OtraPassword123!";
        await Register(email, password);

        await AssertWrongPassword(email);
        await AssertWrongPassword(email);
        await AssertWrongPassword(email);

        var correctAfterLock = await _client.PostAsJsonAsync("/api/users/login", new
        {
            email,
            password
        });
        await AssertForbidden(
            correctAfterLock,
            "/api/users/login",
            ErrorCodes.USR_004,
            ErrorCodes.USR_004_Message);

        var wrongAfterLock = await _client.PostAsJsonAsync("/api/users/login", new
        {
            email,
            password = "NotThePassword123!"
        });
        await AssertForbidden(
            wrongAfterLock,
            "/api/users/login",
            ErrorCodes.USR_004,
            ErrorCodes.USR_004_Message);
    }

    // Activo false with a counter under 3 is the manual lock, not USR-004.
    [Fact]
    public async Task Login_WhenManuallyBlocked_ShouldReturnForbidden_WithUsr005()
    {
        var email = $"manual-{Guid.NewGuid()}@email.com";
        const string password = "OtraPassword123!";
        await Register(email, password);
        ((UserService)_factory.Services.GetRequiredService<IUserService>()).MarkManuallyBlocked(email);

        var correct = await _client.PostAsJsonAsync("/api/users/login", new
        {
            email,
            password
        });
        await AssertForbidden(
            correct,
            "/api/users/login",
            ErrorCodes.USR_005,
            ErrorCodes.USR_005_Message);

        var wrong = await _client.PostAsJsonAsync("/api/users/login", new
        {
            email,
            password = "NotThePassword123!"
        });
        await AssertForbidden(
            wrong,
            "/api/users/login",
            ErrorCodes.USR_005,
            ErrorCodes.USR_005_Message);
    }

    [Fact]
    public async Task Login_WhenManuallyBlockedAfterTwoFailures_ShouldReturnForbidden_WithUsr005()
    {
        var email = $"manual-two-{Guid.NewGuid()}@email.com";
        const string password = "OtraPassword123!";
        await Register(email, password);

        await AssertWrongPassword(email);
        await AssertWrongPassword(email);
        ((UserService)_factory.Services.GetRequiredService<IUserService>()).MarkManuallyBlocked(email);

        var response = await _client.PostAsJsonAsync("/api/users/login", new
        {
            email,
            password
        });
        await AssertForbidden(
            response,
            "/api/users/login",
            ErrorCodes.USR_005,
            ErrorCodes.USR_005_Message);
    }

    private async Task Register(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/users/register", new
        {
            nombre = "Ana",
            apellido = "Pérez",
            email,
            password
        });
        await AssertUserCreated(response);
    }

    private async Task AssertWrongPassword(string email)
    {
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

    private async Task AssertCorrectPassword(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/users/login", new
        {
            email,
            password
        });

        var user = await AssertUserOk(response);
        user.Email.Should().Be(email);
    }
}
