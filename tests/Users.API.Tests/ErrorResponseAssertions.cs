using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Users.API.Exceptions;

namespace Users.API.Tests;

/*
Shared assertions for the Problem Details + errorCode contract.
type/title/detail come from the IExceptionHandler.
errorCode/errorMessage come from <see cref="ErrorCodes"/>.
*/
internal static class ErrorResponseAssertions
{
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

    /*
    USR-002: errorMessage is the joined DataAnnotation texts, not the catalog sentence.
    Still requires a non-empty errorMessage on the wire.
    */
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

    internal static Task AssertUnauthorized(
        HttpResponseMessage response,
        string instance,
        string errorCode,
        string errorMessage)
    {
        return AssertErrorBody(
            response,
            HttpStatusCode.Unauthorized,
            "https://tools.ietf.org/html/rfc7235#section-3.1",
            "Unauthorized",
            "Las credenciales no son válidas.",
            instance,
            errorCode,
            errorMessage);
    }

    internal static Task AssertForbidden(
        HttpResponseMessage response,
        string instance,
        string errorCode,
        string errorMessage)
    {
        return AssertErrorBody(
            response,
            HttpStatusCode.Forbidden,
            "https://tools.ietf.org/html/rfc7231#section-6.5.3",
            "Forbidden",
            "El acceso está prohibido.",
            instance,
            errorCode,
            errorMessage);
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
            "type", "title", "status", "detail", "instance", "errorCode", "errorMessage", "correlationId");
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

        body["correlationId"].ToString().Should().NotBeNullOrWhiteSpace();
    }
}
