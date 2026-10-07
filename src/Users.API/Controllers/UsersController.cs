using Microsoft.AspNetCore.Mvc;
using Users.API.DTOs;
using Users.API.Exceptions;
using Users.API.Services;

namespace Users.API.Controllers;

/// <summary>
/// Endpoints de la API de usuarios.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    /// <summary>
    /// Constructor. Inyecta el servicio de usuarios.
    /// </summary>
    /// <param name="userService">Servicio de usuarios.</param>
    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Registra un nuevo usuario.
    /// </summary>
    /// <param name="request">Datos del usuario a registrar.</param>
    /// <returns>El usuario creado.</returns>
    /// <response code="201">Usuario registrado correctamente.</response>
    /// <response code="400">Los datos del usuario son inválidos (USR-002).</response>
    /// <response code="409">El email ya está registrado (USR-001).</response>
    /// <response code="500">Error interno al procesar el usuario (USR-006).</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public ActionResult<UserResponse> Register([FromBody] RegisterUserRequest request)
    {
        EnsureValidModel();

        var user = _userService.Register(request);
        return Created("/api/users/register", ToUserResponse(user));
    }

    /// <summary>
    /// Autentica un usuario con email y contraseña.
    /// </summary>
    /// <param name="request">Credenciales del usuario.</param>
    /// <returns>El usuario autenticado.</returns>
    /// <response code="200">Login correcto.</response>
    /// <response code="400">Los datos del usuario son inválidos (USR-002).</response>
    /// <response code="401">Credenciales incorrectas (USR-003).</response>
    /// <response code="403">Usuario bloqueado por intentos fallidos (USR-004) o por fraude (USR-005).</response>
    /// <response code="500">Error interno al procesar el usuario (USR-006).</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public ActionResult<LoginResponse> Login([FromBody] LoginRequest request)
    {
        EnsureValidModel();

        var user = _userService.Login(request);
        return Ok(ToLoginResponse(user));
    }

    /// <summary>
    /// Convierte ModelState en USR-002. errorMessage junta los textos de Data Annotations.
    /// </summary>
    private void EnsureValidModel()
    {
        if (ModelState.IsValid)
            return;

        var errorMessages = new List<string>();
        foreach (var entry in ModelState)
        {
            foreach (var error in entry.Value.Errors)
            {
                errorMessages.Add(error.ErrorMessage);
            }
        }

        var errors = string.Join("; ", errorMessages);

        throw new ValidationException(
            ErrorCodes.USR_002,
            string.IsNullOrWhiteSpace(errors)
                ? ErrorCodes.USR_002_Message
                : errors);
    }

    // Map here so PasswordHash never leaves the service/controller boundary.
    private static UserResponse ToUserResponse(Models.User user) => new()
    {
        Id = user.Id,
        Nombre = user.Nombre,
        Apellido = user.Apellido,
        Email = user.Email,
        FechaRegistro = user.FechaRegistro,
        Activo = user.Activo
    };

    private static LoginResponse ToLoginResponse(Models.User user) => new()
    {
        Id = user.Id,
        Nombre = user.Nombre,
        Apellido = user.Apellido,
        Email = user.Email
    };
}
