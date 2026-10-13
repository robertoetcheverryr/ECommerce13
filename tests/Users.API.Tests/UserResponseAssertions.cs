using System.Net;
using System.Text.Json;
using FluentAssertions;
using Users.API.DTOs;

namespace Users.API.Tests;

/*
Shared assertions for user success payloads.
Checks HTTP status and the JSON key set before deserializing.
PasswordHash must never appear on the wire.
*/
internal static class UserResponseAssertions
{
    private static readonly string[] RegisterKeys =
    [
        "id", "nombre", "apellido", "email", "fechaRegistro", "activo"
    ];

    private static readonly string[] LoginKeys =
    [
        "id", "nombre", "apellido", "email"
    ];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    internal static async Task<UserResponse> AssertUserCreated(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await ReadUser(response, RegisterKeys);
    }

    internal static async Task<UserResponse> AssertUserOk(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return await ReadUser(response, LoginKeys);
    }

    private static async Task<UserResponse> ReadUser(
        HttpResponseMessage response,
        IReadOnlyCollection<string> expectedKeys)
    {
        var json = await response.Content.ReadAsStringAsync();
        var body = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
        body.Should().NotBeNull();
        body.Should().ContainKeys(expectedKeys);
        body.Keys.Should().NotContain(key =>
            key.Equals("passwordHash", StringComparison.OrdinalIgnoreCase) ||
            key.Equals("password", StringComparison.OrdinalIgnoreCase));

        var user = JsonSerializer.Deserialize<UserResponse>(json, JsonOptions);
        user.Should().NotBeNull();
        return user;
    }
}
