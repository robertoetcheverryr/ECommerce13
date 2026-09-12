using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Users.API.DTOs;
using Users.API.Exceptions;
using Users.API.Models;
using Users.API.Services;
using Serilog.Events;
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
    public async Task Register_WhenServiceThrows_ShouldLogError_WithUsr006()
    {
        var sink = new CollectingSink();
        var client = _factory.CreateClientWithLogs(sink, services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IUserService));
            if (descriptor is not null)
                services.Remove(descriptor);

            services.AddSingleton<IUserService, ThrowingUserService>();
        });

        var response = await client.PostAsJsonAsync("/api/users/register", new
        {
            nombre = "Test",
            apellido = "User",
            email = $"throw-{Guid.NewGuid()}@email.com",
            password = "MiPassword123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        await AssertInternalError(
            response,
            "/api/users/register",
            ErrorCodes.USR_006,
            ErrorCodes.USR_006_Message);

        var error = sink.Events.Should().ContainSingle(e =>
            e.Level == LogEventLevel.Error &&
            e.RenderMessage().Contains(ErrorCodes.USR_006)).Subject;
        error.RenderMessage().Should().Contain(ErrorCodes.USR_006_Message);
        error.Exception.Should().NotBeNull();
        sink.Events.ShouldAllHaveEndpoint("/api/users/register");
        sink.Events.ShouldAllHaveCorrelationId(
            response.Headers.GetValues(CorrelationId.HeaderName).Single());
    }

    private sealed class ThrowingUserService : IUserService
    {
        public User Register(RegisterUserRequest request)
            => throw new Exception("Unexpected failure");

        public User Login(LoginRequest request)
            => throw new Exception("Unexpected failure");
    }
}
