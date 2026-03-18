using Microsoft.AspNetCore.Mvc;
using asp_webapi_hw.DTO;
using asp_webapi_hw.Exceptions;
using asp_webapi_hw.Models;

namespace asp_webapi_hw.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public ActionResult Register([FromBody] RegisterDto dto)
    {
        if (UserRepository.FindByEmail(dto.Email) is not null)
            throw new ConflictException(
                $"Користувач з email '{dto.Email}' вже існує.");

        var user = UserRepository.Add(new User
        {
            Email = dto.Email,
            UserName = dto.UserName,
            Password = dto.Password,
            Age = dto.Age
        });

        return CreatedAtAction(nameof(Register), new
        {
            id = user.Id,
            email = user.Email,
            userName = user.UserName
        });
    }

    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public ActionResult Login([FromBody] LoginDto dto)
    {
        var user = UserRepository.FindByEmail(dto.Email);

        if (user is null || user.Password != dto.Password)
            throw new UnauthorizedAppException(
                "Невірний email або пароль.");

        return Ok(new
        {
            accessToken = Guid.NewGuid().ToString(),
            expiresIn   = 3600
        });
    }
}
