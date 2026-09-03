using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Products.API.Exceptions;

namespace Products.API.Tests;

// Product's test file was getting large, refactored common assertions here, also becomes single source of
// truth for error assertions so if we change something (which we shouldn't as the spec is already written)
// we have fewer places to change.
/// <summary>
/// Shared assertions for the Problem Details + errorCode contract.
/// type/title/detail come from the IExceptionHandler.
/// errorCode/errorMessage come from <see cref="ErrorCodes"/>.
/// </summary>
internal static class ErrorResponseAssertions
{
    internal static Task AssertNotFound(
        HttpResponseMessage response,
        string instance,
        string errorCode,
        string errorMessage)
    {
        return AssertErrorBody(
            response,
            HttpStatusCode.NotFound,
            "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            "Not Found",
            "El recurso solicitado no fue encontrado.",
            instance,
            errorCode,
            errorMessage);
    }

    internal static Task AssertBadRequest(
        HttpResponseMessage response,
        string instance,
        string errorCode,
        string errorMessage)
    {
        return AssertErrorBody(
            response,
            HttpStatusCode.BadRequest,
            "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            "Bad Request",
            "Los datos enviados no son válidos.",
            instance,
            errorCode,
            errorMessage);
    }

    /// <summary>
    /// PRD-002: errorMessage is the joined DataAnnotation texts, not the catalog sentence.
    /// Still requires a non-empty errorMessage on the wire.
    /// </summary>
    internal static Task AssertBadRequestWithFieldErrors(
        HttpResponseMessage response,
        string instance,
        string errorCode)
    {
        return AssertErrorBody(
            response,
            HttpStatusCode.BadRequest,
            "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            "Bad Request",
            "Los datos enviados no son válidos.",
            instance,
            errorCode,
            expectedErrorMessage: null);
    }

    internal static Task AssertConflict(
        HttpResponseMessage response,
        string instance,
        string errorCode,
        string errorMessage,
        string detail)
    {
        return AssertErrorBody(
            response,
            HttpStatusCode.Conflict,
            "https://tools.ietf.org/html/rfc7231#section-6.5.9",
            "Conflict",
            detail,
            instance,
            errorCode,
            errorMessage);
    }

    internal static Task AssertInternalError(
        HttpResponseMessage response,
        string instance,
        string errorCode,
        string errorMessage)
    {
        return AssertErrorBody(
            response,
            HttpStatusCode.InternalServerError,
            "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            "Internal Server Error",
            "Ocurrió un error inesperado.",
            instance,
            errorCode,
            errorMessage);
    }

    private static async Task AssertErrorBody(
        HttpResponseMessage response,
        HttpStatusCode status,
        string type,
        string title,
        string detail,
        string instance,
        string errorCode,
        string? expectedErrorMessage)
    {
        response.StatusCode.Should().Be(status);

        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body.Should().NotBeNull();
        body.Should().ContainKeys(
            "type", "title", "status", "detail", "instance", "errorCode", "errorMessage");
        body["type"].ToString().Should().Be(type);
        body["title"].ToString().Should().Be(title);
        body["status"].ToString().Should().Be(((int)status).ToString());
        body["detail"].ToString().Should().Be(detail);
        body["instance"].ToString().Should().Be(instance);
        body["errorCode"].ToString().Should().Be(errorCode);

        if (expectedErrorMessage is null)
            body["errorMessage"].ToString().Should().NotBeNullOrWhiteSpace();
        else
            body["errorMessage"].ToString().Should().Be(expectedErrorMessage);
    }
}