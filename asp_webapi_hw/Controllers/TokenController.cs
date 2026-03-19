using asp_webapi_hw.DTO;
using asp_webapi_hw.Exceptions;
using asp_webapi_hw.Models;
using asp_webapi_hw.Services;
using Microsoft.AspNetCore.Mvc;

namespace homework_project.Controllers;

[ApiController]
[Route("api/token")]
[Produces("application/json")]
public class TokenController : ControllerBase
{
    private readonly ITokenService _tokenService;

    public TokenController(ITokenService tokenService)
    {
        _tokenService = tokenService;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST api/token/generate
    // Генерація пари токенів (викликається після успішного логіну)
    // ─────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Генерує Access + Refresh токени для користувача за email/password.
    /// </summary>
    /// <response code="200">Пара токенів успішно згенерована.</response>
    /// <response code="400">Невалідні вхідні дані.</response>
    /// <response code="401">Невірний email або пароль.</response>
    [HttpPost("generate")]
    [ProducesResponseType(typeof(TokenResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public ActionResult<TokenResponseDto> Generate([FromBody] LoginDto dto)
    {
        var user = UserRepository.FindByEmail(dto.Email);

        if (user is null || user.Password != dto.Password)
            throw new UnauthorizedAppException("Невірний email або пароль.");

        var tokens = _tokenService.GenerateTokenPair(user);
        return Ok(tokens);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST api/token/refresh
    // Оновлення пари токенів за Refresh Token
    // ─────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Оновлює пару токенів. Старий Refresh Token відкликається (Rotation).
    /// </summary>
    /// <response code="200">Нова пара токенів.</response>
    /// <response code="400">Невалідні вхідні дані.</response>
    /// <response code="401">Refresh Token невалідний, прострочений або відкликаний.</response>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(TokenResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public ActionResult<TokenResponseDto> Refresh([FromBody] RefreshTokenDto dto)
    {
        var tokens = _tokenService.RefreshTokenPair(dto.RefreshToken);

        if (tokens is null)
            throw new UnauthorizedAppException(
                "Refresh Token невалідний, прострочений або вже відкликаний.");

        return Ok(tokens);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST api/token/revoke
    // Відкликання конкретного Refresh Token (logout з одного пристрою)
    // ─────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Відкликає конкретний Refresh Token. Logout з поточного пристрою.
    /// </summary>
    /// <response code="204">Токен успішно відкликано.</response>
    /// <response code="400">Невалідні вхідні дані.</response>
    /// <response code="404">Токен не знайдено або вже відкликано.</response>
    [HttpPost("revoke")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public IActionResult Revoke([FromBody] RevokeTokenDto dto)
    {
        var revoked = _tokenService.RevokeToken(dto.RefreshToken);

        if (!revoked)
            throw new KeyNotFoundException(
                "Refresh Token не знайдено або вже відкликано.");

        return NoContent();
    }

    [HttpPost("revoke-all/{userId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public IActionResult RevokeAll(int userId)
    {
        var user = UserRepository.GetById(userId);

        if (user is null)
            throw new KeyNotFoundException(
                $"Користувача з ID {userId} не знайдено.");

        _tokenService.RevokeAllUserTokens(userId);
        return NoContent();
    }
    [HttpPost("validate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public ActionResult Validate([FromBody] ValidateTokenDto dto)
    {
        var userId = _tokenService.ValidateAccessToken(dto.AccessToken);

        if (userId is null)
            throw new UnauthorizedAppException(
                "Access Token невалідний або прострочений.");

        return Ok(new
        {
            valid  = true,
            userId = userId
        });
    }
}
