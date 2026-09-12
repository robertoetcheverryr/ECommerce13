using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Users.API.Exceptions;
using Serilog.Events;

namespace Users.API.Tests;

public class BusinessRuleLoggingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public BusinessRuleLoggingTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_WithInvalidData_ShouldLogWarning_WithUsr002()
    {
        var sink = new CollectingSink();
        var client = _factory.CreateClientWithLogs(sink);

        var response = await client.PostAsJsonAsync("/api/users/register", new
        {
            nombre = "",
            apellido = "",
            email = "not-an-email",
            password = ""
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        AssertWarning(sink, ErrorCodes.USR_002, ErrorCodes.USR_002);
        sink.Events.ShouldAllHaveEndpoint("/api/users/register");
        sink.Events.ShouldAllHaveCorrelationId(
            response.Headers.GetValues(CorrelationId.HeaderName).Single());
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ShouldLogWarning_WithUsr001()
    {
        var sink = new CollectingSink();
        var client = _factory.CreateClientWithLogs(sink);
        var email = $"dup-log-{Guid.NewGuid()}@email.com";
        var body = new
        {
            nombre = "María",
            apellido = "González",
            email,
            password = "MiPassword123!"
        };

        (await client.PostAsJsonAsync("/api/users/register", body)).StatusCode.Should().Be(HttpStatusCode.Created);
        sink.Events.Clear();

        var response = await client.PostAsJsonAsync("/api/users/register", body);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        AssertWarning(
            sink,
            ErrorCodes.USR_001,
            string.Format(ErrorCodes.USR_001_Message, email));
        sink.Events.ShouldAllHaveEndpoint("/api/users/register");
        sink.Events.ShouldAllHaveCorrelationId(
            response.Headers.GetValues(CorrelationId.HeaderName).Single());
    }

    [Fact]
    public async Task Login_WithWrongPassword_ShouldLogWarning_WithUsr003()
    {
        var sink = new CollectingSink();
        var client = _factory.CreateClientWithLogs(sink);
        var email = $"wrong-log-{Guid.NewGuid()}@email.com";

        (await client.PostAsJsonAsync("/api/users/register", new
        {
            nombre = "Ana",
            apellido = "Pérez",
            email,
            password = "OtraPassword123!"
        })).StatusCode.Should().Be(HttpStatusCode.Created);
        sink.Events.Clear();

        var response = await client.PostAsJsonAsync("/api/users/login", new
        {
            email,
            password = "NotThePassword123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        AssertWarning(sink, ErrorCodes.USR_003, ErrorCodes.USR_003_Message);
        sink.Events.ShouldAllHaveEndpoint("/api/users/login");
        sink.Events.ShouldAllHaveCorrelationId(
            response.Headers.GetValues(CorrelationId.HeaderName).Single());
    }

    private static void AssertWarning(CollectingSink sink, string errorCode, string errorMessage)
    {
        var warning = sink.Events.Should().ContainSingle(e =>
            e.Level == LogEventLevel.Warning &&
            e.RenderMessage().Contains(errorCode)).Subject;
        warning.RenderMessage().Should().Contain(errorMessage);
    }
}
