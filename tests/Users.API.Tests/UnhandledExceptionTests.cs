using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Users.API.DTOs;
using Users.API.Exceptions;
using Users.API.Models;
using Users.API.Services;
using static Users.API.Tests.ErrorResponseAssertions;

namespace Users.API.Tests;

public class UnhandledExceptionTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public UnhandledExceptionTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_WhenServiceThrows_ShouldReturnUsr006()
    {
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IUserService));
                if (descriptor is not null)
                    services.Remove(descriptor);

                services.AddSingleton<IUserService, ThrowingUserService>();
            });
        }).CreateClient();

        var response = await client.PostAsJsonAsync("/api/users/register", new
        {
            nombre = "Test",
            apellido = "User",
            email = "throw@email.com",
            password = "MiPassword123!"
        });

        await AssertInternalError(
            response,
            "/api/users/register",
            ErrorCodes.USR_006,
            ErrorCodes.USR_006_Message);
    }

    private sealed class ThrowingUserService : IUserService
    {
        public User Register(RegisterUserRequest request)
            => throw new Exception("Unexpected failure");

        public User Login(LoginRequest request)
            => throw new Exception("Unexpected failure");
    }
}
